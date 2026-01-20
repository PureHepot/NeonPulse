using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ShooterModule : PlayerModule
{
    [Header("Hierarchy Refs")]
    public Transform partToRotate;

    //拖入muzzles: index 0=中, 1=左, 2=右
    public List<Transform> muzzles;
    public GameObject bulletPrefab;

    [Header("Rotation Settings")]
    public float rotationSpeed = 15f;

    [Header("Combat Settings")]
    public float fireRate = 0.8f;
    public float sequenceDelay = 0.1f;
    public int damage => PlayerManager.Instance.BulletDamage;

    [Header("State")]
    public int currentLevel = 1;

    private float globalCooldown = 0f;
    private bool isFiring = false;

    private List<float> muzzleVisualProgress = new List<float>();

    //鼠标位置
    private Vector3 targetPos;

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);
        partToRotate.gameObject.SetActive(true);
        muzzleVisualProgress.Clear();
        foreach (var m in muzzles)
        {
            muzzleVisualProgress.Add(1f);
        }
    }

    public override void OnModuleUpdate()
    {
        HandleRotation();

        HandleReloadVisuals();

        if (globalCooldown > 0) globalCooldown -= Time.deltaTime;

        if (InputManager.Instance.Mouse0() && !isFiring && globalCooldown <= 0)
        {
            StartCoroutine(FireSequenceRoutine());
        }
    }

    void HandleRotation()
    {
        if (partToRotate == null) return;

        Vector3 mousePos = MUtils.GetMouseWorldPosition();

        Vector2 direction = mousePos - partToRotate.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);

        partToRotate.rotation = Quaternion.Slerp(partToRotate.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void HandleReloadVisuals()
    {
        float recoverSpeed = 1f / fireRate;

        for (int i = 0; i < muzzles.Count; i++)
        {
            if (muzzleVisualProgress[i] < 1f)
            {
                muzzleVisualProgress[i] += Time.deltaTime * recoverSpeed;
                if (muzzleVisualProgress[i] > 1f) muzzleVisualProgress[i] = 1f;
            }

            bool isActive = false;
            if (currentLevel == 1 && i == 0) isActive = true;
            if (currentLevel >= 2 && i <= 2) isActive = true;

            if (isActive)
            {
                //如果没激活就激活
                if (!muzzles[i].gameObject.activeSelf) muzzles[i].gameObject.SetActive(true);

                //改scale
                float finalScale = DOVirtual.EasedValue(0, 1, muzzleVisualProgress[i], Ease.OutBack); ;

                muzzles[i].localScale = Vector3.one * finalScale;
            }
            else
            {
                if (muzzles[i].gameObject.activeSelf) muzzles[i].gameObject.SetActive(false);
            }
        }
    }

    IEnumerator FireSequenceRoutine()
    {
        isFiring = true;

        globalCooldown = fireRate;

        List<int> activeIndices = new List<int>();

        if (currentLevel == 1)
        {
            if (muzzles.Count > 0) activeIndices.Add(0); // Center
        }
        else
        {
            if (muzzles.Count > 0) activeIndices.Add(0);
            if (muzzles.Count > 1) activeIndices.Add(1);
            if (muzzles.Count > 2) activeIndices.Add(2);
        }

        foreach (int index in activeIndices)
        {
            Transform muzzleT = muzzles[index];

            muzzleVisualProgress[index] = 0f;

            muzzleT.localScale = Vector3.one * 1.5f; // 瞬间变大
               yield return new WaitForSeconds(0.05f);  // 停顿
               SpawnBullet(muzzleT);
               muzzleVisualProgress[index] = 0f; // 然后变没

            SpawnBullet(muzzleT);

            if (activeIndices.Count > 1) yield return new WaitForSeconds(sequenceDelay);
        }

        isFiring = false;
    }

    void SpawnBullet(Transform muzzlePoint)
    {
        GameObject bullet = ObjectPoolManager.Instance.Get(
            bulletPrefab,
            muzzlePoint.position,
            muzzlePoint.rotation
        );

        PlayerBullet bulletScript = bullet.GetComponent<PlayerBullet>();
        if (bulletScript) bulletScript.damage = this.damage;
    }

    public void UpgradeShot()
    {
        currentLevel++;
    }

}
