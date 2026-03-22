using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private Camera _mainCam;
    private float camHalfWidth;
    private float camHalfHeight;
    private float minX, maxX, minY, maxY;

    public int checkPerFrame = 10;
    private int _currentCheckIndex=0;

    [Header("配置")]
    public float border=1f;

    [Header("贴边判断范围")]
    public float borderThreshold = 0.6f;

    // 所有敌人列表
    private List<EnemyBorderEvent> enemyList = new List<EnemyBorderEvent>();

    private void Awake()
    {
        Instance = this;
        _mainCam = Camera.main;
        float camHeight = 2f * _mainCam.orthographicSize;
        float camWidth = camHeight * _mainCam.aspect;
        camHalfWidth = camWidth / 2f;
        camHalfHeight = camHeight / 2f;
    }

    // 间隔性检测所有敌人
    private void Update()
    {
        CheckEnemiesBatch();
        UpdateBounds();
    }
    private void UpdateBounds()
    {
        minX = _mainCam.transform.position.x - camHalfWidth - border;
        maxX = _mainCam.transform.position.x + camHalfWidth + border;
        minY = _mainCam.transform.position.y - camHalfHeight - border;
        maxY = _mainCam.transform.position.y + camHalfHeight + border;
    }

    // 注册敌人
    public void RegisterEnemy(EnemyBase enemy)
    {
        if (!enemyList.Contains(enemy.GetComponent<EnemyBorderEvent>()))
            enemyList.Add(enemy.GetComponent<EnemyBorderEvent>());
    }

    // 移除敌人
    public void UnRegisterEnemy(EnemyBase enemy)
    {
        if (enemyList.Contains(enemy.GetComponent<EnemyBorderEvent>()))
            enemyList.Remove(enemy.GetComponent<EnemyBorderEvent>());
    }

    //分批次检测所有敌人
    private void CheckEnemiesBatch()
    {
        if (enemyList.Count == 0)
        {
            _currentCheckIndex = 0;
            return;
        }

        // 【关键】清理已被销毁的 null 对象
        enemyList.RemoveAll(e => e == null);
        for(int i = 0; i < 10; i++)
        {
            if (enemyList.Count == 0)
            {
                _currentCheckIndex = 0;
                return;
            }

            // 获取当前要检测的敌人
            EnemyBorderEvent enemy = enemyList[_currentCheckIndex];

            // 安全判断
            if (enemy != null && !enemy.isFirstTimeEntering && IsAtBorder(enemy.transform.position))
            {
                enemy.OnBorderReached();
            }

            // 循环索引
            _currentCheckIndex = (_currentCheckIndex + 1) % enemyList.Count;
        }
        // 再次判断（清理后可能为空）
        
    }

    // 判断是否贴边
    private bool IsAtBorder(Vector2 pos)
    {
        return pos.x <= minX + borderThreshold ||
               pos.x >= maxX - borderThreshold ||
               pos.y <= minY + borderThreshold ||
               pos.y >= maxY - borderThreshold;
    }
}
