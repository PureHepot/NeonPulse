using UnityEngine;

public class ArenaTrap : MonoBehaviour
{
    [Header("Layer Settings")]
    public string spawningLayerName = "EnemySpawning";
    public string combatLayerName = "Enemy";

    private int spawningLayer;
    private int combatLayer;

    void Awake()
    {
        spawningLayer = LayerMask.NameToLayer(spawningLayerName);
        combatLayer = LayerMask.NameToLayer(combatLayerName);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == spawningLayer)
        {
            if (other.GetComponent<EnemyBase>() != null)
            {
                other.gameObject.layer = combatLayer;
            }
        }
    }
}