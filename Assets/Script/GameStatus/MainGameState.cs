using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainGameState : GameState
{

    public override void OnEnter()
    {
        Time.timeScale = 1f;
        StartGame();
    }

    public override void OnExit()
    {
        
    }

    public override void OnUpdate()
    {
        
    }


    private void StartGame()
    {
        GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Mono/Player"));
    }
}
