using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateSpawn : BossState
{
    public StateSpawn(BossAirCraft _boss) : base(_boss) { }

    public override void OnEnter()
    {
        base.OnEnter();
        boss.CleanMinionList();

        // 启动生成协程
        boss.StartCoroutine(SpawnWaveRoutine());
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        // 持续平滑移动 (不中断)
        boss.PerformSmoothHover();
    }

    IEnumerator SpawnWaveRoutine()
    {
        // 循环生成指定数量的怪
        for (int i = 0; i < boss.spawnCountPerWave; i++)
        {
            // 检查左翅膀
            if (boss.leftWing != null && !boss.leftWing.IsBroken)
            {
                boss.SpawnSingleMinion(boss.leftSpawnPoint);
            }

            // 检查右翅膀
            if (boss.rightWing != null && !boss.rightWing.IsBroken)
            {
                boss.SpawnSingleMinion(boss.rightSpawnPoint);
            }

            // 每只怪之间稍微有点间隔，更有节奏感
            yield return new WaitForSeconds(0.5f);
        }

        // 生成完毕后，稍作休息再切回 Idle
        yield return new WaitForSeconds(1.0f);
        boss.ChangeState(boss.stateIdle);
    }
}
