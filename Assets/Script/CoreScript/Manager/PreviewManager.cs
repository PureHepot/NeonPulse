using UnityEngine;

public class PreviewManager : MonoSingleton<PreviewManager>
{
    private AssemblyLoadoutPreviewHost assemblyPreviewHost;

    public RenderTexture GetAssemblyPreviewTexture()
    {
        return EnsureAssemblyPreviewHost()?.TargetTexture;
    }

    public void ShowAssemblyPreview(AssemblyLoadoutSnapshot snapshot)
    {
        EnsureAssemblyPreviewHost()?.Show(snapshot);
    }

    public void HideAssemblyPreview()
    {
        assemblyPreviewHost?.HidePreview();
    }

    public void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null)
            return;

        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }

    private AssemblyLoadoutPreviewHost EnsureAssemblyPreviewHost()
    {
        if (assemblyPreviewHost != null)
            return assemblyPreviewHost;

        var cameraObject = GameObject.Find("PlayerModelCamera");
        if (cameraObject == null)
            return null;

        assemblyPreviewHost = cameraObject.GetComponent<AssemblyLoadoutPreviewHost>();
        if (assemblyPreviewHost == null)
            assemblyPreviewHost = cameraObject.AddComponent<AssemblyLoadoutPreviewHost>();

        return assemblyPreviewHost;
    }
}
