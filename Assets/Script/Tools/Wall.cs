using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*public class Wall : MonoBehaviour
{
    private HashSet<Collider2D> needTeleportEnemies = new HashSet<Collider2D>();

    

    private void OnCollisionEnter2D(Collision2D other)
    {
        EnemyBase enemy = other.gameObject.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            if (!enemy.isInScene)
            {
                Physics2D.IgnoreCollision(other.collider, GetComponent<Collider2D>(), true);
            }
            if(enemy.isInScene && enemy.PassWallAdmitted())
            {
                Physics2D.IgnoreCollision(other.collider, GetComponent<Collider2D>(), true);
                needTeleportEnemies.Add(other.collider);
            }
        }
    }
    private void OnCollisionExit2D(Collision2D other)
    {
        EnemyBase enemy = other.gameObject.GetComponent<EnemyBase>();
        if (enemy == null) return;
        if (enemy.isInScene)
        {
            Physics2D.IgnoreCollision(other.collider, GetComponent<Collider2D>(), false);
        }
        if (needTeleportEnemies.Contains(other.collider))
        {
            needTeleportEnemies.Remove(other.collider);
            enemy.OnExitWallAndCheckOutView();
        }
    }
}*/
