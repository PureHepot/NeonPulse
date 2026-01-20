using UnityEngine;

public class BattleFieldLimit : MonoBehaviour
{
    [Header("配置")]
    private string playerTag = "Player";
    public Vector2 margin = new Vector2(0.5f, 0.5f); 

    private Camera _mainCam;
    private float camHalfWidth;
    private float camHalfHeight;
    private float _minX, _maxX, _minY, _maxY;

    private Transform _playerTransform;
    private Rigidbody2D _playerRb;

    private void Awake()
    {
        _mainCam = Camera.main;
        float camHeight = 2f * _mainCam.orthographicSize;
        float camWidth = camHeight * _mainCam.aspect;
        camHalfWidth = camWidth / 2f;
        camHalfHeight = camHeight / 2f;
        UpdateBounds();
    }

    private void FixedUpdate()
    {
        UpdateBounds(); // 实时更新边界
        FindPlayer();   // 查找玩家
        ClampPlayerPos(); // 限制玩家位置
    }

    /// <summary>
    /// 计算相机边界
    /// </summary>
    private void UpdateBounds()
    {
        _minX = _mainCam.transform.position.x - camHalfWidth + margin.x;
        _maxX = _mainCam.transform.position.x + camHalfWidth - margin.x;
        _minY = _mainCam.transform.position.y - camHalfHeight + margin.y;
        _maxY = _mainCam.transform.position.y + camHalfHeight - margin.y;
    }

    /// <summary>
    /// 通过Tag查找玩家
    /// </summary>
    private void FindPlayer()
    {
        if (_playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag(playerTag);
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
                _playerRb = playerObj.GetComponent<Rigidbody2D>();
            }
        }
    }

    /// <summary>
    /// 限制玩家位置在边界内
    /// </summary>
    private void ClampPlayerPos()
    {
        if (_playerTransform == null || _playerRb == null) return;

        Vector2 currentPos = _playerRb.position;
        float clampedX = Mathf.Clamp(currentPos.x, _minX, _maxX);
        float clampedY = Mathf.Clamp(currentPos.y, _minY, _maxY);
        _playerRb.position = new Vector2(clampedX, clampedY);
    }

    // 绘制边界
    private void OnDrawGizmos()
    {
        if (_mainCam == null) _mainCam = Camera.main;
        if (_mainCam == null) return;

        float camHeight = 2f * _mainCam.orthographicSize;
        float camWidth = camHeight * _mainCam.aspect;
        float halfW = camWidth / 2f;
        float halfH = camHeight / 2f;
        float minX = _mainCam.transform.position.x - halfW + margin.x;
        float maxX = _mainCam.transform.position.x + halfW - margin.x;
        float minY = _mainCam.transform.position.y - halfH + margin.y;
        float maxY = _mainCam.transform.position.y + halfH - margin.y;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_mainCam.transform.position, new Vector3(maxX - minX, maxY - minY, 0));
    }
}