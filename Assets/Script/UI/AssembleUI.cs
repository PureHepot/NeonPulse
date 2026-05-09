using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AssembleUI : UIBase
{
    #region Fields

    private readonly List<FrameSlotButton> activeSlots = new();
    private readonly List<FrameConfig> frames = new();
    private readonly List<ModuleConfig> filteredModules = new();
    private readonly List<CoreConfig> filteredCores = new();

    private readonly Dictionary<string, CoreConfig> coreLookup = new();
    private int frameIndex;
    private FrameConfig currentFrame;
    private GameObject currentSlotLayout;
    private FrameSlotButton selectedSlot;
    private string selectedModuleId;

    private Transform moduleContentRoot;
    private Transform coreContentRoot;

    #endregion

    #region Lifecycle

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        var db = GameConfigDatabase.Instance;
        if (db == null || db.allFrames == null)
            return;

        frames.Clear();
        coreLookup.Clear();
        if (db.allCores != null)
        {
            foreach (var core in db.allCores)
            {
                if (core != null)
                    coreLookup[core.coreId] = core;
            }
        }

        foreach (var frame in db.allFrames)
        {
            if (frame != null && GameMgr.Instance.Data.Meta.IsFrameUnlocked(frame.frameId))
                frames.Add(frame);
        }

        if (frames.Count == 0)
            return;

        moduleContentRoot = Get<Transform>("ModuleContent");
        coreContentRoot = Get<Transform>("CoreContent");

        string selectedFrameId = GameMgr.Instance.Data.Meta.GetSelectedFrameId();
        frameIndex = frames.FindIndex(frame => frame.frameId == selectedFrameId);
        if (frameIndex < 0)
            frameIndex = 0;

        BindButtons();
        ShowFrame(frameIndex);
    }

    public override void OnClose()
    {
        CleanupCurrentFrameDisplay();
        GameMgr.Instance.Preview.HideAssemblyPreview();
        base.OnClose();
        Save();
    }

    #endregion

    #region Button Binding

    private void BindButtons()
    {
        Get<Button>("BtnPrevFrame").onClick.SetListener(() => ShowFrame(frameIndex - 1));
        Get<Button>("BtnNextFrame").onClick.SetListener(() => ShowFrame(frameIndex + 1));
        Get<Button>("BackBtn").onClick.SetListener(() => GameMgr.Instance.Game.ChangeState(new MenuState()));
        Get<Button>("Finish").onClick.SetListener(() =>
        {
            if (currentFrame == null)
                return;

            GameMgr.Instance.Loadout.SelectFrame(currentFrame.frameId);
            Save();

            Action action = () => GameMgr.Instance.Game.ChangeState(new MainGameState(false));
            GameMgr.Instance.UI.Open<LoadingUI>(action);
        });
    }

    #endregion

    #region Frame Display

    private void ShowFrame(int index)
    {
        if (frames.Count == 0)
            return;

        frameIndex = (index + frames.Count) % frames.Count;
        currentFrame = frames[frameIndex];
        selectedSlot = null;
        selectedModuleId = null;

        GameMgr.Instance.Loadout.SelectFrame(currentFrame.frameId);

        SetText(Get<Transform>("FrameName"), currentFrame.displayName);
        SetText(Get<Transform>("MoneyNum"), GameMgr.Instance.Data.Meta.softCurrency.ToString());

        CleanupCurrentFrameDisplay();
        SpawnFrameDisplay();

        ShowModulePanel(false);
        ShowLoadoutPanel(false);
        SetPanelItemCount(moduleContentRoot, 0);
        SetPanelItemCount(coreContentRoot, 0);
        RefreshFrameStats();
        ShowFrameDescription();
        RefreshAssemblyPreview();
    }

    private void SpawnFrameDisplay()
    {
        var parent = Get<Transform>("FrameDisplay");
        ClearChildren(parent);

        if (currentFrame == null)
            return;

        if (currentFrame.slotLayoutPrefab != null)
            currentSlotLayout = Instantiate(currentFrame.slotLayoutPrefab, parent);
        else if (currentFrame.frameCore != null)
            currentSlotLayout = Instantiate(currentFrame.frameCore, parent);
        else
            currentSlotLayout = null;

        if (currentSlotLayout == null)
            return;

        currentSlotLayout.transform.localPosition = Vector3.zero;
        currentSlotLayout.transform.localRotation = Quaternion.identity;
        currentSlotLayout.transform.localScale = Vector3.one;
        currentSlotLayout.transform.SetAsLastSibling();

        foreach (var slot in currentSlotLayout.GetComponentsInChildren<FrameSlotButton>(true))
        {
            slot.OnSlotClicked += OnSlotClicked;
            slot.OnSlotHovered += OnSlotHovered;
            slot.OnSlotHoverExited += OnSlotHoverExited;
            activeSlots.Add(slot);
            RefreshSlotVisual(slot);
        }
    }

    #endregion

    #region Slot Interaction

    private void OnSlotHovered(FrameSlotButton slot)
    {
        SetDescription(DescribeSlot(slot, GetSlotRuntime(slot.slotId)));
    }

    private void OnSlotHoverExited(FrameSlotButton slot)
    {
        RefreshDescriptionForCurrentSelection();
    }

    private void OnSlotClicked(FrameSlotButton slot)
    {
        selectedSlot = slot;
        selectedModuleId = GetSlotRuntime(slot.slotId)?.moduleId;

        foreach (var activeSlot in activeSlots)
            RefreshSlotVisual(activeSlot);

        ShowModulePanel(true);
        RefreshModulePanel(slot.allowedCategories);
        RefreshLoadoutPanel();
        RefreshFrameStats();
        RefreshDescriptionForCurrentSelection();
    }

    #endregion

    #region Module Panel

    private void RefreshModulePanel(ModuleCategory filter)
    {
        filteredModules.Clear();

        var db = GameConfigDatabase.Instance;
        if (db?.allModules != null)
        {
            foreach (var module in db.allModules)
            {
                if (module == null)
                    continue;
                if (!GameMgr.Instance.Data.Meta.IsModuleUnlocked(module.ModuleId))
                    continue;
                if (filter != ModuleCategory.None && !module.HasCategory(filter))
                    continue;

                filteredModules.Add(module);
            }
        }

        if (moduleContentRoot == null)
            return;

        var runtimeData = selectedSlot != null ? GetSlotRuntime(selectedSlot.slotId) : null;
        moduleContentRoot.IteratorChild(filteredModules.Count, iterator);

        void iterator(int index, Transform item)
        {
            int idx = index;
            var module = filteredModules[idx];
            bool isSelected = runtimeData != null &&
                              runtimeData.moduleId == module.ModuleId;

            BindPanelItem(
                item,
                module.moduleName,
                module.icon,
                isSelected ? module.themeColor : module.themeColor * 0.55f,
                () =>
                {
                    if (isSelected)
                        UnequipSelectedModule();
                    else
                        SelectModule(module);
                },
                () => SetDescription(DescribeModule(module)),
                RefreshDescriptionForCurrentSelection);
        }
    }

    private void SelectModule(ModuleConfig module)
    {
        if (selectedSlot == null || module == null)
            return;

        if (!GameMgr.Instance.Loadout.EquipModule(selectedSlot.slotId, module.ModuleId))
            return;

        selectedModuleId = module.ModuleId;
        RefreshSelectionState();
    }

    private void UnequipSelectedModule()
    {
        if (selectedSlot == null)
            return;

        if (!GameMgr.Instance.Loadout.UnequipModule(selectedSlot.slotId))
            return;

        selectedModuleId = null;
        RefreshSelectionState();
    }

    #endregion

    #region Core Panel

    private void RefreshLoadoutPanel()
    {
        bool hasModule = selectedSlot != null && !string.IsNullOrEmpty(selectedModuleId);
        ShowLoadoutPanel(hasModule);

        if (coreContentRoot == null)
            return;

        if (!hasModule || selectedSlot == null)
        {
            SetPanelItemCount(coreContentRoot, 0);
            return;
        }

        RefreshCoreSection();
    }

    private void RefreshCoreSection()
    {
        filteredCores.Clear();
        var runtimeData = selectedSlot != null ? GetSlotRuntime(selectedSlot.slotId) : null;
        var selectedModuleConfig = runtimeData?.moduleConfig;

        if (selectedModuleConfig == null)
        {
            SetPanelItemCount(coreContentRoot, 0);
            return;
        }

        var db = GameConfigDatabase.Instance;
        if (db?.allCores != null)
        {
            foreach (var core in db.allCores)
            {
                if (core == null)
                    continue;
                if (!GameMgr.Instance.Data.Meta.IsCoreUnlocked(core.coreId))
                    continue;
                if (!core.CanInsertInto(selectedModuleConfig))
                    continue;

                filteredCores.Add(core);
            }
        }

        if (coreContentRoot == null)
            return;

        string equippedCoreId = runtimeData?.coreId;

        coreContentRoot.IteratorChild(filteredCores.Count, iterator);

        void iterator(int index, Transform item)
        {
            var core = filteredCores[index];
            bool isSelected = equippedCoreId == core.coreId;

            BindPanelItem(
                item,
                core.displayName,
                core.icon,
                isSelected ? Color.yellow : new Color(1f, 1f, 1f, 0.55f),
                () => SelectCore(core),
                () => SetDescription(DescribeCore(core)),
                RefreshDescriptionForCurrentSelection);
        }
    }

    private void SelectCore(CoreConfig core)
    {
        if (selectedSlot == null || string.IsNullOrEmpty(selectedModuleId) || core == null)
            return;

        var runtimeData = GetSlotRuntime(selectedSlot.slotId);
        if (runtimeData?.moduleConfig == null)
            return;

        bool success = runtimeData != null && runtimeData.coreId == core.coreId
            ? GameMgr.Instance.Loadout.RemoveCore(selectedSlot.slotId)
            : GameMgr.Instance.Loadout.InsertCore(selectedSlot.slotId, core.coreId);

        if (!success)
            return;

        RefreshSelectionState();
    }

    #endregion

    #region Slot Visuals

    private void RefreshSlotVisual(FrameSlotButton slot)
    {
        var runtimeData = GetSlotRuntime(slot.slotId);
        bool hasModule = runtimeData != null && runtimeData.HasModule;
        bool hasCore = hasModule && !string.IsNullOrEmpty(runtimeData.coreId);
        bool isSelected = slot == selectedSlot;

        if (slot.BackgroundImage != null)
            slot.BackgroundImage.color = isSelected ? Brighten(slot.DefaultBackgroundColor, 0.25f) : slot.DefaultBackgroundColor;

        slot.transform.localScale = isSelected ? Vector3.one * 1.08f : Vector3.one;

        if (slot.IconImage == null)
            return;

        var module = runtimeData?.moduleConfig;
        if (hasModule && module != null && module.icon != null)
        {
            slot.IconImage.enabled = true;
            slot.IconImage.sprite = module.icon;
            slot.IconImage.color = hasCore ? new Color(1f, 0.92f, 0.35f, 1f) : Color.white;
        }
        else
        {
            slot.IconImage.sprite = null;
            slot.IconImage.enabled = false;
            slot.IconImage.color = Color.white;
        }
    }

    #endregion

    #region Description And Stats

    private void RefreshFrameStats()
    {
        int usedSlots = 0;
        int totalLoad = 0;

        foreach (var slot in activeSlots)
        {
            var runtimeData = GetSlotRuntime(slot.slotId);
            if (runtimeData == null || !runtimeData.HasModule)
                continue;

            usedSlots++;
            totalLoad += runtimeData.GetLoadCost();
        }

        SetText(Get<Transform>("FrameLimit"), $"{usedSlots}/{activeSlots.Count}");
        SetText(Get<Transform>("Load"), totalLoad.ToString());
    }

    private string DescribeSlot(FrameSlotButton slot, LoadoutModuleRuntimeData runtimeData)
    {
        if (slot == null)
            return DescribeFrame();

        if (runtimeData == null || !runtimeData.HasModule)
            return $"{slot.slotId}\n可装配类别: {FormatCategories(slot.allowedCategories)}";

        var lines = new List<string>
        {
            $"{runtimeData.moduleConfig.moduleName} [{runtimeData.moduleRarity}]",
            runtimeData.moduleConfig.description
        };

        if (runtimeData.coreConfig != null)
            lines.Add($"核心: {runtimeData.coreConfig.displayName}");

        if (runtimeData.pluginRuntimes.Count > 0)
            lines.Add($"插件: {string.Join(", ", GetPluginNames(runtimeData.pluginRuntimes))}");

        string stats = BuildRuntimeStatSummary(runtimeData);
        if (!string.IsNullOrEmpty(stats))
            lines.Add(stats);

        return string.Join("\n\n", lines);
    }

    private string DescribeFrame()
    {
        if (currentFrame == null)
            return string.Empty;

        var parts = new List<string>
        {
            currentFrame.displayName,
            currentFrame.description
        };

        if (currentFrame.inherentEffects != null && currentFrame.inherentEffects.Count > 0)
        {
            var effectLines = new List<string>();
            foreach (var effect in currentFrame.inherentEffects)
            {
                if (!string.IsNullOrWhiteSpace(effect.description))
                    effectLines.Add(effect.description);
                else if (!string.IsNullOrWhiteSpace(effect.effectId))
                    effectLines.Add(effect.effectId);
            }

            if (effectLines.Count > 0)
                parts.Add("固有特效: " + string.Join(" / ", effectLines));
        }

        return string.Join("\n\n", parts);
    }

    private string DescribeModule(ModuleConfig module)
    {
        if (module == null)
            return string.Empty;

        var parts = new List<string>
        {
            module.moduleName,
            module.description,
            $"分类: {FormatCategories(module.categories)}",
            $"默认品质: {module.defaultRarity}",
            $"负载: {module.GetLoadCost(module.defaultRarity)}",
            $"插件槽: {module.GetPluginSlots(module.defaultRarity)}"
        };

        var statLines = new List<string>();
        foreach (var stat in module.GetAllowedStats())
        {
            if (stat == null)
                continue;

            float baseValue = module.GetBaseStat(stat, module.defaultRarity);
            if (Mathf.Approximately(baseValue, 0f))
                continue;

            statLines.Add($"{stat.displayName}: {FormatStatValue(stat, baseValue)}");
        }

        if (statLines.Count > 0)
            parts.Add("基础属性\n" + string.Join("\n", statLines));

        return string.Join("\n\n", parts);
    }

    private string DescribeCore(CoreConfig core)
    {
        if (core == null)
            return string.Empty;

        var parts = new List<string>
        {
            core.displayName,
            core.description
        };

        if (core.statBonuses != null && core.statBonuses.Count > 0)
        {
            var lines = new List<string>();
            foreach (var bonus in core.statBonuses)
            {
                string line = bonus.statDefinition != null
                    ? bonus.statDefinition.displayName
                    : bonus.StatId;
                if (!Mathf.Approximately(bonus.additiveBonus, 0f))
                    line += $" +{bonus.additiveBonus:0.##}";
                if (!Mathf.Approximately(bonus.multiplicativeBonus, 0f))
                    line += $" / +{bonus.multiplicativeBonus * 100f:0.#}%";

                lines.Add(line);
            }

            parts.Add("数值加成\n" + string.Join("\n", lines));
        }

        return string.Join("\n\n", parts);
    }

    private string BuildRuntimeStatSummary(LoadoutModuleRuntimeData runtimeData)
    {
        if (runtimeData?.moduleConfig == null)
            return string.Empty;

        var lines = new List<string>();
        foreach (var stat in runtimeData.moduleConfig.GetAllowedStats())
        {
            if (stat == null)
                continue;

            float value = runtimeData.GetFinalStat(stat);
            if (Mathf.Approximately(value, 0f))
                continue;

            lines.Add($"{stat.displayName}: {FormatStatValue(stat, value)}");
        }

        return lines.Count > 0 ? "当前属性\n" + string.Join("\n", lines) : string.Empty;
    }

    private string FormatCategories(ModuleCategory categories)
    {
        if (categories == ModuleCategory.None)
            return "None";

        var results = new List<string>();
        foreach (ModuleCategory value in Enum.GetValues(typeof(ModuleCategory)))
        {
            if (value == ModuleCategory.None)
                continue;
            if ((categories & value) != 0)
                results.Add(value.ToString());
        }

        return string.Join(", ", results);
    }

    private string FormatStatValue(StatDefinition stat, float value)
    {
        if (stat == null)
            return value.ToString("0.##");

        return stat.valueKind switch
        {
            StatValueKind.Integer => Mathf.RoundToInt(value).ToString(),
            StatValueKind.Percent => $"{value * 100f:0.#}%",
            _ => value.ToString("0.##")
        };
    }

    private IEnumerable<string> GetPluginNames(List<LoadoutPluginRuntimeData> plugins)
    {
        foreach (var plugin in plugins)
        {
            if (plugin?.pluginConfig != null)
                yield return plugin.pluginConfig.displayName;
        }
    }

    private void RefreshDescriptionForCurrentSelection()
    {
        if (selectedSlot != null)
            SetDescription(DescribeSlot(selectedSlot, GetSlotRuntime(selectedSlot.slotId)));
        else
            ShowFrameDescription();
    }

    private void ShowFrameDescription()
    {
        SetDescription(DescribeFrame());
    }

    #endregion

    #region Panel Visibility

    private void SetDescription(string text)
    {
        SetText(Get<Transform>("Description"), text);
    }

    private void ShowModulePanel(bool visible)
    {
        var panel = Get<Transform>("ModulePanel");
        if (panel != null)
            panel.gameObject.SetActive(visible);
    }

    private void ShowLoadoutPanel(bool visible)
    {
        var panel = Get<Transform>("CorePanel");
        if (panel != null)
            panel.gameObject.SetActive(visible);
    }

    #endregion

    #region Loadout Access

    private LoadoutModuleRuntimeData GetSlotRuntime(string slotId)
    {
        return GameMgr.Instance.Loadout.GetEquippedModuleRuntime(slotId);
    }

    private void RefreshSelectionState()
    {
        foreach (var activeSlot in activeSlots)
            RefreshSlotVisual(activeSlot);

        if (selectedSlot != null)
        {
            selectedModuleId = GetSlotRuntime(selectedSlot.slotId)?.moduleId;
            RefreshModulePanel(selectedSlot.allowedCategories);
        }

        RefreshLoadoutPanel();
        RefreshFrameStats();
        RefreshDescriptionForCurrentSelection();
        Save();
        RefreshAssemblyPreview();
    }

    private void Save()
    {
        GameMgr.Instance.Data.Save();
    }

    #endregion

    #region Cleanup

    private void CleanupCurrentFrameDisplay()
    {
        foreach (var slot in activeSlots)
        {
            if (slot == null)
                continue;

            slot.OnSlotClicked -= OnSlotClicked;
            slot.OnSlotHovered -= OnSlotHovered;
            slot.OnSlotHoverExited -= OnSlotHoverExited;
        }

        activeSlots.Clear();

        if (currentSlotLayout != null)
            Destroy(currentSlotLayout);

        currentSlotLayout = null;
    }

    #endregion

    #region Panel Item Binding

    private void SetPanelItemCount(Transform contentRoot, int count)
    {
        if (contentRoot == null)
            return;

        contentRoot.IteratorChild(count, (_, _) => { });
    }

    private void BindPanelItem(
        Transform item,
        string label,
        Sprite icon,
        Color color,
        Action onClick,
        Action onHover,
        Action onHoverExit)
    {
        if (item == null)
            return;

        var button = item.GetComponent<Button>();
        if (button != null)
            button.onClick.SetListener(() => onClick?.Invoke());

        var image = item.GetComponent<Image>();
        if (image != null)
            image.color = color;

        var iconImage = FindItemIcon(item);
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        var labelText = item.GetComponentInChildren<TMP_Text>(true);
        if (labelText != null)
            labelText.text = label;

        var trigger = item.GetComponent<EventTrigger>();
        if (trigger == null && (onHover != null || onHoverExit != null))
            trigger = item.gameObject.AddComponent<EventTrigger>();

        if (trigger != null)
        {
            trigger.triggers ??= new List<EventTrigger.Entry>();
            trigger.triggers.Clear();
            trigger.AddTrigger(EventTriggerType.PointerEnter, _ => onHover?.Invoke());
            trigger.AddTrigger(EventTriggerType.PointerExit, _ => onHoverExit?.Invoke());
        }
    }

    private Image FindItemIcon(Transform item)
    {
        foreach (var image in item.GetComponentsInChildren<Image>(true))
        {
            if (image.transform != item)
                return image;
        }

        return null;
    }

    #endregion

    #region Utility

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int index = parent.childCount - 1; index >= 0; index--)
            Destroy(parent.GetChild(index).gameObject);
    }

    private void SetText(Transform target, string value)
    {
        if (target == null)
            return;

        var text = target.GetComponent<Text>();
        if (text != null)
        {
            text.text = value;
            return;
        }

        var tmp = target.GetComponent<TMP_Text>();
        if (tmp != null)
            tmp.text = value;
    }

    private Color Brighten(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp01(color.r + amount),
            Mathf.Clamp01(color.g + amount),
            Mathf.Clamp01(color.b + amount),
            color.a);
    }

    private void RefreshAssemblyPreview()
    {
        var snapshot = GameMgr.Instance.Loadout.BuildCurrentAssemblySnapshot();
        GameMgr.Instance.Preview.ShowAssemblyPreview(snapshot);
        var previewTexture = Get<RawImage>("PreviewTexture");
        if (previewTexture != null)
            previewTexture.texture = GameMgr.Instance.Preview.GetAssemblyPreviewTexture();
    }

    #endregion
}
