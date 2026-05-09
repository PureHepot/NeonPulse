# Project Map

## Overview

`NeonPulse` is a Unity 2D action project built on top of a small in-house framework.
The project currently contains both:

- an older runtime module upgrade system centered on `OLD_ModuleConfig`
- a newer frame/module/core/plugin loadout system centered on `GameConfigDatabase`

The current development focus is the assembly flow:

`StartUI -> AssembleGameState -> AssembleUI -> DataManager meta loadout -> StartNewRun -> Run.loadout -> gameplay/runtime systems`

## Root Structure

- `Assets/`
  - Main game content and scripts.
- `Packages/`
  - Unity package dependencies.
- `ProjectSettings/`
  - Unity project settings.
- `Library/`, `Temp/`, `Logs/`, `obj/`
  - Generated Unity/build/editor data.
- `README.md`
  - Framework-level intro, currently contains visible mojibake in some Chinese text.

## Assets Structure

- `Assets/Scenes/`
  - Unity scenes.
- `Assets/Resources/`
  - Runtime-loaded assets and config assets.
  - Important for this project because `GameConfigDatabase` is loaded from `Resources/Configs/GameConfigDatabase`.
- `Assets/Script/`
  - Main gameplay and framework code.
- `Assets/Plugins/`
  - Third-party plugins such as DOTween, ProCamera2D, Odin Inspector.

## Script Structure

- `Assets/Script/BaseScript/`
  - Base classes such as `UIBase`, `GameState`.
- `Assets/Script/CoreScript/`
  - Core framework managers, save data, config data, utilities.
- `Assets/Script/GameStatus/`
  - High-level game states like menu, assembly, main gameplay.
- `Assets/Script/Player/`
  - Player controller, module scripts, preview sync.
- `Assets/Script/UI/`
  - Runtime UI panels such as `StartUI`, `AssembleUI`, `LevelUpUI`.
- `Assets/Script/Enemys/`, `VFXEffect/`, `Tools/`
  - Enemy logic, effects, miscellaneous helpers.

## Core Runtime Systems

### State Flow

- `GameManager`
  - FSM entry point.
  - Starts in `MenuState`.
- `MenuState`
  - Opens `StartUI`.
- `StartUI`
  - New game enters `AssembleGameState`.
  - Continue enters `MainGameState(true)`.
- `AssembleGameState`
  - Opens `AssembleUI`.
- `MainGameState`
  - On new game calls `DataManager.StartNewRun(seed, frameId)`.
  - On continue restores runtime managers from save.

### UI Framework

- `UIManager`
  - Manages three layers: fullscreen, panel, popup.
- `UIBase`
  - Common UI base with `OnEnter/OnPause/OnResume/OnClose`.
- `ChildBindTool`
  - Name-based child lookup used by most UIs.

### Save / Data

- `DataManager`
  - Owns `SaveRoot`.
  - Exposes:
    - `Meta`: persistent progression/loadout data
    - `Run`: active run state
    - `CurrentLoadout`: current run loadout
- `SaveService`
  - Actual save/load persistence backend.

## Loadout System Map

### Main Config Types

- `GameConfigDatabase`
  - Central ScriptableObject database loaded from `Resources/Configs/GameConfigDatabase`.
  - Holds:
    - `allFrames`
    - `allModules`
    - `allCores`
    - `allPlugins`
    - stat definitions and schemas
- `FrameConfig`
  - Frame identity, base stats, inherent effects, visual prefabs.
- `ModuleConfig`
  - Module identity, type, categories, rarity profiles, stat schema, load cost.
- `CoreConfig`
  - Numeric bonuses applied to a module.
- `PluginConfig`
  - Effect-style add-ons attached to a module.

### Save Models

- `MetaProgressData`
  - Persistent progression and assembly state.
  - Important fields:
    - `selectedFrameId`
    - `unlockedFrameIds`
    - `unlockedModules`
    - `unlockedCoreIds`
    - `unlockedPluginIds`
    - `frameLoadouts`
- `FrameLoadoutSaveData`
  - Persistent per-frame loadout.
- `SlotSaveData`
  - Persistent slot content in assembly mode.
- `RunLoadoutData`
  - Runtime loadout used during gameplay.
- `SlotRuntimeSaveData`
  - Runtime slot content copied from meta when a new run starts.

### Assembly UI Path

- `AssembleUI`
  - Displays unlocked frames.
  - Spawns the selected frame's slot layout prefab.
  - Lets the player:
    - choose a frame
    - equip a module into a slot
    - equip/remove a core on the selected module
  - Persists changes via `DataManager.Save()`.

### Runtime Consumption Path

- `DataManager.StartNewRun`
  - Reads the selected frame from meta.
  - Copies the selected frame's persistent loadout into `Run.loadout`.
- `UpgradeManager`
  - Enables player module behaviours based on the runtime loadout.
  - Also mixes in older `OLD_ModuleConfig` upgrade data.
- `LoadoutModuleRuntimeBuilder`
  - Converts saved slot data into runtime-calculable objects.
- `LoadoutStatGraph`
  - Builds stat pipelines for equipped modules through `GameDataTool`.
- `PlayerManager`
  - Spawns player and applies unlocked/equipped modules.
- `LocalLoadoutProvider`
  - Read/write gateway for loadout data.
- `LoadoutManager`
  - Manager wrapper around the provider plus config caches.

## Important Files For Current Work

- `Assets/Script/UI/AssembleUI.cs`
  - Current assembly UI implementation.
- `Assets/Script/CoreScript/Manager/DataManager.cs`
  - Meta/save/run bridge.
- `Assets/Script/CoreScript/Manager/LoadoutManager.cs`
  - Loadout facade and config cache.
- `Assets/Script/CoreScript/Data/CoreData/LocalPlayerProgressionProvider.cs`
  - Concrete loadout provider implementation.
- `Assets/Script/CoreScript/Data/CoreData/GameData.cs`
  - Save model definitions.
- `Assets/Script/CoreScript/Data/LoadoutData/LoadoutModuleRuntimeData.cs`
  - Runtime calculation helper for module/core/plugin stats.
- `Assets/Script/CoreScript/Data/LoadoutData/LoadoutStatGraph.cs`
  - Adapter that maps loadout configs into `SealedValue<float>` stat graphs.
- `Assets/Script/CoreScript/GameDataTool/`
  - Unity-compiled copy of the stat-composition runtime.
- `Assets/Script/CoreScript/Manager/UpgradeManager.cs`
  - Applies runtime-equipped modules to player systems.

## GameDataTool

The stat-composition utility now exists in two locations:

- `Assets/Script/CoreScript/GameDataTool/SealedValue.cs`
- `Assets/Script/CoreScript/GameDataTool/ModifierGroup.cs`
- `Assets/Script/CoreScript/GameDataTool/SealedGameData.cs`
- `GameDataTool/SealedValue.cs`
- `GameDataTool/ModifierGroup.cs`
- `GameDataTool/SealedGameData.cs`
- `GameDataTool/Example.cs`

### What It Is

`GameDataTool` is not an editor data-entry tool.
It is a runtime composition framework for complex value pipelines.

Core idea:

- `SealedValue<T>` represents a final computed value.
- A value starts from `DefaultValue`.
- It then passes through an ordered list of `Modification<T>`.
- Each modification can cache results and mark itself dirty.
- Modifier groups can contain multiple runtime modifiers and support dynamic function-based values.

This makes it suitable for formulas such as:

- `(base + additive) * summedMultiplier`
- `(base * multiplierA + additiveB) * multiplierC`
- chained clamps / bool gates / dynamic level-based transforms

### Core Pieces

- `SealedValue<T>`
  - Lazy evaluation plus dirty-based incremental recache.
- `Modification<T>`
  - Base node in the modification chain.
- `ModifierGroup<TValue, TModifier>`
  - A named group that owns multiple modifiers of the same semantic type.
- `Modifier<T>`
  - A concrete runtime modifier, either static or `Func<T>`.
- `SealedGameData`
  - Reflection wrapper that exposes all `SealedValue<>` properties on a data object by property name.

### Built-in Group Types

- `FloatAddGroup`
- `FloatMultipleAddGroup`
- `FloatMultipleMulGroup`
- `IntAddGroup`
- `IntMultipleAddGroup`
- `IntMultipleMulGroup`
- `FloatClampModification`
- `BoolAndGroup`
- `BoolOrGroup`
- `FuncGroup<T>`

### Relevance To The New Loadout System

This tool fits the new assembly/loadout direction well.
It can become the stat-resolution layer for:

- frame base values
- module base values
- core numeric bonuses
- plugin-driven numeric modifiers
- temporary combat buffs/debuffs

In other words:

- `GameConfigDatabase` describes the content.
- `FrameLoadoutSaveData / RunLoadoutData` describe what is equipped.
- `GameDataTool` can describe how equipped content is resolved into final numbers.

### Important Current State

`GameDataTool` is now partially integrated into the active loadout path.

Observations:

- A Unity-compiled copy now lives under `Assets/Script/CoreScript/GameDataTool/`.
- `LoadoutStatGraph` uses `SealedValue<float>` to resolve equipped module stats.
- `LoadoutModuleRuntimeData` now delegates stat calculation to that graph.
- The original root `GameDataTool/` folder remains as the source/reference copy.

So right now it is best treated as:

- an active stat-composition subsystem for the new loadout runtime
- not yet the universal stat layer for the whole gameplay stack

### Recommended Integration Direction

When the future replacement for `UpgradeManager` is started, the next clean migration steps are:

1. Keep `GameConfigDatabase` as static content source.
2. Keep `Run.loadout` as equipped-content snapshot.
3. Extend the current `LoadoutStatGraph` from module/core handling into a full runtime stats layer.
4. Apply frame/module/core/plugin effects into named modifier groups instead of hardcoding formula branches in multiple managers.
5. Expose final resolved stats through a dedicated runtime reader instead of mixing config lookup and numeric logic together.

## Current Architectural Tension

The project is in the middle of a migration.

### Newer path

- `GameConfigDatabase`
- `FrameConfig / ModuleConfig / CoreConfig / PluginConfig`
- `AssembleUI`
- `FrameLoadoutSaveData / RunLoadoutData`
- `LoadoutModuleRuntimeData`

### Older path still active

- `OLD_ModuleConfig`
- `ModuleRuntimeData`
- parts of `UpgradeManager`
- parts of `LevelUpUI`

This means the assembly system already exists, but the full runtime upgrade/application path is not yet fully migrated.

## Recommended Direction

For current development, keep the loadout flow centered on a single rule:

- `Meta` is the editable long-term assembly source.
- `Run.loadout` is the per-run snapshot copied from `Meta`.
- UI and gameplay should go through one common loadout access layer instead of directly duplicating save access logic.

That is the direction of the current `loadoutData` unification work.

## Agent Memory

### 2026-04-30

- User preference: read `ProjectMap.md` at the start of work and keep this memory section updated after each completed answer so future conversations can resume quickly.
- AssembleUI generation note:
  - `Assets/Script/UI/AssembleUI.cs` module and core item generation must use the existing `Utils.IteratorChild` extension from `Assets/Script/CoreScript/Tools/Utils.cs`.
  - Do not use the previously added `MUtils.Iterator` path for AssembleUI; it was removed after it failed to drive the UI generation correctly.
- Verification:
  - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds.
  - Remaining warnings are pre-existing project warnings about hidden fields and unused private fields in enemy/player scripts.

### 2026-05-02

- User workflow preference:
  - Before making code edits, explicitly surface a modification request/update first instead of directly patching files without that visible step.

- AssembleUI module button bug:
  - Symptom: clicking any generated module button could equip the last module in the filtered list.
  - Cause: the UI click path passed only `ModuleType` into `LoadoutManager.EquipModule`; `LocalLoadoutProvider` resolves that through `Dictionary<ModuleType, ModuleConfig>`, so multiple `ModuleConfig` assets with the same `moduleType` overwrite each other and the last cached config wins.
  - Fix: `AssembleUI.SelectModule` now equips by `module.ModuleId`; `LoadoutManager`, `ILoadoutMutator`, and `LocalLoadoutProvider` now expose/implement `EquipModule(string slotId, string moduleId)` while keeping the old `ModuleType` overload for compatibility.
  - Verification: `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with only the existing project warnings.

- Runtime rewrite audit:
  - User intent: rebuild the whole in-run gameplay layer from scratch; old in-run systems such as `PlayerManager`, `MaskSystemManager`, enemy spawning/waves, player modules, runtime upgrades, combat HUD, and enemies can be treated as disposable.
  - Preserve for now: menu/state/UI framework, save/meta/loadout assembly data, `AssembleUI`, `GameConfigDatabase`, frame/module/core/plugin config models, `LoadoutManager`, `DataManager` meta/run snapshot bridge, audio/input/UI/pool base services if useful, and background presentation scripts.
  - Background exception: keep `Assets/Script/VFXEffect/BackgroundFXController.cs` and `Assets/Script/CoreScript/Data/VisualThemePreset/SOVisualThemePresets.cs`; old callers in `WaveManager` and enemies can be removed/replaced later without editing the background script itself.
  - Main obsolete runtime cluster: `Assets/Script/GameStatus/MainGameState.cs`, `Assets/Script/CoreScript/Manager/PlayerManager.cs`, `UpgradeManager.cs`, `MaskSystemManager.cs`, `Assets/Script/Player/**`, `Assets/Script/Enemys/**`, old runtime UI (`HUDUI`, `HpBarUI`, `ExpBarUI`, `LevelUpUI`, `MaskGachaUI`, `GameOverUI`, `EndUI`, `PreviewGetConfigUI`), `WaveData.cs`, `OLD_ModuleConfig.cs`, and `ModuleRuntimeData.cs`.

- Runtime cleanup pass:
  - User clarified: do not process concrete `Enemys` behavior scripts for now, and do not process in-run UI scripts for now; clean the remaining old runtime path, including spawning logic because it will be rewritten.
  - `MainGameState` is now a minimal run snapshot state: it starts/saves `DataManager` run data and supports Escape pause, but no longer spawns the player, opens old HUD/LevelUp UI, initializes `UpgradeManager`, subscribes to module upgrade events, or starts wave spawning.
  - `MenuState` no longer touches `PlayerManager.spawnPoint`.
  - `WaveManager` was reduced to a compatibility shell: it keeps `currentWaveIndex`, `activeEnemies`, save sync, and enemy register/unregister APIs for temporarily retained Enemy/UI code, but all old wave config, spawn coroutine, spawn position, background switching, and victory flow were removed.
  - `PlayerManager` and `UpgradeManager` were reduced to compatibility shells so retained UI/Player/Enemy scripts compile while old runtime behavior is disabled.
  - Verification: `dotnet build Assembly-CSharp.csproj --no-restore` succeeds; remaining warnings are in retained Enemy/Player/UI scripts.

- Central manager bootstrap:
  - Added `Assets/Script/CoreScript/Manager/GameMgr.cs` as the central runtime manager loader/access point.
  - `GameMgr` auto-creates itself with `RuntimeInitializeOnLoadMethod`, creates core managers in a controlled order, keeps typed references such as `GameMgr.Instance.Data`, `GameMgr.Instance.UI`, `GameMgr.Instance.Audio`, `GameMgr.Instance.Loadout`, and supports `GameMgr.Instance.Get<T>()`.
  - Manager creation first checks for an existing scene instance, then tries `Resources/Prefabs/Managers/{TypeName}` and `Resources/Managers/{TypeName}`, then creates a plain GameObject with the manager component. This allows configured manager prefabs later without requiring managers to be mounted in the main scene.
  - `MonoSingleton<T>` now exposes `RegisterInstance(T instance)` so managers created by `GameMgr` also populate legacy `XxxManager.Instance` caches; old code remains compatible while new code should use `GameMgr`.
  - Active menu/assembly/run flow now uses `GameMgr.Instance` instead of direct `DataManager.Instance`, `UIManager.Instance`, `AudioManager.Instance`, `LoadoutManager.Instance`, or `GameManager.Instance`.
  - `Assembly-CSharp.csproj` was updated to include the new `GameMgr.cs` for command-line build verification; Unity/Rider may regenerate this file.
  - Verification: `dotnet build Assembly-CSharp.csproj --no-restore` succeeds; remaining warnings are existing retained Enemy/Player/UI warnings.

- UIManager bootstrap exception:
  - `UIManager` is a special manager and must live on the `Canvas`, not as a plain child object under `GameMgr`.
  - `GameMgr` now loads `UIManager` during scene-manager setup, after the scene is loaded: it finds or creates a Canvas, adds required `CanvasScaler`, `GraphicRaycaster`, and `EventSystem` support, then attaches or finds `UIManager` on that Canvas.
  - `GameMgr.RegisterManager` no longer reparents `UIManager` under the `GameMgr` root.
  - `UIManager.InitUIStructure` now resolves the Canvas from its own GameObject first and creates/finds `Layer_FullScreen`, `Layer_Panel`, and `Layer_Popup` under the Canvas transform, so UI prefabs instantiate into the correct hierarchy.
  - Verification: `dotnet build Assembly-CSharp.csproj --no-restore` succeeds.

- MenuState `StartScene` lookup fix:
  - Symptom: returning from `AssembleUI` to `MenuState` could not reacquire `StartScene`, so the menu scene presentation object stopped coming back.
  - Cause: `MenuState.OnExit()` disables `StartScene`, and the previous lookup path could not find inactive scene objects when a new `MenuState` instance entered again.
  - Fix: `Assets/Script/GameStatus/MenuState.cs` now resolves `StartScene` by recursively searching the active scene root hierarchy, which includes inactive children already present in the scene.
  - Verification: `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with only the existing retained-script warnings.

- Module system analysis and multiplayer-oriented rewrite direction:
  - Current assembly path is coherent:
    - `AssembleUI` edits the selected frame loadout through `GameMgr.Instance.Loadout`.
    - `LoadoutManager` delegates reads/writes to `LocalLoadoutProvider`.
    - `LocalLoadoutProvider` writes into `DataManager.Meta.frameLoadouts` while outside a run, and into `Run.loadout` while a run is active.
    - `DataManager.StartNewRun()` snapshots the selected frame's meta loadout into `Run.loadout`.
    - `LoadoutModuleRuntimeBuilder` + `LoadoutStatGraph` resolve each slot into runtime stats from `ModuleConfig` + `CoreConfig`; plugin configs are collected, but plugin effects are still only exposed as `effectId` strings for later consumption.
  - Current runtime architecture is not ready for multiplayer:
    - The active provider and managers are global singletons bound to one local save context, not to a player/entity instance.
    - The old executable module layer (`PlayerModule`, `ModuleManager`, old `UpgradeManager` callers) is MonoBehaviour-driven on the local player prefab and reads global singletons directly.
    - Unlock data is still keyed by `ModuleType` in `MetaProgressData.unlockedModules`, which is too coarse if multiple `ModuleConfig` assets share one type.
    - Frame slot authority currently comes from scene/UI slot layout prefabs (`FrameSlotButton.slotId` / `allowedCategories`), which is fine for assembly display but weak as an authoritative gameplay/network schema.
  - Recommended rewrite direction:
    - Keep `AssembleUI`, `Meta`, `Run.loadout`, `GameConfigDatabase`, and the current stat graph as the assembly/data foundation.
    - Introduce a per-entity runtime layer such as `EntityLoadoutSnapshot` + `EntityModuleHost` + `EntityModuleBehaviour`.
    - Each controllable entity should own its own loadout snapshot and stat source instead of reading `DataManager`/`LoadoutManager` singletons directly.
    - Module behaviour attachment should be driven by `moduleId`/`slotId`, not just `ModuleType`.
    - Runtime module behaviours should consume an injected entity context/stat source and never pull gameplay state from global singletons, so the same module logic can run for local player, remote player, AI ally, or server authority.
    - For multiplayer, replicate loadout snapshots or slot diffs keyed by player/entity id; only the authority side mutates them, clients rebuild visuals/behaviours from the replicated snapshot.
    - Migrate unlock ownership from `List<ModuleType>` to module-id-based unlocks if multiple module variants per type remain part of the design.
    - Move authoritative slot definitions out of pure UI prefabs into config/schema data, while still letting `AssembleUI` render those slots with the existing prefab/button layout.
  - Recommended first implementation/test path:
    - First validate the new runtime attachment model inside `AssembleUI`, not in combat.
    - Add a preview entity/module host in the assembly scene that consumes the currently edited frame loadout and live-refreshes when slot/module/core data changes.
    - This lets module attach/detach, slot identity, stat resolution, and future network-friendly per-entity boundaries be verified before rebuilding gameplay combat flow.

- Player prefab / module prefab / preview audit:
  - `Assets/Resources/Prefabs/Mono/Player/Player.prefab` is only a shell player object:
    - root `Player` carries `PlayerController` and `ModuleManager`
    - child `Body` carries visuals
    - child `Modules` exists but is empty in the prefab; module prefabs are not pre-mounted under the player
  - The player prefab script GUIDs confirm:
    - `157b2bb857dd6874dbf104f36a78450b` -> `Assets/Script/Player/PlayerController.cs`
    - `271bb8878925dc740bb8a10f25717c81` -> `Assets/Script/Player/Modules/ModuleManager.cs`
  - Module prefabs live in `Assets/Resources/Prefabs/Module/` and each root carries a concrete old `PlayerModule` behaviour:
    - `Health.prefab` -> `HealthModule`
    - `Movement.prefab` -> `MovementModule`
    - `OriginShooter.prefab` -> `OriginShooterModule`
    - `LaserDrones.prefab` -> `LaserDroneModule`
    - `SawBladeModule.prefab` -> `SawBladeModule`
    - `Shotgunner.prefab` -> `ShotgunModule`
    - `Sniper.prefab` -> `SniperModule`
    - `SpeedBooster.prefab` -> `DashModule`
    - `Shield.prefab` is a composite case: the root also carries `ShieldController` and `DynamicArcShield`, while the `ShieldModule` behaviour itself is a disabled component on the same root object
  - Old runtime module architecture implication:
    - `ModuleManager` manages unlocked `PlayerModule` MonoBehaviours keyed only by `ModuleType`
    - `LoadModule` no longer instantiates/registers module prefabs; its original logic is commented out
    - this means the current player prefab does not authoritatively declare module attachments, and the old module lifecycle is incomplete/legacy
  - Preview system is split across multiple legacy paths:
    - `PlayerPreview`:
      - directly mounted on `PlayerModelCamera` in `Assets/Scenes/Main.unity`
      - instantiates `Player.prefab`, destroys `PlayerController` and `Rigidbody2D`, removes the movement module from `ModuleManager`, sets the whole clone to `UI_Model`, and listens for `GameEvent.PlayerUIModelUnlock/Lock`
    - `PlayerPreviewSync`:
      - directly mounted in `Assets/Scenes/Main 1.unity`, `Main 2.unity`, and `Main 3.unity`
      - rebuilds a dummy player clone, disables controller/physics/colliders, then unlocks preview modules from `UpgradeManager.Instance.UnlockedModuleTypes`
      - `LevelUpUI` looks for `PlayerModelCamera` and calls `playerPreview.RebuildPreview()`
    - `PreviewManager`:
      - still exists as a third manager-based preview path and is now auto-created by `GameMgr`
      - largely appears legacy/underused; outside the manager itself, the notable live reference found is `LaserDroneModule` using `PreviewManager.Instance.SetLayerRecursively(...)`
  - Preview rewrite implication:
    - existing preview content is still tightly coupled to old `UpgradeManager`, `ModuleType`, and `PlayerModule` activation
    - there are at least two scene-driven preview implementations plus one manager path, so future assembly-preview work should consolidate onto one loadout-driven preview host instead of extending all three

- New assembly module system V1:
  - Implemented a first-pass new assembly path centered on `AssembleUI`, without reconnecting old combat runtime.
  - Added `Assets/Script/CoreScript/Data/LoadoutData/AssemblyLoadoutSnapshot.cs` as the new preview/loadout snapshot model for assembly-time module state.
  - `LoadoutManager` now exposes `BuildCurrentAssemblySnapshot()` so callers can consume the current selected-frame loadout without reading `Meta`/`Run` storage directly.
  - Added `Assets/Script/CoreScript/Manager/AssemblyLoadoutPreviewHost.cs`:
    - it finds/uses `PlayerModelCamera`
    - clones `Player.prefab` into a dedicated preview root
    - neutralizes physics/controller state for preview use
    - mounts module prefabs directly from the current loadout snapshot by `slotId` and `moduleType`, without `UpgradeManager`
    - currently resolves old module visual prefabs from `Resources/Prefabs/Module/*`
  - `PreviewManager` now exposes the new assembly preview entry points:
    - `ShowAssemblyPreview(snapshot)`
    - `HideAssemblyPreview()`
    - `GetAssemblyPreviewTexture()`
  - `AssembleUI` now:
    - creates a runtime `PreviewSurface` `RawImage` inside `FrameDisplay`
    - binds it to the `PlayerModelCamera` render texture through `PreviewManager`
    - refreshes the new assembly preview whenever frame/module/core selection changes
  - Scope note:
    - this V1 is intentionally preview/data-host focused
    - it does not revive the old `UpgradeManager` gameplay path
    - it provides the new loadout-driven attachment boundary needed for the later entity/runtime rewrite
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with only the existing retained-script warnings.

- Assembly preview prefab/config follow-up:
  - The hardcoded `ModuleType -> Resources/Prefabs/Module/*` mapping in `AssemblyLoadoutPreviewHost` was removed.
  - `ModuleConfig` now owns its preview prefab reference through a new `previewPrefab` field, so assembly preview attachment is driven by the equipped `moduleId`/`ModuleConfig`, not by a global type switch.
  - Current module config assets under `Assets/Resources/Configs/ModuleConfig/` were populated with matching preview prefabs:
    - movement modules -> `Movement.prefab`
    - health modules -> `Health.prefab`
    - defence modules -> `Shield.prefab`
    - base shooter modules -> `OriginShooter.prefab`
  - `AssembleUI` no longer creates a runtime `RawImage` under `FrameDisplay`.
  - Assembly preview output is now routed to the existing `PreviewPanel/BG/PreviewTexture` node in `Assets/Resources/Prefabs/UI/AssembleUI.prefab`, which already binds the `PlayerModelCamera` render texture in the intended UI location.
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with only the existing retained-script warnings.

### 2026-05-03

- Defence module asset naming cleanup:
  - User requested asset names and `moduleId` values be aligned and made easier to search.
  - Defence module config assets under `Assets/Resources/Configs/ModuleConfig/Defence/` were renamed to:
    - `BaseDefenseModule.asset`
    - `BaseDefenseModule_01.asset`
    - `BaseDefenseModule_02.asset`
    - `BaseDefenseModule_03.asset`
    - `BaseDefenseModule_04.asset`
  - Matching internal names and ids were normalized to:
    - `m_Name: BaseDefenseModule[_NN]`
    - `moduleId: base_defense_module[_nn]`
  - This also fixed duplicated ids in the old `BaseDefenceModule 3/4.asset` assets, which had both been incorrectly set to `base_defence_module_2`.
  - Naming rule to keep using for searchable config assets:
    - no spaces in asset names
    - stable sortable suffixes like `_01`, `_02`
    - `moduleId` mirrors the asset name in lowercase snake_case

- `ModuleType` retention analysis:
  - Current conclusion: `ModuleType` is not a good long-term identity field for the new loadout system, but it is still actively required by the current codebase and cannot be removed yet by simply keeping `categories`.
  - `categories` currently cover slot compatibility and stat/schema eligibility only.
  - `ModuleType` is still used by the active loadout/save path in several places:
    - `MetaProgressData.unlockedModules` is still `List<ModuleType>`
    - slot save/runtime data still serialize `moduleType`
    - `LocalPlayerProgressionProvider` still caches and resolves modules by `ModuleType` as a compatibility path
    - `CoreConfig` / `PluginConfig` restrictions still use `List<ModuleType>`
    - `DataManager` default unlock seeding still unlocks by `ModuleType`
  - Architectural direction:
    - unique identity should move to `moduleId`
    - coarse equip/stat eligibility should stay on `categories`
    - if coarse family restrictions are still needed later, introduce a new dedicated concept (for example module family/archetype/tags), instead of reusing `ModuleType` as identity
  - Practical migration target:
    - replace `ModuleType`-based unlocks, slot persistence, and core/plugin restrictions with `moduleId` and/or category-based rules
    - only remove `ModuleType` after those compatibility bridges are gone

- `ModuleType` migration phase 1 implemented:
  - Active assembly/loadout flow now prefers `moduleId` and `ModuleConfig` over `ModuleType`.
  - `MetaProgressData` now has `unlockedModuleIds` as the new unlock source while keeping the old `unlockedModules : List<ModuleType>` as a legacy bridge.
  - `MetaProgressData.IsModuleUnlocked` / `UnlockModule` now support both `moduleId` and `ModuleType`.
  - Legacy `ModuleType` unlock data is migrated into `unlockedModuleIds` through `MetaProgressData.MigrateLegacyUnlockedModules(...)`, called from `DataManager.EnsureDefaultUnlocks()`.
  - Default module unlock seeding now uses explicit module ids:
    - `base_move_module`
    - `base_health_module`
    - `base_shooter_module`
    - `defense_module_base`
  - `CoreConfig` and `PluginConfig` now support category-based insertion restrictions through `restrictedToCategories`, while keeping `restrictedToModules` as a compatibility field.
  - `LocalPlayerProgressionProvider` now resolves the equipped module primarily from `slot.moduleId`; `slot.moduleType` is only a fallback for old save/runtime data.
  - `LoadoutModuleRuntimeData.HasModule` now depends on `moduleConfig != null` instead of `moduleType != None`, so old/partial slot data no longer blocks valid module-id-based runtime resolution.
  - `AssembleUI` now checks module unlocks by `module.ModuleId` and tracks the selected module by `moduleId`; core filtering uses the selected `ModuleConfig` instead of `ModuleType`.
  - `LoadoutManager` and `DataManager` now keep the first module encountered per `ModuleType` in compatibility caches/fallback paths, avoiding unstable overwrite behavior when multiple `ModuleConfig` assets share one type.
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are existing retained Enemy/Player/UI warnings.

### 2026-05-05

- Assembly preview simplification for Defence/module prefab authoring:
  - User clarified that module preview prefabs are authored directly against the player prefab, so their default local transform should be treated as authoritative.
  - `Assets/Script/CoreScript/Manager/AssemblyLoadoutPreviewHost.cs` was simplified accordingly:
    - removed the preview-player auto-rotation path
    - removed the `PreviewSlotAnchors` / slot-position conversion preview layer
    - preview modules now instantiate directly under `Player/Modules`
    - preview modules no longer force `localPosition/localRotation/localScale` back to zero/identity/one after instantiation
    - preview instances strip their `MonoBehaviour` gameplay scripts and register a `PassiveModule` shell only, so assembly preview stays visual-only and no longer runs module rotation/combat logic
  - Direction note:
    - this keeps the current assembly preview aligned with the later plan to turn an `AssembleUI` panel into a playable combat sandbox, instead of investing further in a separate preview-only module anchor hierarchy.
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are existing retained Enemy/Player/UI warnings.

- Assembly preview frame-core resolution:
  - User clarified that selecting a frame in `AssembleUI` should also mount the corresponding player core visual under the preview player.
  - Runtime frame-core prefabs live under `Assets/Resources/Prefabs/Mono/Frame/Core/` and follow `Core_{FramePrefabName}.prefab`.
  - `Assets/Script/CoreScript/Manager/AssemblyLoadoutPreviewHost.cs` now resolves frame-core visuals in this order:
    - `Resources/Prefabs/Mono/Frame/Core/Core_{slotLayoutPrefab.name}`
    - fallback to `FrameConfig.frameCore`
  - This keeps assembly preview core visuals aligned with the player-mounted runtime prefab naming convention instead of relying only on the older `FrameConfig.frameCore` reference.
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are existing retained Enemy/Player/UI warnings.

- Weapon module runtime refactor V1:
  - Scope implemented around `Weapon_Ranged_Base -> OriginShooterModule` to prepare plugin-driven runtime effects without reviving the old `UpgradeManager` flow.
  - `Assets/Script/Player/Modules/PlayerModule.cs` now exposes stat reads by `StatDefinition` and `statId` in addition to the legacy `StatType` path, so module behaviours can prefer the new `ModuleConfig` stat-SO data while keeping legacy fallback values alive.
  - Added new execution-model base classes:
    - `Assets/Script/Player/Modules/WeaponModuleBase.cs`
    - `Assets/Script/Player/Modules/ProjectileWeaponModule.cs`
  - Design direction:
    - keep `PlayerModule` thin as the common lifecycle/runtime-data base
    - introduce shared middle layers by behaviour model (`WeaponModuleBase`, `ProjectileWeaponModule`) instead of building a broad inheritance tree purely around tags like ranged/melee
  - `WeaponModuleBase` now owns:
    - shared weapon cooldown ticking
    - shared aim rotation helper
    - plugin effect construction from `LoadoutPluginRuntimeData`
    - weapon-level hooks for muzzle-plan mutation and projectile-spawn mutation
  - `ProjectileWeaponModule` now owns:
    - `ProjectileSpawnData`
    - `WeaponFireContext`
    - `WeaponMuzzlePoint`
    - a common projectile spawn path that passes runtime spawn data into projectile behaviours through `IProjectileSpawnReceiver`
  - `OriginShooterModule` was rewritten onto that new stack:
    - now inherits `ProjectileWeaponModule`
    - reads damage and fire cadence from runtime loadout stats instead of `UpgradeManager`
    - prefers `weapon.damage` / `weapon.shotspeed` stat ids and falls back to legacy `StatType` values where needed
    - builds a muzzle plan, allows plugin effects to modify it, then spawns projectiles through `ProjectileSpawnData`
    - still reuses the existing prefab-authored muzzle transforms and visual reload animation
  - `Assets/Script/Player/PlayerBullet.cs` was rewritten as the first projectile receiver:
    - now implements `IProjectileSpawnReceiver`
    - consumes `ProjectileSpawnData` instead of relying only on direct field pokes from the weapon module
    - supports a first runtime homing path by reacquiring the nearest `IDamageable` target inside the projectile hit layer and steering toward it each frame
  - First plugin effect path implemented:
    - `WeaponModuleEffectFactory` currently resolves `PluginType.Homing` / `effectId == "Homing"` into `HomingWeaponModuleEffect`
    - that effect writes homing parameters into `ProjectileSpawnData` using plugin params with runtime fallbacks
  - Follow-up implication:
    - extra-muzzle plugins should be implemented later as another `IWeaponModuleEffect` that edits `WeaponMuzzlePoint` lists, instead of hardcoding more branches back into `OriginShooterModule`
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are existing retained Enemy/Player/UI warnings.

- Assembly completion now enters a minimal runtime spawn path for mech testing:
  - Goal for this step is intentionally narrow: after the user clicks the assembly completion flow and enters game state, skip reviving broader combat/progression systems and just materialize the currently assembled player entity for hands-on validation.
  - `Assets/Script/GameStatus/MainGameState.cs` now spawns the runtime player on enter after the run snapshot is prepared, and saves player position/hp back into the active run snapshot on exit.
  - `Assets/Script/CoreScript/Manager/PlayerManager.cs` now owns the first runtime assembly materialization path:
    - instantiate the runtime `Player` prefab
    - mount the selected frame core under `Player/Core`
    - build each equipped module from `Run.loadout` through `LoadoutModuleRuntimeBuilder`
    - instantiate module runtime prefabs directly under `Player/Modules`
    - register each spawned module with `ModuleManager`
  - Frame core resolution follows the same runtime naming rule already used by assembly preview:
    - `Resources/Prefabs/Mono/Frame/Core/Core_{slotLayoutPrefab.name}`
    - fallback to `FrameConfig.frameCore`
  - Current boundary:
    - this step only guarantees that the assembled mech entity is spawned for runtime testing
    - other game-start logic remains intentionally unexpanded until mech validation is stable
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are existing retained Enemy/Player/UI warnings.

- New-game assembly now discards stale active run state before opening `AssembleUI`:
  - Root cause found during mech-spawn testing:
    - `LocalLoadoutProvider` switches to `Run.loadout` whenever `DataManager.HasActiveRun` is true
    - `Start -> AssembleUI` could therefore edit an old persisted run instead of `Meta.frameLoadouts`
    - `Finish` then always starts a fresh run snapshot from `Meta.frameLoadouts`, making the newly equipped modules appear missing in `RunLoadoutData`
  - Fix applied:
    - `Assets/Script/GameStatus/AssembleGameState.cs` now clears any stale active run before opening `AssembleUI`
    - `Assets/Script/CoreScript/Manager/DataManager.cs` now exposes `ClearActiveRun()` for this explicit new-game reset path
  - Intent:
    - `Continue` keeps using the old run snapshot
    - `Start -> Assemble` is now guaranteed to be a fresh assembly flow backed by meta loadout data
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are existing retained Enemy/Player/UI warnings.

- `OriginShooterModule` firing cadence was decoupled from muzzle animation:
  - Runtime issue found during assembled-mech testing:
    - ranged weapon could fire the first bullet, then appear unusable
    - current implementation was treating `weapon.shotSpeed` as a raw cooldown seconds value, while the config data contains a speed-style number (for example `110` on `Weapon_Ranged_Base`)
    - muzzle warmup / stagger visuals were also still part of the actual firing path, polluting cadence
  - `Assets/Script/Player/Modules/OriginShooterModule.cs` was simplified:
    - removed muzzle firing/reload visual progression
    - removed coroutine-based warmup / stagger timing from the core fire path
    - firing now happens immediately and cadence is controlled only by `SetCooldown(fireInterval)`
    - `weapon.shotSpeed` now resolves into a real fire interval instead of being used directly as seconds
    - legacy `BaseFireRate` fallback is retained for compatibility
  - Practical effect:
    - continuous fire no longer depends on animation state
    - shot-speed stat changes now affect weapon cadence directly instead of locking the weapon behind an inflated cooldown
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are existing retained Enemy/Player/UI warnings.

- `StatType` compatibility layer was removed from the project:
  - Scope:
    - deleted the old `Assets/Script/CoreScript/Data/ModuleData` directory, including the legacy `StatType` enum and old module-upgrade data assets/scripts
    - removed `StatType`-based APIs from the active loadout/runtime path
    - current runtime stat access now uses only `StatDefinition` and `statId`
  - Code changes:
    - `GameConfigDatabase` no longer builds a legacy-type lookup table for stats
    - `StatDefinition` no longer exposes `legacyStatType`
    - `ModuleConfig.ModuleStatValue` no longer serializes `statType`
    - `CoreConfig.CoreStatBonus` now binds directly to `StatDefinition`
    - `LoadoutStatGraph`, `LoadoutModuleRuntimeData`, `LocalLoadoutProvider`, `LoadoutManager`, and `PlayerModule` were all simplified to string / definition based stat reads
    - player module scripts that still read stats (`MovementModule`, `DashModule`, `HealthModule`, `ShieldModule`, `LaserDroneModule`, `SawBladeModule`, `ShotgunModule`, `SniperModule`, `OriginShooterModule`) now use explicit `statId` strings instead of enum values
  - Asset cleanup:
    - removed serialized `statType:` lines from current `ModuleConfig` assets
    - removed serialized `legacyStatType:` lines from current `StatDefinition` assets
  - Important boundary:
    - some older module scripts now point at stat ids that are not yet backed by current `StatDefinition` assets; those paths fall back to their script defaults until corresponding stat assets are authored
    - this is intentional and keeps the codebase free of the old enum bridge without silently preserving dead data paths
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are existing retained Enemy/Player/UI warnings.

- `OriginShooterModule` now binds directly to the five current weapon stat definitions:
  - Current weapon stat set for the base shooter flow is:
    - `weapon.damage`
    - `weapon.shotspeed`
    - `weapon.critchance`
    - `weapon.critdamage`
    - `weapon.weaponcount`
  - `Assets/Script/Player/Modules/OriginShooterModule.cs` now treats those five stat ids as the complete runtime contract for this weapon module instead of mixing in placeholder ids such as `weapon.muzzle_count` or unrelated multiplier ids.
  - `Assets/Resources/Configs/StatConfig/Schema_Weapon_Ranged.asset` now includes `Stat_Weapon_GunCount`, so the assembly/runtime layer exposes all five current shooter stats through the shared weapon schema.
  - Multi-gun behaviour was changed to match the new prefab-authoring rule:
    - only one authored muzzle is kept in the weapon prefab
    - runtime reads `weapon.weaponcount`
    - extra muzzles are generated virtually from that single authored muzzle as the prototype
    - each generated muzzle keeps the same radius from the rotation center as the prototype muzzle
    - adjacent muzzles are spaced by `15` degrees
    - the angular fan is centered on the current mouse aim direction
    - each muzzle computes its own bullet rotation so every shot still points at the mouse position
  - Crit handling note:
    - `weapon.critchance` and `weapon.critdamage` are now consumed by `OriginShooterModule` directly when spawning bullets, instead of being incorrectly reused as generic damage/fire-rate multipliers
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are existing retained Enemy/Player/UI warnings.

- `OriginShooterModule` multi-muzzle visuals now clone the authored muzzle and keep parallel aim:
  - Follow-up runtime issue from playtesting:
    - virtual extra muzzles were being used only for spawn math, so the player could not see cloned muzzle objects
    - each muzzle previously computed its own aim rotation toward the mouse, which could invert against the weapon's overall facing and allow shots to point back toward the player
  - `Assets/Script/Player/Modules/OriginShooterModule.cs` was adjusted so that:
    - extra muzzle visuals are instantiated at runtime from the single authored prototype muzzle
    - the muzzle fan is centered on `player.transform.position` rather than the weapon pivot, to keep the center-distance rule aligned with the user-authored expectation
    - all muzzle rotations stay parallel to the player-center aim direction
    - only the overall weapon facing tracks the mouse; individual muzzles no longer re-aim at the mouse independently
  - Practical result:
    - multiple muzzle objects are now visible during runtime
    - projectile directions stay coherent with the main weapon facing instead of folding inward toward the player
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are existing retained Enemy/Player/UI warnings.

- In-combat player/enemy motion now has a shared continuous-velocity physics layer:
  - Added `Assets/Script/Physics/ContinuousPhysicsMotor2D.cs` as a thin motor over `Rigidbody2D`:
    - stores desired linear velocity
    - eases actual rigidbody velocity toward that target in `FixedUpdate`
    - damps angular velocity over time
    - exposes `SetDesiredVelocity`, `StopDriving`, `SnapVelocity`, `AddImpulse`, and `ResetMotion`
  - `PlayerController` now owns that motor and exposes shared motion APIs for modules:
    - locomotion is now driven through the motor instead of directly writing rigidbody velocity
    - recoil / dash / hit reactions can add impulse without fighting the locomotion loop
  - `EnemyBase` now owns the same motor and exposes helper methods for children:
    - `DriveVelocity`
    - `StopMovementDrive`
    - `SnapVelocity`
    - `ApplyImpulse`
    - knockback no longer clears velocity first; it suspends drive briefly, then applies linear + angular impulse
  - Player-side combat integration updated:
    - `OriginShooterModule` now applies firing recoil to the player body
    - `PlayerBullet` now forwards configurable impact force and angular impulse into `EnemyBase.TakeDamage(...)`
    - `HealthModule`, `DashModule`, and `SawBladeModule` now use the shared player motion APIs rather than writing rigidbody state directly
  - Enemy-side movement integration updated for current non-boss set:
    - migrated to the shared drive layer:
      - `EnemyBomber`
      - `EnemyChaser`
      - `EnemyDasher`
      - `EnemyDrifter`
      - `EnemySpinner`
      - `EnemySpinShooter`
      - `EnemyHacker`
      - `EnemyPatroller`
      - `EnemyBlade`
      - `EnemyCrasher`
      - `EnemyElectric`
      - `EnemySummoner`
      - `EnemyShooter`
    - these classes now express motion as target velocity or impulse instead of repeatedly hard-setting rigidbody velocity every state tick
  - Current boundary:
    - boss behaviours were intentionally left alone
    - slime-specific physics remains separate
    - `EnemyElectric` / `EnemySummoner` still snap once when they settle at their chosen center point; this is currently a deliberate state transition rather than a fully eased arrival
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are existing retained boss/utility warnings.

### 2026-05-09

- InRun Step 1 skeleton landed:
  - Added `Assets/Script/InRun/` base structure for `Core`, `Theme`, `Loop`, `Score`, and `Pulse`.
  - Added first-pass in-run core types:
    - `InRunPhase`
    - `CombatGrade`
    - `InRunRuntimeSaveData`
    - `ThemeRuntimeSaveData`
    - `CombatLoopRuntimeSaveData`
    - `InRunRuntimeContext`
  - Added first-pass config ScriptableObjects:
    - `InRunConfigDatabase`
    - `BattleThemeConfig`
    - `CombatLoopGlobalConfig`
    - `ScoreConfig`
    - `PulseConfig`
  - `RunSaveData` now carries `inRun : InRunRuntimeSaveData`, so the new runtime layer has a dedicated serialized save branch instead of piggybacking on old wave/progression state.
  - `MainGameState` now starts `InRunDirector` after spawning the runtime player and stops it on exit.
  - Current `InRunDirector` behavior is intentionally debug-only for Step 1:
    - it does not spawn enemies or drive gameplay systems yet
    - it auto-simulates the full state sequence in logs from `Bootstrap` through `RunEnded`
    - theme selection is seeded off `Run.runSeed` and uses `InRunConfigDatabase` when available, otherwise falls back to synthetic debug theme ids
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are pre-existing retained-script warnings.

- InRun Step 2 timer/pulse/HUD landed:
  - Added:
    - `Assets/Script/InRun/Loop/CombatLoopController.cs`
    - `Assets/Script/InRun/Pulse/PulseSystem.cs`
    - `Assets/Script/InRun/UI/InRunHUD.cs`
  - `InRunDirector` no longer only auto-prints the entire state flow immediately.
    - It now drives a real placeholder loop sequence:
      - `CombatLoopPreparing`
      - `CombatLoopActive`
      - countdown timer
      - `CombatLoopComplete`
      - `PulseReady`
      - wait for `R`
      - `PulseResolving`
      - `LoopReward`
      - `Shop`
    - loop/shop/boss/reward are still placeholder transitions for now; no enemy spawning or real reward/shop logic yet.
  - Added debug loop duration override inside `InRunDirector`:
    - `debugLoopDurationSeconds = 30f` by default for Step 2 testing
    - if set `<= 0`, runtime falls back to `CombatLoopGlobalConfig.loopDurationSeconds`
  - Added a simple `OnGUI` debug HUD instead of prefab UI for this step.
    - Shows phase, theme progress, loop progress, selected theme id, timer, and pulse status.
    - Shows a centered `PULSE READY / Press R` prompt during `PulseReady`.
  - `MainGameState` now passes `isContinue` into `InRunDirector.BeginRun(isContinue)`.
  - `InRunRuntimeContext` now exposes theme resolution helpers so the in-run flow can reuse previously selected themes during resume instead of always redrawing.
  - Verification:
    - `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors; remaining warnings are pre-existing retained-script warnings.
