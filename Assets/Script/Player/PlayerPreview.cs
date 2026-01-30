using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPreview : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform spawnPoint;
    public string uiLayerName = "UI_Model";

    private GameObject currentModel;
    public GameObject CurrentModel => currentModel;

    void Start()
    {
        ShowPlayer();
    }

    public void ShowPlayer()
    {
        if (currentModel != null) Destroy(currentModel);

        currentModel = Instantiate(playerPrefab, spawnPoint);
        currentModel.transform.SetPositionX(spawnPoint.position.x);
        currentModel.transform.SetPositionY(spawnPoint.position.y);
        currentModel.transform.SetPositionZ(0f);

        var controller = currentModel.GetComponent<PlayerController>();
        if (controller) Destroy(controller);
        var rb = currentModel.GetComponent<Rigidbody2D>();
        if (rb) Destroy(rb);

        SetLayerRecursively(currentModel, LayerMask.NameToLayer(uiLayerName));
        currentModel.tag = "Untagged";
        SetUnscaledTimeRecursively(currentModel);

        EventManager.AddListener<ModuleType>(GameEvent.PlayerUIModel, UnLockUIModule);
    }

    private void UnLockUIModule(ModuleType moduleType)
    {
        currentModel.GetComponent<PlayerModuleManager>().UnlockModule(moduleType);
    }

    void Update()
    {
        if (currentModel != null)
        {
            currentModel.transform.Rotate(0, 0, -30 * Time.unscaledDeltaTime);
        }
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
    void SetUnscaledTimeRecursively(GameObject obj)
    {
        if (obj == null) return;

        ParticleSystem[] particles = obj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            var main = ps.main;
            main.useUnscaledTime = true;
        }

        Animator[] animators = obj.GetComponentsInChildren<Animator>(true);
        foreach (var anim in animators)
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }
}
