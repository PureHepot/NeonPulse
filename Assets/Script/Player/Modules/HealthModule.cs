using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HealthModule : PlayerModule
{
    [Header("Health Stats")]
    public int maxHp = 100;
    public int currentHp;

    [Header("Hurt Settings")]
    public float knockbackForce = 15f;
    public float stunDuration = 0.2f;
    public float invincibilityDuration = 1.0f;
    public Color hurtColor = Color.red;
    public Color normalColor = Color.white;

    private bool isInvincible = false;

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);
        maxHp = PlayerManager.Instance.MaxHealth;
        currentHp = maxHp;
    }

    public override void OnModuleUpdate() { }

    public void TakeDamage(int amount, Transform attacker)
    {
        if (isInvincible || player.IsDead) return;

        currentHp -= amount;
        PlayerManager.Instance.CurrentHp = currentHp; 

        if (currentHp <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(HurtRoutine(attacker));
    }

    IEnumerator HurtRoutine(Transform attacker)
    {
        player.IsStunned = true;
        isInvincible = true;

        PlayHurtVisuals();

        if (attacker != null)
        {
            Vector2 knockbackDir = (player.transform.position - attacker.position).normalized;
            player.Colli2d.enabled = false;
            player.Rigid2d.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        }


        yield return new WaitForSeconds(stunDuration);

        player.IsStunned = false;

        yield return new WaitForSeconds(invincibilityDuration - stunDuration);

        player.Colli2d.enabled = true;
        isInvincible = false;
        player.BodyRenderer.color = normalColor;
    }

    //受击表现
    void PlayHurtVisuals()
    {
        player.BodyRenderer.DOKill();
        player.BodyRenderer.DOColor(hurtColor, 0.05f).OnComplete(() =>
        {
            player.BodyRenderer.DOColor(normalColor, 0.2f);
        });

        player.BodyRenderer.DOFade(0.5f, 0.1f).SetLoops(5, LoopType.Yoyo);

        player.transform.DOKill();
        player.transform.DOPunchScale(new Vector3(-0.2f, 0.2f, 0), 0.2f, 10, 1);
    }

    //角色死亡
    void Die()
    {
        player.IsDead = true;
        player.SetVelocity(Vector2.zero);
        player.OnDeath?.Invoke();
        Debug.Log("Player Died");
    }

    //升级接口
    public override void UpgradeModule()
    {
        maxHp += 20;
        PlayerManager.Instance.MaxHealth = maxHp;
        PlayerManager.Instance.CurrentHp = currentHp = maxHp;
        Debug.Log($"血量升级！当前上限: {maxHp}");
    }
}
