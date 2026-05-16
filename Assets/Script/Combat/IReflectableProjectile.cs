using UnityEngine;

public interface IReflectableProjectile
{
    bool TryReflect(Vector3 reflectorPosition, Vector3 preferredTargetPosition);
}
