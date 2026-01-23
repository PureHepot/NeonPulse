using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount);
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
}
