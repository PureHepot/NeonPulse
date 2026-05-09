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

    private List<ModuleType> Mods = new();//临时存

    public void ApplyCurrentMaskVisuals()
    {
        if (currentMask != null)
        {
            PlayerManager.Instance.UpdatePlayerVisuals(currentMask.bodySprite, currentMask.themeColor);
            HealthModule health = PlayerManager.Instance.CurrentModules.GetModule<HealthModule>(ModuleType.Health);
            health.normalColor = currentMask.themeColor;
            health.hurtColor = Color.white - currentMask.themeColor;
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

    /// <summary>
    /// 从存档恢复面具（根据名称匹配）
    /// </summary>
    public void InitFromSaveData()
    {
        var run = DataManager.Instance.Run;
        if (run == null || string.IsNullOrEmpty(run.build.currentMaskName)) return;

        foreach (var mask in maskPool)
        {
            if (mask.maskName == run.build.currentMaskName)
            {
                currentMask = mask;
                return;
            }
        }
    }

    private void EquipMask(MaskConfig mask)
    {
        currentMask = mask;

        // 同步到存档
        var run = DataManager.Instance.Run;
        if (run != null)
            run.build.currentMaskName = mask.maskName;

        // 应用外观
        ApplyCurrentMaskVisuals();

        EventManager.Broadcast(GameEvent.PlayerSkinChanged);

        // 解锁模块
        foreach (var Mod in mask.guaranteedModules)
        {
            UpgradeManager.Instance.UnlockModule(Mod);
            //PlayrPreview的模块同步解锁
            EventManager.Broadcast(GameEvent.PlayerUIModelUnlock, Mod);
        }

        //将不是面具带来的模块禁用
        Mods.Clear();
        foreach (var Mod in UpgradeManager.Instance.UnlockedModuleTypes)
        {
            bool isFromMask = false;
            foreach(var m in mask.guaranteedModules)
            {
                if(Mod == m)
                {
                    isFromMask = true;
                    break;
                }
            }
            if(!isFromMask)
            {
                Mods.Add(Mod);
                EventManager.Broadcast(GameEvent.PlayerUIModelLock, Mod);
            }
        }

        foreach (var Mod in Mods)
        {
            UpgradeManager.Instance.GainUpgradePointByModule(Mod);
            UpgradeManager.Instance.ResetLevel(Mod);
            UpgradeManager.Instance.LockModule(Mod);

        }


        Debug.Log($"装备面具: {mask.maskName}");
    }
}
