using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int amount);
    void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal);
}
