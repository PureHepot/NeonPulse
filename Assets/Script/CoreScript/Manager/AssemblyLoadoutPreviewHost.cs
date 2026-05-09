using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class AssemblyLoadoutPreviewHost : MonoBehaviour
{
    private const string PreviewRootName = "AssemblyPreviewRoot";
    private const string PlayerPrefabResourcePath = "Prefabs/Mono/Player/Player";
    private const string FrameCoreResourceRoot = "Prefabs/Mono/Frame/Core";
    private const string FrameSlotLayoutPreviewName = "FrameSlotLayoutPreview";

    private readonly List<GameObject> hiddenLegacyObjects = new();

    private Transform previewRoot;
    private Camera previewCamera;
    private GameObject previewPlayer;
    private PlayerController previewController;
    private ModuleManager previewModules;
    private Transform coreRoot;
    private Transform modulesRoot;
    private GameObject frameCoreInstance;
    private string currentFrameId;
    private int previewLayer = -1;
    private SpriteRenderer defaultCoreSpriteRenderer;
    private Animator defaultCoreAnimator;

    public RenderTexture TargetTexture => previewCamera != null ? previewCamera.targetTexture : null;

    private void Awake()
    {
        previewCamera = GetComponent<Camera>();
        previewLayer = LayerMask.NameToLayer("UI_Model");
        previewRoot = transform.Find(PreviewRootName);
        if (previewRoot == null)
        {
            var root = new GameObject(PreviewRootName);
            previewRoot = root.transform;
            previewRoot.SetParent(transform, false);
            previewRoot.localPosition = new Vector3(0f, 0f, 10f);
            previewRoot.localRotation = Quaternion.identity;
            previewRoot.localScale = Vector3.one;
        }
    }

    public void Show(AssemblyLoadoutSnapshot snapshot)
    {
        HideLegacyPreview();
        EnsurePreviewPlayer();
        SyncFrame(snapshot != null ? snapshot.frameId : null);
        SyncModules(snapshot);
        if (previewPlayer != null)
            previewPlayer.SetActive(true);
    }

    public void HidePreview()
    {
        if (previewPlayer != null)
            previewPlayer.SetActive(false);

        RestoreLegacyPreview();
    }

    private void EnsurePreviewPlayer()
    {
        if (previewPlayer != null)
            return;

        var playerPrefab = Resources.Load<GameObject>(PlayerPrefabResourcePath);
        if (playerPrefab == null)
        {
            Debug.LogWarning("[AssemblyLoadoutPreviewHost] Player prefab not found for assembly preview.");
            return;
        }

        previewPlayer = Instantiate(playerPrefab, previewRoot);
        previewPlayer.transform.localPosition = Vector3.zero;
        previewPlayer.transform.localRotation = Quaternion.identity;
        previewPlayer.transform.localScale = Vector3.one;

        previewController = previewPlayer.GetComponent<PlayerController>();
        previewModules = previewPlayer.GetComponent<ModuleManager>();
        if (previewController != null)
            previewController.ConfigureRuntime(false, true);

        var rigidbody = previewPlayer.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.simulated = false;
            rigidbody.velocity = Vector2.zero;
        }

        foreach (var collider in previewPlayer.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;

        coreRoot = previewPlayer.transform.Find("Core");
        if (coreRoot == null)
            coreRoot = previewPlayer.transform;

        modulesRoot = previewPlayer.transform.Find("Modules");
        if (modulesRoot == null)
            modulesRoot = previewPlayer.transform;

        defaultCoreSpriteRenderer = coreRoot.GetComponent<SpriteRenderer>();
        defaultCoreAnimator = coreRoot.GetComponent<Animator>();

        if (previewLayer >= 0)
            SetLayerRecursively(previewPlayer, previewLayer);

        SetUnscaledTimeRecursively(previewPlayer);
    }

    private void SyncFrame(string frameId)
    {
        frameId ??= string.Empty;
        if (currentFrameId == frameId)
            return;

        currentFrameId = frameId;
        RefreshFrameCore(ResolveFrameConfig(frameId));
    }

    private void SyncModules(AssemblyLoadoutSnapshot snapshot)
    {
        if (previewModules == null || modulesRoot == null)
            return;

        previewModules.ClearRuntimeModules();

        if (snapshot == null)
            return;

        foreach (var slot in snapshot.slots)
        {
            if (slot == null || !slot.HasModule || slot.runtimeData == null)
                continue;

            var modulePrefab = PlayerModulePrefabResolver.Resolve(slot.runtimeData);
            if (modulePrefab == null)
            {
                Debug.LogWarning($"[AssemblyLoadoutPreviewHost] Missing runtime prefab for module {slot.moduleId}.");
                continue;
            }

            GameObject moduleObject = Instantiate(modulePrefab, modulesRoot);

            if (previewLayer >= 0)
                SetLayerRecursively(moduleObject, previewLayer);

            NeutralizePhysics(moduleObject);
            SetUnscaledTimeRecursively(moduleObject);
            RemovePreviewBehaviours(moduleObject);

            var playerModule = moduleObject.AddComponent<PassiveModule>();

            previewModules.RegisterRuntimeModule(playerModule, slot.runtimeData);
        }
    }

    private FrameConfig ResolveFrameConfig(string frameId)
    {
        if (string.IsNullOrWhiteSpace(frameId))
            return null;

        var database = GameConfigDatabase.Instance;
        if (database?.allFrames == null)
            return null;

        foreach (var frame in database.allFrames)
        {
            if (frame != null && string.Equals(frame.frameId, frameId, System.StringComparison.OrdinalIgnoreCase))
                return frame;
        }

        return null;
    }

    private void HideLegacyPreview()
    {
        hiddenLegacyObjects.Clear();
        for (int index = 0; index < transform.childCount; index++)
        {
            var child = transform.GetChild(index);
            if (child == previewRoot)
                continue;

            if (child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(false);
                hiddenLegacyObjects.Add(child.gameObject);
            }
        }
    }

    private void RestoreLegacyPreview()
    {
        foreach (var legacyObject in hiddenLegacyObjects)
        {
            if (legacyObject != null)
                legacyObject.SetActive(true);
        }

        hiddenLegacyObjects.Clear();
    }

    private static void NeutralizePhysics(GameObject target)
    {
        foreach (var rigidbody in target.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.simulated = false;
            rigidbody.velocity = Vector2.zero;
        }

        foreach (var collider in target.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private static void SetUnscaledTimeRecursively(GameObject obj)
    {
        foreach (var particle in obj.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particle.main;
            main.useUnscaledTime = true;
        }

        foreach (var animator in obj.GetComponentsInChildren<Animator>(true))
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    private static void RemovePreviewBehaviours(GameObject target)
    {
        foreach (var behaviour in target.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null)
                continue;

            behaviour.enabled = false;
            Destroy(behaviour);
        }
    }

    private void RefreshFrameCore(FrameConfig frameConfig)
    {
        if (coreRoot == null)
            return;

        if (frameCoreInstance != null)
        {
            Destroy(frameCoreInstance);
            frameCoreInstance = null;
        }

        if (defaultCoreSpriteRenderer != null)
            defaultCoreSpriteRenderer.enabled = true;

        if (defaultCoreAnimator != null)
            defaultCoreAnimator.enabled = true;

        var resolvedFrameCore = ResolveFrameCorePrefab(frameConfig);
        if (resolvedFrameCore == null)
            return;

        if (TryInstantiateFrameCore(resolvedFrameCore))
            return;

        ApplyFrameCoreSpriteProxy(resolvedFrameCore);
    }

    private static GameObject ResolveFrameCorePrefab(FrameConfig frameConfig)
    {
        if (frameConfig == null)
            return null;

        string framePrefabName = string.Empty;
        if (frameConfig.slotLayoutPrefab != null)
            framePrefabName = frameConfig.slotLayoutPrefab.name;
        else if (frameConfig.frameCore != null)
            framePrefabName = frameConfig.frameCore.name;
        else if (!string.IsNullOrWhiteSpace(frameConfig.frameId))
            framePrefabName = frameConfig.frameId.Trim();

        if (!string.IsNullOrWhiteSpace(framePrefabName))
        {
            var runtimeFrameCore = Resources.Load<GameObject>($"{FrameCoreResourceRoot}/Core_{framePrefabName}");
            if (runtimeFrameCore != null)
                return runtimeFrameCore;
        }

        return frameConfig.frameCore;
    }

    private bool TryInstantiateFrameCore(GameObject frameCorePrefab)
    {
        if (frameCorePrefab == null)
            return false;

        if (frameCorePrefab.GetComponentInChildren<SpriteRenderer>(true) == null)
            return false;

        frameCoreInstance = Instantiate(frameCorePrefab, coreRoot);
        frameCoreInstance.name = FrameSlotLayoutPreviewName;
        frameCoreInstance.transform.localPosition = Vector3.zero;
        frameCoreInstance.transform.localRotation = Quaternion.identity;
        frameCoreInstance.transform.localScale = Vector3.one;

        NeutralizePhysics(frameCoreInstance);
        SetUnscaledTimeRecursively(frameCoreInstance);

        if (previewLayer >= 0)
            SetLayerRecursively(frameCoreInstance, previewLayer);

        if (defaultCoreSpriteRenderer != null)
            defaultCoreSpriteRenderer.enabled = false;

        if (defaultCoreAnimator != null)
            defaultCoreAnimator.enabled = false;

        return true;
    }

    private void ApplyFrameCoreSpriteProxy(GameObject frameCorePrefab)
    {
        if (defaultCoreSpriteRenderer == null || frameCorePrefab == null)
            return;

        Sprite sprite = null;
        Color color = Color.white;

        var spriteRenderer = frameCorePrefab.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer != null)
        {
            sprite = spriteRenderer.sprite;
            color = spriteRenderer.color;
        }
        else
        {
            var image = frameCorePrefab.GetComponentInChildren<Image>(true);
            if (image != null)
            {
                sprite = image.sprite;
                color = image.color;
            }
        }

        if (sprite == null)
            return;

        defaultCoreSpriteRenderer.sprite = sprite;
        defaultCoreSpriteRenderer.color = color;

        if (defaultCoreAnimator != null)
            defaultCoreAnimator.enabled = false;
    }
}
