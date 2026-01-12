using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    void Start()
    {
        EventManager.AddListener<Vector2>(GameEvent.PlayerScaleChange, (scale) =>
        {
            transform.localScale = new Vector3(scale.x, scale.y, 1);
            Debug.Log($"玩家缩放到: {scale}");
        });

        CameraManager.Instance.Follow(transform, false);

        CameraManager.Instance.FocusOn(GameObject.Find("Square").transform, 1.2f);

        Timer.Register(1f, () =>
        {
            Debug.Log("1s到达");
            transform.position += new Vector3(0, 2, 0);

            AudioManager.Instance.PlayBGM("MainTheme");
        });


        Timer.Register(2f,() => 
        {
            Debug.Log("2s到达");
            Timer.Register(2f,
            onComplete: () => {
                Debug.Log("蓄力完成");
                EventManager.Broadcast(GameEvent.PlayerScaleChange, new Vector2(2f, 2f));
            },
            onUpdate: (percent) => {
                transform.position = new Vector3(0, 2, 0) * percent;
            });
        });

        
    }

    private void OnDestroy()
    {

    }

    void Update()
    {
        
    }
}
