using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("边界检测")]
    public int checkPerFrame = 10;

    [Header("贴边判断范围")]
    public float borderThreshold = 0.6f;

    private readonly EnemyBoundaryService boundaryService = new();

    private void Awake()
    {
        Instance = this;
        boundaryService.Configure(checkPerFrame, borderThreshold);
    }

    private void Update()
    {
        boundaryService.Configure(checkPerFrame, borderThreshold);
        boundaryService.Tick(Time.deltaTime, Camera.main);
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        boundaryService.RegisterEnemy(enemy);
    }

    public void UnRegisterEnemy(EnemyBase enemy)
    {
        boundaryService.UnregisterEnemy(enemy);
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;

        boundaryService.Reset();
    }
}
