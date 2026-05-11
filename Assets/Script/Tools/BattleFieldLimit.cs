using UnityEngine;

public class BattleFieldLimit : MonoBehaviour
{
    [Header("Config")]
    private string playerTag = "Player";
    public Vector2 margin = new Vector2(0.5f, 0.5f);

    private Camera _mainCam;
    private float camHalfWidth;
    private float camHalfHeight;
    private float _minX, _maxX, _minY, _maxY;

    private Transform _playerTransform;
    private Rigidbody2D _playerRb;

    public GameObject leftWall;
    public GameObject rightWall;
    public GameObject topWall;
    public GameObject bottomWall;
    public GameObject ArenaTrap;
    private float wallThickness = 1f;

    private void Awake()
    {
        _mainCam = Camera.main;
        UpdateBounds();
    }

    private void FixedUpdate()
    {
        UpdateBounds(); // Refresh bounds from the current camera size.
        FindPlayer();
        ClampPlayerPos();
        UpdateAllWalls();
        UpdateArenaTrap(ArenaTrap);
    }

    /// <summary>
    /// Calculate camera-based bounds.
    /// </summary>
    private void UpdateBounds()
    {
        if (_mainCam == null)
            _mainCam = Camera.main;

        if (_mainCam == null)
            return;

        float camHeight = 2f * _mainCam.orthographicSize;
        float camWidth = camHeight * _mainCam.aspect;
        camHalfWidth = camWidth / 2f;
        camHalfHeight = camHeight / 2f;

        _minX = _mainCam.transform.position.x - camHalfWidth + margin.x;
        _maxX = _mainCam.transform.position.x + camHalfWidth - margin.x;
        _minY = _mainCam.transform.position.y - camHalfHeight + margin.y;
        _maxY = _mainCam.transform.position.y + camHalfHeight - margin.y;
    }

    /// <summary>
    /// Find the player by tag.
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
    /// Clamp the player inside the current bounds.
    /// </summary>
    private void ClampPlayerPos()
    {
        if (_playerTransform == null || _playerRb == null) return;

        Vector2 currentPos = _playerRb.position;
        float clampedX = Mathf.Clamp(currentPos.x, _minX, _maxX);
        float clampedY = Mathf.Clamp(currentPos.y, _minY, _maxY);
        _playerRb.position = new Vector2(clampedX, clampedY);
    }

    // Draw the current playable bounds.
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

    private void UpdateAllWalls()
    {
        // Only update walls that are assigned.
        if (leftWall != null) UpdateWall(leftWall, WallSide.Left);
        if (rightWall != null) UpdateWall(rightWall, WallSide.Right);
        if (topWall != null) UpdateWall(topWall, WallSide.Top);
        if (bottomWall != null) UpdateWall(bottomWall, WallSide.Bottom);
    }

    private enum WallSide { Left, Right, Top, Bottom }

    private void UpdateWall(GameObject wall, WallSide side)
    {
        BoxCollider2D col = wall.GetComponent<BoxCollider2D>();

        switch (side)
        {
            case WallSide.Left:
                wall.transform.position = new Vector3(_minX - wallThickness / 2, _mainCam.transform.position.y, 0);
                if (col != null) col.size = new Vector2(wallThickness, _maxY - _minY);
                break;

            case WallSide.Right:
                wall.transform.position = new Vector3(_maxX + wallThickness / 2, _mainCam.transform.position.y, 0);
                if (col != null) col.size = new Vector2(wallThickness, _maxY - _minY);
                break;

            case WallSide.Top:
                wall.transform.position = new Vector3(_mainCam.transform.position.x, _maxY + wallThickness / 2, 0);
                if (col != null) col.size = new Vector2(wallThickness, camHalfWidth * 2);
                break;

            case WallSide.Bottom:
                wall.transform.position = new Vector3(_mainCam.transform.position.x, _minY - wallThickness / 2, 0);
                if (col != null) col.size = new Vector2(wallThickness, camHalfWidth * 2);
                break;
        }
    }

    private void UpdateArenaTrap(GameObject trap)
    {
        if (trap == null) return;

        BoxCollider2D box = trap.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.transform.position = new Vector3(_mainCam.transform.position.x, _mainCam.transform.position.y, 0);
            box.size = new Vector2(_maxX - _minX, _maxY - _minY);
        }
    }
}
