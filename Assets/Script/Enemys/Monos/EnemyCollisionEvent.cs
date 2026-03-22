using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCollisionEvent : EnemyBorderEvent
{
    private EnemyBase enemy;
    private Camera _mainCam;
    private float camHalfWidth;
    private float camHalfHeight;
    private float minX, maxX, minY, maxY;
    private GameObject enemyObj;
    private bool isAlready=false;
    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        
        _mainCam = Camera.main;
        float camHeight = 2f * _mainCam.orthographicSize;
        float camWidth = camHeight * _mainCam.aspect;
        camHalfWidth = camWidth / 2f;
        camHalfHeight = camHeight / 2f;
        enemyObj = enemy.gameObject;
        UpdateBounds();
    }
    
    private void UpdateBounds()
    {
        minX = _mainCam.transform.position.x - camHalfWidth ;
        maxX = _mainCam.transform.position.x + camHalfWidth ;
        minY = _mainCam.transform.position.y - camHalfHeight ;
        maxY = _mainCam.transform.position.y + camHalfHeight ;
    }
    private void Update()
    {
        if (enemy.isInScene)
        {
            isAlready = true;
        }
        if (isAlready)
        {
            OnUpdate();
        }
    }
    public override void OnUpdate()
    {
        if (EventAdmitted())
        {
            Transform enemyTransform = enemyObj.transform;
            Rigidbody2D enemyRb = enemyObj.GetComponent<Rigidbody2D>();
            if (enemyTransform == null || enemyRb == null) return;

            Vector2 currentPos = enemyRb.position;
            float clampedX = Mathf.Clamp(currentPos.x, minX, maxX);
            float clampedY = Mathf.Clamp(currentPos.y, minY, maxY);
            enemyRb.position = new Vector2(clampedX, clampedY);
        }
        
    }
    protected override bool EventAdmitted()
    {
        return true;
    }
}
