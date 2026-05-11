using System.Collections.Generic;
using UnityEngine;

public class DragonPortalTraveller : MonoBehaviour
{
    private struct OriginalMaskState
    {
        public SpriteRenderer renderer;
        public SpriteMaskInteraction maskInteraction;
    }

    private sealed class GhostVisual
    {
        public GameObject root;
        public Transform[] originalTransforms;
        public Transform[] ghostTransforms;
        public SpriteRenderer[] ghostRenderers;
    }

    private DragonPortalSurface currentPortal;
    private DragonPortalSurface linkedPortal;
    private GhostVisual ghostVisual;
    private SpriteRenderer[] sourceRenderers;
    private Transform[] sourceTransforms;
    private OriginalMaskState[] originalMaskStates;
    private Rigidbody2D cachedRigidbody;
    private Collider2D cachedCollider;
    private bool teleportedThisPass;
    private float enterSideSign;

    public static DragonPortalTraveller GetOrCreate(Collider2D hitCollider, bool createIfMissing = true)
    {
        if (hitCollider == null)
            return null;

        Rigidbody2D attachedBody = hitCollider.attachedRigidbody;
        GameObject root = attachedBody != null ? attachedBody.gameObject : hitCollider.gameObject;
        DragonPortalTraveller traveller = root.GetComponent<DragonPortalTraveller>();
        if (traveller == null && createIfMissing)
            traveller = root.AddComponent<DragonPortalTraveller>();
        return traveller;
    }

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody2D>();
        cachedCollider = GetComponent<Collider2D>();
        sourceRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        sourceTransforms = new Transform[sourceRenderers.Length];
        originalMaskStates = new OriginalMaskState[sourceRenderers.Length];

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            sourceTransforms[i] = sourceRenderers[i].transform;
            originalMaskStates[i] = new OriginalMaskState
            {
                renderer = sourceRenderers[i],
                maskInteraction = sourceRenderers[i].maskInteraction
            };
        }
    }

    public void EnterPortal(DragonPortalSurface portal)
    {
        if (portal == null || portal.linkedPortal == null)
            return;

        if (currentPortal == portal)
            return;

        currentPortal = portal;
        linkedPortal = portal.linkedPortal;
        teleportedThisPass = false;
        enterSideSign = Mathf.Sign(portal.SignedDistanceToPortal(transform.position));
        if (Mathf.Approximately(enterSideSign, 0f))
            enterSideSign = 1f;

        ApplySourceMaskInteraction(SpriteMaskInteraction.VisibleOutsideMask);
        EnsureGhost();
        SetGhostActive(true);
    }

    public void ExitPortal(DragonPortalSurface portal)
    {
        if (portal != currentPortal)
            return;

        currentPortal = null;
        linkedPortal = null;
        teleportedThisPass = false;
        ApplySourceMaskInteractionToOriginal();
        SetGhostActive(false);
    }

    public void TickPortalVisual(DragonPortalSurface portal, DragonPortalSurface targetPortal)
    {
        if (portal == null || targetPortal == null || portal != currentPortal || ghostVisual == null)
            return;

        SyncGhostTransforms(portal, targetPortal);
        TryTeleportThrough(portal, targetPortal);
    }

    private void EnsureGhost()
    {
        if (ghostVisual != null && ghostVisual.root != null)
            return;

        ghostVisual = new GhostVisual
        {
            root = new GameObject(name + "_PortalGhost"),
            originalTransforms = sourceTransforms,
            ghostTransforms = new Transform[sourceTransforms.Length],
            ghostRenderers = new SpriteRenderer[sourceRenderers.Length]
        };

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            GameObject child = new GameObject(sourceRenderers[i].name + "_Ghost");
            child.transform.SetParent(ghostVisual.root.transform, false);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sourceRenderers[i].sprite;
            renderer.sharedMaterial = sourceRenderers[i].sharedMaterial;
            renderer.sortingLayerID = sourceRenderers[i].sortingLayerID;
            renderer.sortingOrder = sourceRenderers[i].sortingOrder + 1;
            renderer.color = sourceRenderers[i].color;
            renderer.flipX = sourceRenderers[i].flipX;
            renderer.flipY = sourceRenderers[i].flipY;
            renderer.drawMode = sourceRenderers[i].drawMode;
            renderer.size = sourceRenderers[i].size;
            renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            ghostVisual.ghostTransforms[i] = child.transform;
            ghostVisual.ghostRenderers[i] = renderer;
        }

        ghostVisual.root.SetActive(false);
    }

    private void SyncGhostTransforms(DragonPortalSurface portal, DragonPortalSurface targetPortal)
    {
        for (int i = 0; i < ghostVisual.ghostTransforms.Length; i++)
        {
            Transform source = ghostVisual.originalTransforms[i];
            Transform ghost = ghostVisual.ghostTransforms[i];
            SpriteRenderer sourceRenderer = sourceRenderers[i];
            SpriteRenderer ghostRenderer = ghostVisual.ghostRenderers[i];

            ghost.position = portal.MapWorldPointToLinked(source.position, targetPortal);

            Vector3 mirroredRight = portal.MapWorldVectorToLinked(source.right, targetPortal);
            if (mirroredRight.sqrMagnitude > 0.0001f)
                ghost.right = mirroredRight.normalized;

            ghost.localScale = source.lossyScale;
            ghostRenderer.sprite = sourceRenderer.sprite;
            ghostRenderer.color = sourceRenderer.color;
            ghostRenderer.flipX = sourceRenderer.flipX;
            ghostRenderer.flipY = sourceRenderer.flipY;
            ghostRenderer.enabled = sourceRenderer.enabled;
        }
    }

    private void TryTeleportThrough(DragonPortalSurface portal, DragonPortalSurface targetPortal)
    {
        if (teleportedThisPass)
            return;

        Bounds bounds = CalculateWorldBounds();
        float distance = portal.SignedDistanceToPortal(bounds.center);
        float extent = ApproximatePortalNormalExtent(portal, bounds);

        bool fullyCrossed = enterSideSign < 0f
            ? distance - extent > 0f
            : distance + extent < 0f;

        if (!fullyCrossed)
            return;

        Vector3 mappedPosition = portal.MapWorldPointToLinked(transform.position, targetPortal);
        transform.position = targetPortal.GetSurfacePoint(mappedPosition, -enterSideSign);

        if (cachedRigidbody != null)
        {
            cachedRigidbody.position = transform.position;
            cachedRigidbody.velocity = portal.MapWorldVectorToLinked(cachedRigidbody.velocity, targetPortal);
        }

        teleportedThisPass = true;
        currentPortal = targetPortal;
        linkedPortal = targetPortal.linkedPortal;
        enterSideSign = Mathf.Sign(targetPortal.SignedDistanceToPortal(transform.position));
        if (Mathf.Approximately(enterSideSign, 0f))
            enterSideSign = 1f;
    }

    private Bounds CalculateWorldBounds()
    {
        if (sourceRenderers == null || sourceRenderers.Length == 0)
            return new Bounds(transform.position, Vector3.zero);

        Bounds bounds = sourceRenderers[0].bounds;
        for (int i = 1; i < sourceRenderers.Length; i++)
            bounds.Encapsulate(sourceRenderers[i].bounds);
        return bounds;
    }

    private float ApproximatePortalNormalExtent(DragonPortalSurface portal, Bounds worldBounds)
    {
        Vector3 localExtents = portal.transform.InverseTransformVector(worldBounds.extents);
        return Mathf.Abs(localExtents.x);
    }

    private void ApplySourceMaskInteraction(SpriteMaskInteraction interaction)
    {
        for (int i = 0; i < sourceRenderers.Length; i++)
            sourceRenderers[i].maskInteraction = interaction;
    }

    private void ApplySourceMaskInteractionToOriginal()
    {
        for (int i = 0; i < originalMaskStates.Length; i++)
        {
            if (originalMaskStates[i].renderer != null)
                originalMaskStates[i].renderer.maskInteraction = originalMaskStates[i].maskInteraction;
        }
    }

    private void SetGhostActive(bool active)
    {
        if (ghostVisual != null && ghostVisual.root != null)
            ghostVisual.root.SetActive(active);
    }

    private void OnDisable()
    {
        ApplySourceMaskInteractionToOriginal();
        SetGhostActive(false);
    }

    private void OnDestroy()
    {
        ApplySourceMaskInteractionToOriginal();
        if (ghostVisual != null && ghostVisual.root != null)
            Destroy(ghostVisual.root);
    }
}
