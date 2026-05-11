using UnityEngine;

[ExecuteAlways]
public class TailBoneSway : MonoBehaviour
{
    [SerializeField] private Transform[] bones;
    [SerializeField] private bool animate = true;
    [SerializeField] private bool previewInEditMode = true;
    [SerializeField] private float amplitude = 10f;
    [SerializeField] private float frequency = 1.25f;
    [SerializeField] private float phaseOffset = 0.45f;
    [SerializeField] private float tipFalloff = 1.35f;
    [SerializeField] private float smooth = 14f;
    [SerializeField, HideInInspector] private Quaternion[] restRotations;

    public Transform[] Bones => bones;

    private void OnEnable()
    {
        EnsureRestPose();
    }

    private void OnValidate()
    {
        if (frequency < 0f) frequency = 0f;
        if (tipFalloff < 0.1f) tipFalloff = 0.1f;
        if (smooth < 0f) smooth = 0f;
        EnsureRestPose();
    }

    private void LateUpdate()
    {
        if (!animate || bones == null || bones.Length == 0) return;
        if (!Application.isPlaying && !previewInEditMode) return;

        EnsureRestPose();

        float time = Application.isPlaying ? Time.time : (float)UnityEngine.Time.realtimeSinceStartup;
        float waveTime = time * frequency * Mathf.PI * 2f;
        float damp = smooth <= 0f ? 1f : 1f - Mathf.Exp(-smooth * Mathf.Max(Time.deltaTime, 0.016f));

        for (int i = 0; i < bones.Length; i++)
        {
            Transform bone = bones[i];
            if (bone == null) continue;

            float t = bones.Length <= 1 ? 0f : i / (float)(bones.Length - 1);
            float strength = Mathf.Pow(t, tipFalloff);
            float angle = Mathf.Sin(waveTime + i * phaseOffset) * amplitude * strength;
            Quaternion targetRotation = restRotations[i] * Quaternion.Euler(0f, 0f, angle);

            bone.localRotation = smooth <= 0f
                ? targetRotation
                : Quaternion.Slerp(bone.localRotation, targetRotation, damp);
        }
    }

    public void SetBones(Transform[] newBones)
    {
        bones = newBones;
        CacheRestPose();
    }

    [ContextMenu("Cache Rest Pose")]
    public void CacheRestPose()
    {
        if (bones == null)
        {
            restRotations = new Quaternion[0];
            return;
        }

        restRotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            restRotations[i] = bones[i] != null ? bones[i].localRotation : Quaternion.identity;
        }
    }

    private void EnsureRestPose()
    {
        if (bones == null)
        {
            restRotations = new Quaternion[0];
            return;
        }

        if (restRotations == null || restRotations.Length != bones.Length)
        {
            CacheRestPose();
        }
    }
}
