using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MUtils
{
    public static Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        return new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0);
    }

    public static int RandomPulseFunc()
    {
        return UnityEngine.Random.Range(0, 2) <= 0 ? -1 : 1;
    }

    public static void DestroyChildren(this Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject.Destroy(parent.GetChild(i).gameObject);
        }
    }

}
