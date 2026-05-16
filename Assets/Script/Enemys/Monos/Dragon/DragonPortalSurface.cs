using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class DragonPortalSurface : MonoBehaviour
{
    public enum PortalType
    {
        Enter,
        Outer
    }

    [Header("Portal Setup")]
    public PortalType portalType;
    public DragonPortalSurface linkedPortal;
    public bool autoBuildMask = true;
    public float triggerWidthScale = 0.2f;
    public float triggerHeightScale = 0.9f;
    public float surfaceOffset = 0.02f;
    public bool autoLinkOppositePortal = true;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D triggerCollider;
    private SpriteMask spriteMask;
    private readonly HashSet<DragonPortalTraveller> activeTravellers = new HashSet<DragonPortalTraveller>();

    public SpriteMask PortalMask => spriteMask;
    public Vector3 PortalNormal => transform.right;
    public Vector3 PortalUp => transform.up;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        RebuildSupportComponents();
        TryAutoLink();
    }

    private void OnEnable()
    {
        TryAutoLink();
    }

    private void OnValidate()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<BoxCollider2D>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        if (!Application.isPlaying)
            RebuildSupportComponents();
    }

    private void LateUpdate()
    {
        if (linkedPortal == null || activeTravellers.Count == 0)
            return;

        foreach (DragonPortalTraveller traveller in activeTravellers)
        {
            if (traveller == null)
                continue;

            traveller.TickPortalVisual(this, linkedPortal);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        DragonPortalTraveller traveller = DragonPortalTraveller.GetOrCreate(other);
        if (traveller == null)
            return;

        traveller.EnterPortal(this);
        activeTravellers.Add(traveller);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DragonPortalTraveller traveller = DragonPortalTraveller.GetOrCreate(other);
        if (traveller == null)
            return;

        if (!activeTravellers.Contains(traveller))
        {
            traveller.EnterPortal(this);
            activeTravellers.Add(traveller);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        DragonPortalTraveller traveller = DragonPortalTraveller.GetOrCreate(other, false);
        if (traveller == null)
            return;

        activeTravellers.Remove(traveller);
        traveller.ExitPortal(this);
    }

    public Vector3 MapWorldPointToLinked(Vector3 worldPoint, DragonPortalSurface targetPortal)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        local.x = -local.x;
        return targetPortal.transform.TransformPoint(local);
    }

    public Vector2 MapWorldVectorToLinked(Vector2 worldVector, DragonPortalSurface targetPortal)
    {
        Vector3 local = transform.InverseTransformDirection(worldVector);
        local.x = -local.x;
        Vector3 mapped = targetPortal.transform.TransformDirection(local);
        return new Vector2(mapped.x, mapped.y);
    }

    public void LinkTo(DragonPortalSurface other)
    {
        linkedPortal = other;
        if (other != null && other.linkedPortal != this)
            other.linkedPortal = this;
    }

    public float SignedDistanceToPortal(Vector3 worldPoint)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        return local.x;
    }

    public Vector3 GetSurfacePoint(Vector3 worldPoint, float sideSign)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        local.x = surfaceOffset * Mathf.Sign(sideSign);
        return transform.TransformPoint(local);
    }

    private void RebuildSupportComponents()
    {
        if (spriteRenderer == null || triggerCollider == null)
            return;

        Bounds bounds = spriteRenderer.bounds;
        Vector3 lossy = transform.lossyScale;
        float scaleX = Mathf.Abs(lossy.x) <= 0.0001f ? 1f : Mathf.Abs(lossy.x);
        float scaleY = Mathf.Abs(lossy.y) <= 0.0001f ? 1f : Mathf.Abs(lossy.y);

        triggerCollider.offset = Vector2.zero;
        triggerCollider.size = new Vector2(
            Mathf.Max(0.05f, bounds.size.x * triggerWidthScale / scaleX),
            Mathf.Max(0.2f, bounds.size.y * triggerHeightScale / scaleY));

        if (!autoBuildMask)
            return;

        if (spriteMask == null)
        {
            Transform existing = transform.Find("PortalMask");
            if (existing != null)
                spriteMask = existing.GetComponent<SpriteMask>();

            if (spriteMask == null)
            {
                GameObject maskObj = new GameObject("PortalMask");
                maskObj.transform.SetParent(transform, false);
                spriteMask = maskObj.AddComponent<SpriteMask>();
            }
        }

        spriteMask.sprite = spriteRenderer.sprite;
        spriteMask.transform.localPosition = Vector3.zero;
        spriteMask.transform.localRotation = Quaternion.identity;
        spriteMask.transform.localScale = Vector3.one;
        spriteMask.isCustomRangeActive = true;
        spriteMask.frontSortingOrder = 5000;
        spriteMask.backSortingOrder = -5000;
    }

    private void TryAutoLink()
    {
        if (!autoLinkOppositePortal || linkedPortal != null)
            return;

        DragonPortalSurface[] portals = FindObjectsOfType<DragonPortalSurface>(true);
        float bestDistance = float.MaxValue;
        DragonPortalSurface bestMatch = null;

        for (int i = 0; i < portals.Length; i++)
        {
            DragonPortalSurface candidate = portals[i];
            if (candidate == null || candidate == this)
                continue;

            if (candidate.portalType == portalType)
                continue;

            float distance = Vector3.SqrMagnitude(candidate.transform.position - transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = candidate;
            }
        }

        if (bestMatch != null)
            LinkTo(bestMatch);
    }
}
