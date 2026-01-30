using UnityEngine;

public class DisturbAura : EnemyAuraBase
{
    [Header("Disturb Settings")]
    public float maxShakeStrength = 3f;
    public float minShakeStrength = 2f;

    public float maxInterval = 0.6f;
    public float minInterval = 0.2f;

    public float maxRange = 4f;

    private float timer;

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(player.position, transform.position);
        float t = Mathf.InverseLerp(maxRange, 0.5f, dist);

        float shakeStrength = Mathf.Lerp(minShakeStrength, maxShakeStrength, t);
        float interval = Mathf.Lerp(maxInterval, minInterval, t);

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = interval;
            CameraManager.Instance.ShakeSimple(shakeStrength, 0.12f);
        }
    }

    protected override void OnPlayerEnter() { }

    protected override void OnPlayerExit()
    {
        timer = 0f;
    }
}
