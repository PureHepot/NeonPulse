using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaskSystemManager : MonoSingleton<MaskSystemManager>
{
    [Header("Gacha Settings")]
    public List<MaskConfig> maskPool;
    public int gachaCost = 0;

    [Header("Current State")]
    public MaskConfig currentMask;

    public void ApplyCurrentMaskVisuals()
    {
        if (currentMask != null)
        {
            PlayerManager.Instance.UpdatePlayerVisuals(currentMask.bodySprite, currentMask.themeColor);
        }
    }

    public bool CanAfford()
    {
        return UpgradeManager.Instance.UpgradePoints >= gachaCost;
    }

    public MaskConfig RollGacha()
    {
        if (!UpgradeManager.Instance.ConsumeUpgradePoint(gachaCost))
            return null;

        int index = Random.Range(0, maskPool.Count);
        MaskConfig result = maskPool[index];

        EquipMask(result);

        return result;
    }

    private void EquipMask(MaskConfig mask)
    {
        currentMask = mask;

        // 应用外观
        ApplyCurrentMaskVisuals();

        EventManager.Broadcast(GameEvent.PlayerSkinChanged);

        // 解锁模块
        foreach (var mod in mask.guaranteedModules)
        {
            UpgradeManager.Instance.UnlockModule(mod);
            //PlayrPreview的模块同步解锁
            EventManager.Broadcast(GameEvent.PlayerUIModelUnlock, mod);
        }

        //将不是面具带来的模块禁用
        foreach(var mod in UpgradeManager.Instance.UnlockedModuleTypes)
        {
            bool isFromMask = false;
            foreach(var m in mask.guaranteedModules)
            {
                if(mod == m)
                {
                    isFromMask = true;
                    break;
                }
            }
            if(!isFromMask)
            {
                PlayerManager.Instance.CurrentModules.DisableModule(mod);
                EventManager.Broadcast(GameEvent.PlayerUIModelLock, mod);
            }
        }

        Debug.Log($"装备面具: {mask.maskName}");
    }
}
