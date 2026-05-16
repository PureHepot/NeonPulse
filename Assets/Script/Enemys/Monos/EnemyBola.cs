using System.Collections.Generic;
using UnityEngine;

public class EnemyBola : EnemyBase
{
    [Header("Movement")]
    public float innerHoldRadius = 2.2f;
    public float outerHoldRadius = 3.6f;
    public float followSmooth = 8f;

    [Header("Attack Cycle")]
    public float firstShootDelay = 1f;
    public float shootInterval = 1.5f;
    public float regenerateDelay = 2f;

    [Header("Nail Launch")]
    public Transform group;
    public float nailSpeed = 14f;
    public int nailDamage = 1;
    public float nailLifeTime = 6f;

    private readonly List<Vector3> slotLocalPositions = new List<Vector3>();
    private readonly List<Quaternion> slotLocalRotations = new List<Quaternion>();
    private readonly List<EnemyNail> armedNails = new List<EnemyNail>();

    private EnemyNail nailPrefab;
    private EnemyNail nailTemplate;
    private float shootTimer;
    private float regenerateTimer;
    private bool pendingRegenerate;

    protected override void Awake()
    {
        base.Awake();
        AutoBindGroup();
        CacheNailSlots();
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        AutoBindGroup();
        CacheNailSlots();

        EnsureNailsReady();

        pendingRegenerate = false;
        regenerateTimer = 0f;
        shootTimer = firstShootDelay;
    }

    protected override void MoveBehavior()
    {
        if (playerTransform == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        FollowAndHoldInRing();
        UpdateAttackCycle();
    }

    private void FollowAndHoldInRing()
    {
        Vector2 toPlayer = playerTransform.position - transform.position;
        float dist = toPlayer.magnitude;
        float inner = Mathf.Min(innerHoldRadius, outerHoldRadius);
        float outer = Mathf.Max(innerHoldRadius, outerHoldRadius);

        if (dist > outer)
        {
            Vector2 desiredVelocity = toPlayer.normalized * moveSpeed;
            rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, followSmooth * Time.fixedDeltaTime);
        }
        else if (dist < inner)
        {
            Vector2 awayFromPlayer = (-toPlayer).normalized;
            Vector2 desiredVelocity = awayFromPlayer * moveSpeed;
            rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, followSmooth * Time.fixedDeltaTime);
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, followSmooth * Time.fixedDeltaTime);
        }
    }

    private void UpdateAttackCycle()
    {
        if (pendingRegenerate)
        {
            regenerateTimer -= Time.fixedDeltaTime;
            if (regenerateTimer <= 0f)
            {
                pendingRegenerate = false;
                RegenerateNails();
                shootTimer = shootInterval;
            }

            return;
        }

        if (armedNails.Count == 0)
        {
            RegenerateNails();
            shootTimer = shootInterval;
            return;
        }

        shootTimer -= Time.fixedDeltaTime;
        if (shootTimer <= 0f && IsNearPlayer())
        {
            FireAllNails();
            shootTimer = shootInterval;
        }
    }

    private bool IsNearPlayer()
    {
        if (playerTransform == null) return false;
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        float inner = Mathf.Min(innerHoldRadius, outerHoldRadius);
        float outer = Mathf.Max(innerHoldRadius, outerHoldRadius);
        return dist >= inner && dist <= outer;
    }

    private void FireAllNails()
    {
        for (int i = armedNails.Count - 1; i >= 0; i--)
        {
            EnemyNail nail = armedNails[i];
            if (nail == null) continue;

            nail.Launch(nailSpeed, nailDamage, nailLifeTime, transform);
        }

        armedNails.Clear();
        pendingRegenerate = true;
        regenerateTimer = regenerateDelay;
    }

    private void RegenerateNails()
    {
        EnemyNail source = nailTemplate != null ? nailTemplate : nailPrefab;
        if (group == null || source == null || slotLocalPositions.Count == 0)
        {
            return;
        }

        armedNails.Clear();
        for (int i = 0; i < slotLocalPositions.Count; i++)
        {
            GameObject nailObj = Instantiate(source.gameObject, group);
            nailObj.transform.localPosition = slotLocalPositions[i];
            nailObj.transform.localRotation = slotLocalRotations[i];
            nailObj.SetActive(true);

            EnemyNail nail = nailObj.GetComponent<EnemyNail>();
            if (nail != null)
            {
                nail.ResetProjectile();
                armedNails.Add(nail);
            }
        }
    }

    private void EnsureNailsReady()
    {
        armedNails.Clear();

        if (group == null) return;

        for (int i = group.childCount - 1; i >= 0; i--)
        {
            Transform child = group.GetChild(i);
            EnemyNail nail = child.GetComponent<EnemyNail>();
            if (nail == null) continue;

            nail.ResetProjectile();
            armedNails.Add(nail);
        }

        if (armedNails.Count == 0)
        {
            RegenerateNails();
        }
    }

    private void AutoBindGroup()
    {
        if (group == null)
        {
            Transform found = transform.Find("group");
            if (found != null) group = found;
        }
    }

    private void CacheNailSlots()
    {
        if (nailPrefab != null && slotLocalPositions.Count > 0 && slotLocalRotations.Count == slotLocalPositions.Count)
        {
            return;
        }

        slotLocalPositions.Clear();
        slotLocalRotations.Clear();

        if (group == null) return;

        for (int i = 0; i < group.childCount; i++)
        {
            Transform child = group.GetChild(i);
            EnemyNail nail = child.GetComponent<EnemyNail>();
            if (nail == null) continue;

            if (nailPrefab == null)
            {
                nailPrefab = nail;
                CreateRuntimeNailTemplate(nailPrefab);
            }
            slotLocalPositions.Add(child.localPosition);
            slotLocalRotations.Add(child.localRotation);
        }
    }

    private void CreateRuntimeNailTemplate(EnemyNail source)
    {
        if (source == null || nailTemplate != null) return;

        GameObject templateObj = Instantiate(source.gameObject, transform);
        templateObj.name = source.gameObject.name + "_TemplateRuntime";
        templateObj.SetActive(false);
        nailTemplate = templateObj.GetComponent<EnemyNail>();
    }
}
