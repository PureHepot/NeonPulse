using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AssembleUI : UIBase
{
    private readonly List<FrameSlotButton> activeSlots = new();
    private readonly List<FrameConfig> frames = new();
    private readonly List<ModuleConfig> filteredModules = new();
    private readonly Dictionary<string, Transform> panelCache = new();

    private int frameIndex;
    private FrameConfig currentFrame;
    private GameObject currentSlotLayout;
    private FrameSlotButton selectedSlot;
    private string selectedModuleId;

    private Transform bgRoot;
    private Transform framePanel;
    private Transform modificationPanel;
    private Transform frameDetailPanel;
    private Transform moduleCargoPanel;
    private Transform coreCargoPanel;
    private Transform moduleDetailPanel;
    private Transform moduleCargoDetailPanel;
    private Transform previewPanel;
    private Transform moduleCargoContent;
    private Transform coreCargoContent;
    private Transform moduleEntryContent;
    private Transform moduleCargoEntryContent;

    public override void OnEnter(object args)
    {
        base.OnEnter(args);

        var db = GameConfigDatabase.Instance;
        if (db == null || db.allFrames == null)
            return;

        CachePanels();

        frames.Clear();
        foreach (var frame in db.allFrames)
        {
            if (frame != null && GameMgr.Instance.Data.Meta.IsFrameUnlocked(frame.frameId))
                frames.Add(frame);
        }

        if (frames.Count == 0)
            return;

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
        Save();
        base.OnClose();
    }

    private void CachePanels()
    {
        panelCache.Clear();

        bgRoot = Get<Transform>("Background");
        if (bgRoot == null)
            return;

        for (int index = 0; index < bgRoot.childCount; index++)
        {
            var child = bgRoot.GetChild(index);
            panelCache[child.name] = child;
        }

        panelCache.TryGetValue("FramePanel", out framePanel);
        panelCache.TryGetValue("ModificationPanel", out modificationPanel);
        panelCache.TryGetValue("FrameDetailPanel", out frameDetailPanel);
        panelCache.TryGetValue("ModuleCargoPanel", out moduleCargoPanel);
        panelCache.TryGetValue("CoreCargoPanel", out coreCargoPanel);
        panelCache.TryGetValue("ModuleDetailPanel", out moduleDetailPanel);
        panelCache.TryGetValue("ModuleCargoDetailPanel", out moduleCargoDetailPanel);
        panelCache.TryGetValue("PreviewPanel", out previewPanel);

        moduleCargoContent = FindIn(moduleCargoPanel, "ModuleCargoContent");
        coreCargoContent = FindIn(coreCargoPanel, "CoreCargoContent");
        moduleEntryContent = FindIn(moduleDetailPanel, "EntryContent");
        moduleCargoEntryContent = FindIn(moduleCargoDetailPanel, "CargoEntryContent") ??
                                  FindIn(moduleCargoDetailPanel, "EntryChangeContent");
    }

    private void BindButtons()
    {
        var prevButton = Get<Button>("BtnPrevFrame");
        if (prevButton != null)
            prevButton.onClick.SetListener(() => ShowFrame(frameIndex - 1));

        var nextButton = Get<Button>("BtnNextFrame");
        if (nextButton != null)
            nextButton.onClick.SetListener(() => ShowFrame(frameIndex + 1));

        var backButton = Get<Button>("BackBtn");
        if (backButton != null)
            backButton.onClick.SetListener(() => GameMgr.Instance.Game.ChangeState(new MenuState()));

        var finishButton = Get<Button>("Finish");
        if (finishButton != null)
        {
            finishButton.onClick.SetListener(() =>
            {
                if (currentFrame == null)
                    return;

                GameMgr.Instance.Loadout.SelectFrame(currentFrame.frameId);
                Save();

                Action action = () => GameMgr.Instance.Game.ChangeState(new MainGameState(false));
                GameMgr.Instance.UI.Open<LoadingUI>(action);
            });
        }
    }

    private void ShowFrame(int index)
    {
        if (frames.Count == 0)
            return;

        frameIndex = (index + frames.Count) % frames.Count;
        currentFrame = frames[frameIndex];
        selectedSlot = null;
        selectedModuleId = null;

        GameMgr.Instance.Loadout.SelectFrame(currentFrame.frameId);

        CleanupCurrentFrameDisplay();
        SpawnFrameDisplay();
        RefreshFrameTexts();
        RefreshFrameStats();
        RefreshAssemblyPreview();
        ShowFrameOverview();
    }

    private void SpawnFrameDisplay()
    {
        var parent = FindIn(framePanel, "FrameDisplay");
        ClearChildren(parent);

        if (parent == null || currentFrame == null)
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
            activeSlots.Add(slot);
            RefreshSlotVisual(slot);
        }
    }

    private void OnSlotClicked(FrameSlotButton slot)
    {
        if (slot == null)
            return;

        selectedSlot = slot;
        var runtimeData = GetSlotRuntime(slot.slotId);
        selectedModuleId = runtimeData?.moduleId;

        RefreshSlotVisuals();

        if (runtimeData == null || !runtimeData.HasModule)
        {
            OpenModuleCargoForSlot(slot);
            RefreshAssemblyPreview();
            return;
        }

        RefreshModuleDetail(runtimeData);
        ShowInstalledModuleFlow();
        RefreshAssemblyPreview();
    }

    private void OpenModuleCargoForSlot(FrameSlotButton slot)
    {
        if (slot == null)
            return;

        var runtimeData = GetSlotRuntime(slot.slotId);
        selectedModuleId = runtimeData != null && runtimeData.HasModule ? runtimeData.moduleId : null;
        RefreshModuleCargo(slot.allowedCategories);
        RefreshModuleCargoDetail();
        ShowModuleCargoFlow();
    }

    private void RefreshFrameTexts()
    {
        if (currentFrame == null)
            return;

        SetText(FindIn(framePanel, "FrameName"), currentFrame.displayName);
        SetText(FindIn(frameDetailPanel, "FrameName"), currentFrame.displayName);
        SetText(FindIn(frameDetailPanel, "FrameDescription"), currentFrame.description);
        SetText(FindIn(frameDetailPanel, "HealthNum"), Mathf.RoundToInt(currentFrame.baseMaxHP).ToString());
    }

    private void RefreshFrameStats()
    {
        int totalLoad = 0;

        foreach (var slot in activeSlots)
        {
            var runtimeData = GetSlotRuntime(slot.slotId);
            if (runtimeData == null || !runtimeData.HasModule)
                continue;

            totalLoad += runtimeData.GetLoadCost();
        }

        SetText(FindIn(frameDetailPanel, "LoadNum"), totalLoad.ToString());
    }

    private void RefreshModuleCargo(ModuleCategory allowedCategories)
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
                if (allowedCategories != ModuleCategory.None && !module.HasCategory(allowedCategories))
                    continue;

                filteredModules.Add(module);
            }
        }

        if (moduleCargoContent == null)
            return;

        moduleCargoContent.IteratorChild(filteredModules.Count, (index, item) =>
        {
            var module = filteredModules[index];
            var rootButton = item.GetComponent<Button>();

            SetText(FindIn(item, "ModuleName"), module.moduleName);
            SetText(FindIn(item, "ModuleDescription"), module.description);
            SetText(FindIn(item, "LoadNum"), module.GetLoadCost(module.defaultRarity).ToString());
            SetImageSprite(FindIn(item, "ModuleIcon"), module.icon);

            if (rootButton != null)
            {
                rootButton.onClick.SetListener(() =>
                {
                    selectedModuleId = module.ModuleId;
                    RefreshModuleCargoDetail();
                });
            }
        });
    }

    private void EquipModuleToSelectedSlot(ModuleConfig module)
    {
        if (selectedSlot == null || module == null)
            return;

        if (!GameMgr.Instance.Loadout.EquipModule(selectedSlot.slotId, module.ModuleId))
            return;

        selectedModuleId = module.ModuleId;
        Save();
        RefreshFrameStats();
        RefreshSlotVisuals();
        RefreshAssemblyPreview();

        var runtimeData = GetSlotRuntime(selectedSlot.slotId);
        if (runtimeData == null || !runtimeData.HasModule)
            return;

        RefreshModuleDetail(runtimeData);
        RefreshModuleCargoDetail();
        RefreshModificationPanel(runtimeData);
        ShowInstalledModuleFlow();
    }

    private void RefreshModuleDetail(LoadoutModuleRuntimeData runtimeData)
    {
        if (moduleDetailPanel == null || runtimeData?.moduleConfig == null)
            return;

        FillModuleInfo(moduleDetailPanel, runtimeData);
        RefreshModuleEntryList(moduleEntryContent, runtimeData);
        RefreshModuleCoreEquip(runtimeData);
        BindModuleDetailButtons();
    }

    private void RefreshModuleCargoDetail()
    {
        if (moduleCargoDetailPanel == null)
            return;

        var equippedRuntime = GetSelectedRuntime();
        var detailRuntime = GetCargoSelectedRuntime();

        FillModuleInfo(moduleCargoDetailPanel, detailRuntime);

        RefreshModuleEntryList(moduleCargoEntryContent, detailRuntime);

        var equipButton = FindIn(moduleCargoDetailPanel, "EquipBtn")?.GetComponent<Button>();
        if (equipButton != null)
        {
            bool canEquip = detailRuntime != null &&
                            detailRuntime.moduleConfig != null &&
                            selectedSlot != null &&
                            !IsCargoSelectionInstalled(equippedRuntime);
            equipButton.gameObject.SetActive(canEquip);
            equipButton.onClick.SetListener(EquipSelectedCargoModule);
        }

        var removeButton = FindIn(moduleCargoDetailPanel, "RemoveBtn")?.GetComponent<Button>();
        if (removeButton != null)
        {
            bool canRemove = IsCargoSelectionInstalled(equippedRuntime);
            removeButton.gameObject.SetActive(canRemove);
            removeButton.onClick.SetListener(RemoveSelectedCargoModule);
        }
    }

    private void FillModuleInfo(Transform root, LoadoutModuleRuntimeData runtimeData)
    {
        var moduleConfig = runtimeData?.moduleConfig;
        var coreConfig = runtimeData?.coreConfig;

        SetText(FindIn(root, "ModuleName"), moduleConfig != null ? moduleConfig.moduleName : "");
        SetText(FindIn(root, "ModuleDescription"), moduleConfig != null ? moduleConfig.description : "");
        SetText(FindIn(root, "CoreName"), coreConfig != null ? coreConfig.displayName : "");
        SetText(FindIn(root, "CoreDescription"), coreConfig != null ? coreConfig.description : string.Empty);
        SetText(FindIn(root, "LoadNum"), runtimeData != null && runtimeData.HasModule ? runtimeData.GetLoadCost().ToString() : "0");

        SetImageSprite(FindIn(root, "ModuleIcon"), moduleConfig != null ? moduleConfig.icon : null);
        SetImageSprite(FindIn(root, "CoreIcon"), coreConfig != null ? coreConfig.icon : null);
    }

    private void RefreshModuleEntryList(Transform content, LoadoutModuleRuntimeData runtimeData)
    {
        if (content == null)
            return;

        var entries = BuildModuleEntries(runtimeData);
        content.IteratorChild(entries.Count, (index, item) =>
        {
            var entry = entries[index];
            SetText(FindIn(item, "EntryName"), entry.name);
            SetText(FindIn(item, "EntryValue"), entry.value);
            SetText(FindIn(item, "EntryValueEnd"), string.Empty);
        });
    }

    private void RefreshModuleCoreEquip(LoadoutModuleRuntimeData runtimeData)
    {
        var moduleCoreEquip = FindIn(moduleDetailPanel, "ModuleCoreEquip");
        if (moduleCoreEquip == null)
            return;

        RefreshSingleModuleCard(FindIn(moduleCoreEquip, "ModuleGroup"), runtimeData);
        RefreshSingleCoreCard(FindIn(moduleCoreEquip, "CoreGroup"), runtimeData?.coreConfig, OpenCoreCargoForSelectedModule);

        var coreItemButton = FindIn(moduleCoreEquip, "CoreItem")?.GetComponent<Button>();
        if (coreItemButton != null)
            coreItemButton.onClick.SetListener(OpenCoreCargoForSelectedModule);
    }

    private void RefreshModificationPanel(LoadoutModuleRuntimeData runtimeData)
    {
        if (modificationPanel == null)
            return;

        RefreshSingleCoreCard(FindIn(modificationPanel, "CoreGroup"), runtimeData?.coreConfig, OpenCoreCargoForSelectedModule);
        RefreshSingleModuleCard(FindIn(modificationPanel, "ModuleGroup"), runtimeData);
    }

    private void RefreshSingleModuleCard(Transform root, LoadoutModuleRuntimeData runtimeData)
    {
        if (root == null)
            return;

        FillModuleInfo(root, runtimeData);
    }

    private void RefreshSingleCoreCard(Transform root, CoreConfig coreConfig, Action onClick)
    {
        if (root == null)
            return;

        SetText(FindIn(root, "CoreName"), coreConfig != null ? coreConfig.displayName : "Not Installed");
        SetText(FindIn(root, "CoreDescription"), coreConfig != null ? coreConfig.description : "Core system pending.");
        SetImageSprite(FindIn(root, "CoreIcon"), coreConfig != null ? coreConfig.icon : null);

        var button = FindIn(root, "CoreItem")?.GetComponent<Button>();
        if (button != null)
            button.onClick.SetListener(() => onClick?.Invoke());
    }

    private List<(string name, string value)> BuildModuleEntries(LoadoutModuleRuntimeData runtimeData)
    {
        var result = new List<(string name, string value)>();
        if (runtimeData?.moduleConfig == null)
            return result;

        foreach (var stat in runtimeData.moduleConfig.GetAllowedStats())
        {
            if (stat == null)
                continue;

            float value = runtimeData.GetFinalStat(stat);
            if (Mathf.Approximately(value, 0f))
                continue;

            result.Add((stat.displayName, FormatStatValue(stat, value)));
        }

        return result;
    }

    private void BindModuleDetailButtons()
    {
        var exchangeButton = FindIn(moduleDetailPanel, "ExchangeBtn")?.GetComponent<Button>();
        if (exchangeButton != null)
        {
            exchangeButton.onClick.SetListener(() =>
            {
                SetPanelVisible(frameDetailPanel, false);
                if (selectedSlot != null)
                    OpenModuleCargoForSlot(selectedSlot);
            });
        }

        var removeButton = FindIn(moduleDetailPanel, "RemoveBtn")?.GetComponent<Button>();
        if (removeButton != null)
            removeButton.onClick.SetListener(RemoveSelectedModule);
    }

    private void RemoveSelectedModule()
    {
        if (selectedSlot == null)
            return;

        if (!GameMgr.Instance.Loadout.UnequipModule(selectedSlot.slotId))
            return;

        selectedModuleId = null;
        Save();
        RefreshFrameStats();
        RefreshSlotVisuals();
        RefreshAssemblyPreview();
        RefreshModuleCargoDetail();
        ShowFrameOverview();
    }

    private void EquipSelectedCargoModule()
    {
        var runtimeData = GetCargoSelectedRuntime();
        if (runtimeData?.moduleConfig == null)
            return;

        EquipModuleToSelectedSlot(runtimeData.moduleConfig);
    }

    private void RemoveSelectedCargoModule()
    {
        if (selectedSlot == null)
            return;

        var equippedRuntime = GetSelectedRuntime();
        if (!IsCargoSelectionInstalled(equippedRuntime))
            return;

        if (!GameMgr.Instance.Loadout.UnequipModule(selectedSlot.slotId))
            return;

        Save();
        RefreshFrameStats();
        RefreshSlotVisuals();
        RefreshAssemblyPreview();

        selectedModuleId = null;
        CloseCargoAndShowCurrentSelection();
    }

    private void OpenCoreCargoForSelectedModule()
    {
        var runtimeData = GetSelectedRuntime();
        RefreshModificationPanel(runtimeData);
        RefreshCoreCargo(runtimeData?.moduleConfig);

        SetPanelVisible(framePanel, false);
        SetPanelVisible(frameDetailPanel, false);
        SetPanelVisible(modificationPanel, true);
        SetPanelVisible(coreCargoPanel, true);
    }

    private void RefreshCoreCargo(ModuleConfig moduleConfig)
    {
        if (coreCargoContent == null)
            return;

        var cores = new List<CoreConfig>();
        var db = GameConfigDatabase.Instance;
        if (db?.allCores != null)
        {
            foreach (var core in db.allCores)
            {
                if (core == null)
                    continue;
                if (moduleConfig != null && !core.CanInsertInto(moduleConfig))
                    continue;

                cores.Add(core);
            }
        }

        coreCargoContent.IteratorChild(cores.Count, (index, item) =>
        {
            var core = cores[index];
            SetText(FindIn(item, "CoreName"), core.displayName);
            SetText(FindIn(item, "CoreDescription"), core.description);
            SetImageSprite(FindIn(item, "CoreIcon"), core.icon);
        });
    }

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

    private void RefreshSlotVisuals()
    {
        foreach (var activeSlot in activeSlots)
            RefreshSlotVisual(activeSlot);
    }

    private LoadoutModuleRuntimeData GetSlotRuntime(string slotId)
    {
        return GameMgr.Instance.Loadout.GetEquippedModuleRuntime(slotId);
    }

    private LoadoutModuleRuntimeData GetSelectedRuntime()
    {
        return selectedSlot == null ? null : GetSlotRuntime(selectedSlot.slotId);
    }

    private LoadoutModuleRuntimeData GetCargoSelectedRuntime()
    {
        if (selectedSlot == null)
            return null;

        var moduleConfig = GameMgr.Instance.Loadout.GetModuleConfig(selectedModuleId);
        if (moduleConfig == null)
            return null;

        return new LoadoutModuleRuntimeData
        {
            slotId = selectedSlot.slotId,
            moduleId = moduleConfig.ModuleId,
            moduleType = moduleConfig.moduleType,
            moduleRarity = moduleConfig.defaultRarity,
            moduleConfig = moduleConfig,
            coreConfig = null,
            coreId = string.Empty,
            database = GameConfigDatabase.Instance,
            statGraph = new LoadoutStatGraph(moduleConfig, moduleConfig.defaultRarity, null)
        };
    }

    private bool IsCargoSelectionInstalled(LoadoutModuleRuntimeData equippedRuntime)
    {
        return equippedRuntime != null &&
               equippedRuntime.HasModule &&
               !string.IsNullOrEmpty(selectedModuleId) &&
               string.Equals(equippedRuntime.moduleId, selectedModuleId, StringComparison.Ordinal);
    }

    private void CloseCargoAndShowCurrentSelection()
    {
        SetPanelVisible(moduleCargoPanel, false);
        SetPanelVisible(moduleCargoDetailPanel, false);
        SetPanelVisible(frameDetailPanel, true);

        var runtimeData = GetSelectedRuntime();
        if (runtimeData != null && runtimeData.HasModule)
        {
            selectedModuleId = runtimeData.moduleId;
            RefreshModuleDetail(runtimeData);
            RefreshModificationPanel(runtimeData);
            SetPanelVisible(moduleDetailPanel, true);
        }
        else
        {
            SetPanelVisible(moduleDetailPanel, false);
        }
    }

    private void Save()
    {
        GameMgr.Instance.Data.Save();
    }

    private void CleanupCurrentFrameDisplay()
    {
        foreach (var slot in activeSlots)
        {
            if (slot == null)
                continue;

            slot.OnSlotClicked -= OnSlotClicked;
        }

        activeSlots.Clear();

        if (currentSlotLayout != null)
            Destroy(currentSlotLayout);

        currentSlotLayout = null;
    }

    private Transform FindIn(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
            return null;

        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }

        return null;
    }

    private void SetPanelVisible(Transform panel, bool visible)
    {
        if (panel != null)
            panel.gameObject.SetActive(visible);
    }

    private void ShowFrameOverview()
    {
        SetPanelVisible(framePanel, true);
        SetPanelVisible(modificationPanel, false);
        SetPanelVisible(frameDetailPanel, true);
        SetPanelVisible(moduleCargoPanel, false);
        SetPanelVisible(coreCargoPanel, false);
        SetPanelVisible(moduleDetailPanel, false);
        SetPanelVisible(moduleCargoDetailPanel, false);
    }

    private void ShowInstalledModuleFlow()
    {
        SetPanelVisible(framePanel, true);
        SetPanelVisible(modificationPanel, false);
        SetPanelVisible(frameDetailPanel, true);
        SetPanelVisible(moduleCargoPanel, false);
        SetPanelVisible(coreCargoPanel, false);
        SetPanelVisible(moduleDetailPanel, true);
        SetPanelVisible(moduleCargoDetailPanel, false);
    }

    private void ShowModuleCargoFlow()
    {
        SetPanelVisible(framePanel, true);
        SetPanelVisible(modificationPanel, false);
        SetPanelVisible(frameDetailPanel, false);
        SetPanelVisible(moduleCargoPanel, true);
        SetPanelVisible(coreCargoPanel, false);
        SetPanelVisible(moduleDetailPanel, false);
        SetPanelVisible(moduleCargoDetailPanel, true);
    }

    private void SetImageSprite(Transform target, Sprite sprite)
    {
        if (target == null)
            return;

        var image = target.GetComponent<Image>();
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

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

    private void RefreshAssemblyPreview()
    {
        var snapshot = GameMgr.Instance.Loadout.BuildCurrentAssemblySnapshot();
        GameMgr.Instance.Preview.ShowAssemblyPreview(snapshot);

        var previewTexture = FindIn(previewPanel, "PreviewTexture");
        if (previewTexture == null)
            return;

        var rawImage = previewTexture.GetComponent<RawImage>();
        if (rawImage != null)
            rawImage.texture = GameMgr.Instance.Preview.GetAssemblyPreviewTexture();
    }
}
