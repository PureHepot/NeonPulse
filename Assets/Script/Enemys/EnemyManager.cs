using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private Camera _mainCam;
    private float camHalfWidth;
    private float camHalfHeight;
    private float minX;
    private float maxX;
    private float minY;
    private float maxY;

    public int checkPerFrame = 10;
    private int _currentCheckIndex = 0;

    [Header("配置")]
    public float border = 1f;

    [Header("贴边判断范围")]
    public float borderThreshold = 0.6f;

    // 所有敌人列表
    private readonly List<EnemyBorderEvent> enemyList = new();

    private void Awake()
    {
        Instance = this;
        CacheCamera();
    }

    private void Update()
    {
        UpdateBounds();
        CheckEnemiesBatch();
    }

    private void CacheCamera()
    {
        _mainCam = Camera.main;
        if (_mainCam == null)
            return;

        float camHeight = 2f * _mainCam.orthographicSize;
        float camWidth = camHeight * _mainCam.aspect;
        camHalfWidth = camWidth / 2f;
        camHalfHeight = camHeight / 2f;
    }

    private void UpdateBounds()
    {
        if (_mainCam == null)
        {
            CacheCamera();
            if (_mainCam == null)
                return;
        }

        minX = _mainCam.transform.position.x - camHalfWidth - border;
        maxX = _mainCam.transform.position.x + camHalfWidth + border;
        minY = _mainCam.transform.position.y - camHalfHeight - border;
        maxY = _mainCam.transform.position.y + camHalfHeight + border;
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        EnemyBorderEvent borderEvent = enemy.GetComponent<EnemyBorderEvent>();
        if (borderEvent == null)
            return;

        if (!enemyList.Contains(borderEvent))
            enemyList.Add(borderEvent);
    }

    public void UnRegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        EnemyBorderEvent borderEvent = enemy.GetComponent<EnemyBorderEvent>();
        if (borderEvent == null)
            return;

        enemyList.Remove(borderEvent);
        if (_currentCheckIndex >= enemyList.Count)
            _currentCheckIndex = 0;
    }

    private void CheckEnemiesBatch()
    {
        if (_mainCam == null)
            return;

        if (enemyList.Count == 0)
        {
            _currentCheckIndex = 0;
            return;
        }

        enemyList.RemoveAll(e => e == null);
        if (enemyList.Count == 0)
        {
            _currentCheckIndex = 0;
            return;
        }

        if (_currentCheckIndex >= enemyList.Count)
            _currentCheckIndex = 0;

        int checksThisFrame = Mathf.Min(Mathf.Max(1, checkPerFrame), enemyList.Count);
        for (int i = 0; i < checksThisFrame; i++)
        {
            if (enemyList.Count == 0)
            {
                _currentCheckIndex = 0;
                return;
            }

            if (_currentCheckIndex >= enemyList.Count)
                _currentCheckIndex = 0;

            EnemyBorderEvent enemy = enemyList[_currentCheckIndex];
            if (enemy != null && !enemy.isFirstTimeEntering && IsAtBorder(enemy.transform.position))
                enemy.OnBorderReached();

            _currentCheckIndex = (_currentCheckIndex + 1) % enemyList.Count;
        }
    }

    private bool IsAtBorder(Vector2 pos)
    {
        return pos.x <= minX + borderThreshold ||
               pos.x >= maxX - borderThreshold ||
               pos.y <= minY + borderThreshold ||
               pos.y >= maxY - borderThreshold;
    }
}
