using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class VocalistBerserkState : BossBaseState
{
    private VocalistBoss vocalistBoss;
    private int subPhase;
    private float phaseTimer;
    private float cloneWaveTimer;
    private float cloneShotTimer;
    private Vector2 chargeDirection;
    private readonly List<Vector2> pendingCloneSides = new List<Vector2>();

    public override void Enter(BossBase context)
    {
        base.Enter(context);
        vocalistBoss = context as VocalistBoss;
        subPhase = 0;
        phaseTimer = 0f;
        cloneWaveTimer = 0f;
        cloneShotTimer = 0f;
        pendingCloneSides.Clear();

        if (vocalistBoss == null) return;

        vocalistBoss.isBerserk = true;
        vocalistBoss.transform.DOKill();
        vocalistBoss.transform.DOPunchScale(vocalistBoss.transform.localScale * 0.25f, 0.35f, 8, 0.8f);
        StartCharge();
        StartCloneWave();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (vocalistBoss == null) return;

        UpdateChargeMovement();
        UpdateCloneWaves();
    }

    public override void Exit()
    {
        if (vocalistBoss != null) vocalistBoss.isBerserk = false;
    }

    private void UpdateChargeMovement()
    {
        phaseTimer += Time.deltaTime;

        if (subPhase == 0)
        {
            vocalistBoss.transform.position += (Vector3)(chargeDirection * vocalistBoss.berserkChargeSpeed * Time.deltaTime);
            RotateTowards(chargeDirection);

            if (phaseTimer >= vocalistBoss.berserkChargeDuration)
            {
                subPhase = 1;
                phaseTimer = 0f;
            }
        }
        else
        {
            Vector2 roamDir = GetDirectionToPlayer();
            if (roamDir.sqrMagnitude < 0.001f) roamDir = Random.insideUnitCircle.normalized;

            vocalistBoss.transform.position += (Vector3)(roamDir * vocalistBoss.berserkMoveSpeed * Time.deltaTime);
            RotateTowards(roamDir);

            if (phaseTimer >= vocalistBoss.berserkRecoverDuration)
            {
                StartCharge();
            }
        }

        ClampToArena();
    }

    private void UpdateCloneWaves()
    {
        cloneWaveTimer += Time.deltaTime;

        if (pendingCloneSides.Count == 0 && cloneWaveTimer >= vocalistBoss.cloneWaveInterval)
        {
            StartCloneWave();
        }

        if (pendingCloneSides.Count == 0) return;

        cloneShotTimer -= Time.deltaTime;
        if (cloneShotTimer > 0f) return;

        Vector2 side = pendingCloneSides[0];
        pendingCloneSides.RemoveAt(0);
        SpawnCloneFromSide(side);
        cloneShotTimer = vocalistBoss.cloneSequentialDelay;
    }

    private void StartCharge()
    {
        subPhase = 0;
        phaseTimer = 0f;
        chargeDirection = GetDirectionToPlayer();
        if (chargeDirection.sqrMagnitude < 0.001f) chargeDirection = Random.insideUnitCircle.normalized;
        if (chargeDirection.sqrMagnitude < 0.001f) chargeDirection = Vector2.right;
    }

    private void StartCloneWave()
    {
        cloneWaveTimer = 0f;
        cloneShotTimer = 0f;
        pendingCloneSides.Clear();

        pendingCloneSides.Add(Vector2.up);
        pendingCloneSides.Add(Vector2.down);
        pendingCloneSides.Add(Vector2.left);
        pendingCloneSides.Add(Vector2.right);

        for (int i = 0; i < pendingCloneSides.Count; i++)
        {
            int swapIndex = Random.Range(i, pendingCloneSides.Count);
            Vector2 temp = pendingCloneSides[i];
            pendingCloneSides[i] = pendingCloneSides[swapIndex];
            pendingCloneSides[swapIndex] = temp;
        }
    }

    private void SpawnCloneFromSide(Vector2 side)
    {
        Vector2 half = vocalistBoss.arenaHalfSize;
        Vector2 spawnPos;

        if (side == Vector2.up)
        {
            spawnPos = new Vector2(Random.Range(-half.x, half.x), half.y);
        }
        else if (side == Vector2.down)
        {
            spawnPos = new Vector2(Random.Range(-half.x, half.x), -half.y);
        }
        else if (side == Vector2.left)
        {
            spawnPos = new Vector2(-half.x, Random.Range(-half.y, half.y));
        }
        else
        {
            spawnPos = new Vector2(half.x, Random.Range(-half.y, half.y));
        }

        Vector2 direction = GetDirectionFromSpawn(spawnPos, side);
        vocalistBoss.SpawnDrillClone(spawnPos, direction);
    }

    private Vector2 GetDirectionFromSpawn(Vector2 spawnPos, Vector2 side)
    {
        if (vocalistBoss.playerTarget != null)
        {
            Vector2 toPlayer = (Vector2)vocalistBoss.playerTarget.position - spawnPos;
            if (toPlayer.sqrMagnitude > 0.001f) return toPlayer.normalized;
        }

        return -side.normalized;
    }

    private Vector2 GetDirectionToPlayer()
    {
        if (vocalistBoss.playerTarget == null) return Vector2.zero;
        return ((Vector2)vocalistBoss.playerTarget.position - (Vector2)vocalistBoss.transform.position).normalized;
    }

    private void RotateTowards(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        vocalistBoss.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void ClampToArena()
    {
        Vector3 pos = vocalistBoss.transform.position;
        pos.x = Mathf.Clamp(pos.x, -vocalistBoss.arenaHalfSize.x, vocalistBoss.arenaHalfSize.x);
        pos.y = Mathf.Clamp(pos.y, -vocalistBoss.arenaHalfSize.y, vocalistBoss.arenaHalfSize.y);
        vocalistBoss.transform.position = pos;
    }
}
