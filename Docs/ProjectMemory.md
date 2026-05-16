# Project Memory

> 本文件记录任务记忆、当前进度、协作偏好、历史决策和后续注意事项。  
> 项目结构请放 `ProjectMap.md`。  
> 各系统入口请放 `ProjectEntryPoints.md`。  
> 新记忆建议追加到最上方或对应日期下，避免把进度塞回 `ProjectMap.md`。

## 维护规则

- 每次开始处理仓库任务前，先看：
  1. `ProjectMemory.md`
  2. `ProjectEntryPoints.md`
  3. 必要时再看 `ProjectMap.md`
- 每次完成较大改动后，更新本文件的“当前进度”和“日期记忆”。
- `ProjectMap.md` 不再放 Agent Memory、任务进度或推荐方向。
- `ProjectEntryPoints.md` 不放历史流水账，只保留当前有效入口。

## 用户协作偏好

- 开始代码修改前，先明确说明要改什么，不要直接静默 patch。
- 用户希望我在每次继续工作时读取项目记忆，并在完成回答 / 改动后更新记忆。
- 用户正在把 `ProjectMap.md` 从“大杂烩”拆成：
  - `ProjectMap.md`：只记录项目结构。
  - `ProjectEntryPoints.md`：记录各种入口。
  - `ProjectMemory.md`：记录记忆和进度。

## 当前真实进度摘要

### 总体状态

项目已经从早期 GameJam Framework 和旧局内玩法，迁移到新的：

```text
StartUI
-> AssembleGameState
-> AssembleUI
-> Meta frame loadout
-> DataManager.StartNewRun
-> Run.loadout
-> MainGameState
-> PlayerManager runtime mech spawn
-> InRunDirector
```

当前主线是重建局内 InRun 小循环。

### 已完成 / 基本可测试

- 中央 `GameMgr` 管理器入口已建立。
- `UIManager` bootstrap 特殊处理完成，UIManager 保持挂在 Canvas 上。
- `MenuState -> StartUI -> AssembleGameState -> AssembleUI` 流程可用。
- 新游戏进入组装前会清理 stale active run，避免组装写入旧 `Run.loadout`。
- `AssembleUI` 可以编辑当前 frame loadout。
- `LoadoutManager / LocalLoadoutProvider` 成为当前 loadout 读写入口。
- `DataManager.StartNewRun()` 会把 `Meta.frameLoadouts` 快照到 `Run.loadout`。
- 组装预览已改为 `ModuleConfig.previewPrefab` 驱动。
- frame core 预览 / 运行时挂载使用 `Core_{slotLayoutPrefab.name}` 优先，fallback 到 `FrameConfig.frameCore`。
- `PlayerManager` 已能在进入 `MainGameState` 后生成运行时机甲，用于测试。
- `OriginShooterModule` 已迁到 `WeaponModuleBase / ProjectileWeaponModule` 栈。
- `PlayerBullet` 已支持 `ProjectileSpawnData` 和第一版 homing 行为。
- `StatType` 兼容层已从当前 runtime stat 访问中移除，改用 `StatDefinition` / `statId`。
- `ContinuousPhysicsMotor2D` 已成为玩家和多数普通敌人的共享运动层。
- InRun Step 1/2/3/4/5/6 方向已推进：
  - 有 InRun runtime save branch。
  - 有 theme / loop / score / pulse 基础结构。
  - 有 InRun flow runner / resume coordinator。
  - 可以跑 theme -> 多个 combat loop -> boss -> next theme。
  - 可以按预算刷怪。
  - 可以进入 boss。
  - 有最小奖励和商店流程。
  - 商店商品开始纳入 runtime storage / warehouse 方向。

### 当前仍是临时 / 待完善

- InRun reward / shop 仍偏 debug 输入，需要正式 UI。
- 数值和配置还不完整，怪物、商店、奖励、Boss 都需要正式调参。
- 旧 runtime UI、旧 `PlayerModule`、旧敌人、旧 `EnemyManager`、旧 `UpgradeManager` 等仍有兼容残留。
- 新模块行为还没有完全变成 per-entity / multiplayer-friendly 架构。
- 插件效果目前只有 Homing 这条比较明确的武器效果路径。
- `ModuleType` 虽然不应再作为长期身份，但仍有兼容字段 / 旧资产引用。
- Boss 行为本体仍偏旧脚本，Boss arena clamp 已更新但 boss 完整战斗还要继续整理。

## 当前架构判断

### 新路径

优先扩展这些：

- `GameMgr`
- `GameConfigDatabase`
- `FrameConfig / ModuleConfig / CoreConfig / PluginConfig`
- `AssembleUI`
- `LoadoutManager`
- `LocalLoadoutProvider`
- `Run.loadout`
- `LoadoutModuleRuntimeBuilder`
- `LoadoutStatGraph`
- `InRunDirector`
- `Assets/Script/InRun/**`
- `ContinuousPhysicsMotor2D`

### 旧路径 / 谨慎扩展

除非为了兼容或删除迁移，不要继续在这些地方扩展新玩法：

- `OLD_ModuleConfig`
- 旧 `ModuleRuntimeData`
- 旧 `UpgradeManager` 行为路径
- 旧 `LevelUpUI` 升级逻辑
- 旧 `WaveManager` 波次逻辑
- 旧 `HUDUI / ExpBarUI / MaskGachaUI` 等局内 UI
- `EnemyManager` 的旧边界逻辑

## 模块系统记忆

### ModuleType 结论

- `ModuleType` 不适合作为新 loadout 系统长期身份。
- 当前长期身份应使用 `moduleId`。
- slot 兼容性 / stat schema eligibility 应使用 category 或 dedicated tags。
- `ModuleType` 只能作为旧数据兼容桥逐步清理。

### ModuleType 迁移已做

- `MetaProgressData` 新增 `unlockedModuleIds`。
- `IsModuleUnlocked` / `UnlockModule` 支持 `moduleId` 和 `ModuleType`。
- 旧 `ModuleType` unlock 会迁移到 `unlockedModuleIds`。
- 默认 unlock 使用显式 module id。
- `CoreConfig` / `PluginConfig` 支持 category restriction。
- `LocalPlayerProgressionProvider` 优先用 `slot.moduleId`。
- `LoadoutModuleRuntimeData.HasModule` 看 `moduleConfig != null`。
- `AssembleUI` 按 `module.ModuleId` 检查 unlock 和选择 module。

### 资产命名规则

继续使用：

- asset 名字不带空格。
- 使用稳定可排序后缀，例如 `_01`、`_02`。
- `moduleId` 与 asset 名低驼 / 小写 snake_case 对齐。

已处理过的 defence module 命名：

- `BaseDefenseModule.asset`
- `BaseDefenseModule_01.asset`
- `BaseDefenseModule_02.asset`
- `BaseDefenseModule_03.asset`
- `BaseDefenseModule_04.asset`

对应 id：

- `base_defense_module`
- `base_defense_module_01`
- `base_defense_module_02`
- `base_defense_module_03`
- `base_defense_module_04`

## 组装 / 预览记忆

### AssembleUI 生成规则

- `AssembleUI.cs` 生成 module / core item 时必须使用已有 `Utils.IteratorChild` extension。
- 不要使用之前添加过的 `MUtils.Iterator` 路径；它已移除，因为无法正确驱动 UI 生成。

### 组装预览方向

- 预览不再依赖旧 `UpgradeManager`。
- `AssemblyLoadoutPreviewHost` 从当前 selected frame loadout 构造预览。
- module preview prefab 来自 `ModuleConfig.previewPrefab`。
- module prefab 的默认 local transform 视为作者权威，不要强行归零。
- preview module 剥离 gameplay MonoBehaviour，只保留视觉 / passive shell。
- 选择 frame 时也应挂载对应 frame core visual。

### 预览历史问题

旧预览路径至少有：

- `PlayerPreview`
- `PlayerPreviewSync`
- `PreviewManager`

以后不要继续同时维护多套预览，应收敛到 loadout-driven preview host。

## 武器 / 子弹记忆

### 当前基础射手 stat contract

`OriginShooterModule` 当前只应依赖这五个 stat id：

- `weapon.damage`
- `weapon.shotspeed`
- `weapon.critchance`
- `weapon.critdamage`
- `weapon.weaponcount`

### 射速修正记忆

- 之前 `weapon.shotSpeed` 被当成 cooldown 秒数，导致第一发后看似不能开火。
- 已改为把 shot speed 解析成 fire interval。
- firing cadence 不再依赖 muzzle warmup / reload coroutine。

### 多枪口规则

- prefab 只保留一个作者配置的 muzzle。
- runtime 根据 `weapon.weaponcount` 生成额外 muzzle。
- 额外 muzzle 克隆视觉对象。
- fan 以玩家中心到鼠标方向为中心。
- muzzle 方向保持平行，避免朝内折回玩家。

### 插件方向

- Homing 已有第一条路径。
- 额外枪口 / 分裂 / 反弹等不要写回 `OriginShooterModule` 分支，应做成新的 `IWeaponModuleEffect`。

## 玩家 / 物理 / 敌人记忆

### ContinuousPhysicsMotor2D

- 玩家和多数普通敌人已经迁到共享 motor。
- 运动应优先写 desired velocity / impulse，不要直接反复写 Rigidbody velocity。
- clamp 到边界时必须同时修正向外 current velocity 和 desired velocity。

### 敌人边界新规则

- `EnemyBoundaryService` 是中心服务。
- `EnemyBoundaryConstraint` 表示持续约束，例如不能离开范围。
- `EnemyBoundaryReaction` 表示触发反应，例如传送 / 死亡 / 反弹。
- 第一次从屏幕外进入 arena 不应触发边界反应。
- 不要在同一敌人上混用多个互斥 constraint 风格，除非专门写 composed behavior。

### Boss arena

- Boss arena 不再使用四面静态 collider wall。
- 现在只保存 bounds，并在 `BossEncounterDirector.LateTick()` / `InRunDirector.LateUpdate()` 中限制玩家位置。
- 这样不会被玩家受伤无敌帧 / collider disable 绕过，也不会污染敌人和子弹的物理世界。

## InRun 记忆

### InRun 当前结构

主要目录：

- `Assets/Script/InRun/Core/`
- `Assets/Script/InRun/Theme/`
- `Assets/Script/InRun/Loop/`
- `Assets/Script/InRun/Pulse/`
- `Assets/Script/InRun/Score/`
- `Assets/Script/InRun/Spawn/`
- `Assets/Script/InRun/Reward/`
- `Assets/Script/InRun/Shop/`
- `Assets/Script/InRun/Boss/`
- `Assets/Script/InRun/UI/`

### InRun 已落地的关键类

- `InRunDirector`
- `InRunFlowRunner`
- `InRunResumeCoordinator`
- `InRunRuntimeContext`
- `InRunRuntimeSaveData`
- `CombatLoopController`
- `PulseSystem`
- `EnemySpawnDirector`
- `RewardDirector`
- `ScoreResolver`
- `ShopDirector`
- `BossEncounterDirector`
- `BossArenaLimiter`

### 当前可跑的小循环

当前目标流程：

```text
ThemeSelecting / ThemeIntro
-> CombatLoopActive
-> PulseReady or loop timeout
-> PulseResolving
-> LoopReward
-> Shop
-> repeat loop
-> BossPreparing
-> BossActive
-> BossReward
-> NextTheme / RunEnded
```

### 商店 / 仓库记忆

- `ShopDirector.OpenShop(theme, runtime)` 会优先尝试从 `runtime.shopInventory` 恢复商品。
- 如果没有恢复数据，则从 theme shop catalog 生成。
- 没有 catalog 时使用 placeholder 商品。
- 购买会：
  - 扣 `runtime.runCurrency`
  - 标记 offer purchased
  - 写入 `runtime.pendingRewards`
  - `warehouseSlotsDelta` 会修改仓库容量
  - 非货币商品尝试进入 warehouse
  - snapshot shop inventory

## 历史日期记忆

### 2026-05-16

- `AssembleUI.prefab` 已被用户彻底重做，旧 `AssembleUI.cs` 不再适合直接依赖全局重名节点。
- `AssembleUI.cs` 已先完成第一段迁移，当前策略是：
  - 先缓存 `BG` 下的主 Panel，而不是继续依赖 `UIBase.Get(name)` 的全树重名查找。
  - 先打通 Frame 相关流程：`FramePanel`、`FrameDetailPanel`、`ModuleDetailPanel`、`PreviewPanel`。
  - `FrameDetailPanel` 当前会填充：
    - `FrameName`
    - `FrameDescription`
    - `HealthNum` = `FrameConfig.baseMaxHP`
    - `LoadNum` = 当前已安装模块总负载
  - `FramePanel` 当前仍复用 `FrameDisplay` + `FrameSlotButton` 机制展示上一次拼装结果。
  - 点击已安装模块时会打开 `ModuleDetailPanel`，并填充模块/核心基础信息，同时刷新 `PreviewPanel`。
  - 未安装槽位当前先不进入模块库流程，留待下一步实现。
- `AssembleUI.cs` 第二段已接上：
  - 点击空槽位会关闭 `FrameDetailPanel`，打开 `ModuleCargoPanel`。
  - `ModuleCargoPanel` 会按 `FrameSlotButton.allowedCategories` 过滤可用模组，并用 `IteratorChild` 生成 `ModuleCargoContent`。
  - 点击模块库中的 `ModuleItem` 会把该模组装到当前槽位，然后刷新：
    - `ModuleDetailPanel`
    - `FrameDetailPanel` 的总负载
    - 槽位图标状态
    - `PreviewPanel`
  - `ModuleDetailPanel` 现在会维护 `ModuleEntryDetail`，按模块配置允许的 Stat 生成词条和值。
  - `ExchangeBtn` 已绑定为重新打开当前槽位的 `ModuleCargoPanel`。
  - `RemoveBtn` 已绑定为卸下当前槽位模组，并回到 `FrameDetailPanel`。

### 2026-05-10

- 最新提交显示 InRun 已推进到第六步方向：
  - 边缘事件迁移至 `InRunDirector`。
  - `EnemyManager` 变成 service / 兼容壳方向。
  - 商店物品开始真正纳入存储。
- 同日还有提交说明：
  - 局内第五步可刷怪。
  - 可简单测试商店。
  - 可进入 Boss。
- `ProjectMap.md` 当前过长，已按职责拆分为三个文件：
  - `ProjectMap.md`
  - `ProjectEntryPoints.md`
  - `ProjectMemory.md`

### 2026-05-09

- InRun Step 1 skeleton：
  - 增加 `Assets/Script/InRun/` 基础结构。
  - 增加 Core / Theme / Loop / Score / Pulse。
  - `RunSaveData` 增加 `inRun : InRunRuntimeSaveData`。
  - `MainGameState` 启动 / 停止 `InRunDirector`。
- InRun Step 2 timer / pulse / HUD：
  - `CombatLoopController`
  - `PulseSystem`
  - `InRunHUD`
  - 从单纯日志模拟改成 placeholder loop sequence。

### 2026-05-05

- 组装预览简化：
  - 预览模块直接挂到 `Player/Modules`。
  - 不再做 preview slot anchor 转换。
  - 不再重置 prefab local transform。
  - 剥离 gameplay MonoBehaviour，避免预览运行战斗逻辑。
- frame core 预览：
  - 优先 `Resources/Prefabs/Mono/Frame/Core/Core_{slotLayoutPrefab.name}`。
  - fallback `FrameConfig.frameCore`。
- `OriginShooterModule` 开始按新 weapon module runtime 重构。
- 组装完成后进入最小 runtime spawn 路径，用于实机验证装配机甲。
- 修复新游戏组装写到旧 active run 的问题。
- 移除 `StatType` 兼容层，当前 stat 访问使用 `StatDefinition` / `statId`。
- 基础射手使用五个当前武器 stat。
- 添加 `ContinuousPhysicsMotor2D`，迁移玩家和多数普通敌人。

### 2026-05-03

- Defence module asset 命名清理。
- 统一 `BaseDefenseModule[_NN]` 命名和 `base_defense_module[_nn]` id。
- 修复 defence module duplicated id。
- 明确 `ModuleType` 不是长期身份字段。
- ModuleType migration phase 1：active assembly/loadout flow 优先 `moduleId`。

### 2026-05-02

- 用户偏好：改代码前先明确说明改动请求 / 计划。
- 修复 AssembleUI module button bug：
  - 旧路径只传 `ModuleType`，多个 `ModuleConfig` 共用同一个 type 时会装备最后一个。
  - 新路径改为 `AssembleUI.SelectModule` 按 `module.ModuleId` equip。
  - `LoadoutManager`、`ILoadoutMutator`、`LocalLoadoutProvider` 增加 `EquipModule(string slotId, string moduleId)`。
- 用户意图：重建整套 in-run gameplay layer。
- 可保留：menu/state/UI framework、save/meta/loadout assembly data、`AssembleUI`、`GameConfigDatabase`、Frame/Module/Core/Plugin config、`LoadoutManager`、`DataManager`、基础 services、背景 presentation。
- 旧 runtime 可视为可丢弃：旧 player manager、mask、wave、runtime upgrade、combat HUD、敌人等。
- 运行时清理：
  - `MainGameState` 简化为 run snapshot state。
  - `WaveManager` 降为兼容壳。
  - `PlayerManager` / `UpgradeManager` 降为兼容壳。
- 新增 `GameMgr` central manager bootstrap。
- UIManager bootstrap 特殊处理。
- 修复 `MenuState` 返回后找不到 inactive `StartScene` 的问题。
- 分析 multiplayer-friendly module runtime 方向。
- 做了 player prefab / module prefab / preview audit。
- 新 assembly module system V1 落地。
- `ModuleConfig.previewPrefab` 驱动组装预览。

### 2026-04-30

- 用户要求：开始工作前读 `ProjectMap.md`，并在每次完成回答后更新 Agent Memory。
- AssembleUI 生成必须使用 `Utils.IteratorChild`。
- `dotnet build Assembly-CSharp.csproj --no-restore` 曾验证成功，仅有旧 warning。

## 后续优先级建议

1. 把 reward / shop 从 debug input 改成正式 UI。
2. 补齐 InRun 配置：enemy pool、spawn curve、score、shop catalog、boss config。
3. 把 shop / reward / warehouse 的物品效果真正落地到运行时系统。
4. 清理旧 runtime UI 和旧 upgrade path，避免新功能继续依赖 `UpgradeManager`。
5. 继续把 module runtime 从 global singleton 改成 per-entity host/context。
6. 完成 Boss 实战闭环：boss spawn、arena、奖励、下一主题 / run end。
7. 增加 run 结束结算，把局内奖励回写到 meta progression。

## 验证基线

历史多次执行：

```bash
dotnet build Assembly-CSharp.csproj --no-restore
```

结果通常为：

- `0 error`
- 剩余 warning 多为旧 Enemy / Player / UI / boss utility 脚本警告

最新记忆中的一次基线：

```bash
dotnet build .\Assembly-CSharp.csproj -nologo
```

结果：

- `0 error`
- `12 warning`

注意：我当前没有在 Unity Editor 里运行场景，进度判断主要来自仓库文件、提交信息和文档记录。

### 2026-05-16 AssembleUI 增补

- `AssembleUI.prefab` 根节点当前以 `Background` 为准，不再使用 `BG`，因为用户为规避重名问题已主动改名。
- `AssembleUI.cs` 已按新 prefab 的组合式面板流转继续调整：
  - 点击已安装模块槽位时，不再关闭 `FrameDetailPanel`，而是在其保持显示的同时打开 `ModuleDetailPanel`。
  - 点击空槽位，或点击 `ModuleDetailPanel/ExchangeBtn` 时，会打开 `ModuleCargoPanel` 与 `ModuleCargoDetailPanel`，同时关闭 `ModuleDetailPanel`。
  - `ModuleDetailPanel` 下新增的 `ModuleCoreEquip` 已接入展示与按钮绑定；当前阶段点击 `CoreItem` 只负责打开 `CoreCargoPanel`、关闭 `FramePanel`、打开 `ModificationPanel`。
  - `ModificationPanel` 当前同步显示：
    - `CoreGroup`：当前选中模块上已安装的 Core
    - `ModuleGroup`：当前正在改造的 Module
  - `ModuleCargoDetailPanel` 当前作为模块选择流程右侧详情面板，显示当前槽位对应模块的基础信息与词条列表。

### 2026-05-16 Plugin System 增补

- 用户确认：`AssembleUI` 本轮任务先暂停，后续再做 `CorePanel` / 核心相关 UI。
- 新插件系统本轮决定不再扩旧 `Assets/Script/Player/IWeapon/*` 的 `Plugin/WeaponPlugin` 路径，改为以当前新链路为准：
  - `LoadoutModuleRuntimeData`
  - `LoadoutStatGraph`
  - `WeaponModuleBase`
  - `WeaponModuleEffectFactory`
- 已识别的旧遗留插件脚本与迁移结论：
  - `ChasePlugin`：语义保留，兼容映射到新 `Homing` 效果链。
  - `ReflectMod`：不再作为旧 prefab 脚本挂件使用，改为新系统中的近战反弹投射物能力。
  - `CriticalhitPlugin`：更适合作为插件数值修正，而不是旧随机脚本逻辑。
  - `ExplodePlugin` / `PenetrateMod`：当前仍是遗留空壳，尚未迁移到新 runtime。
- `PluginConfig` 已扩展出按品质配置的数值修正（`PluginStatModifierProfile` / `PluginStatModifier`），插件现在既可以：
  - 改模块基础数值
  - 也可以走 `effectId` / `pluginType` 提供特殊效果
- `LoadoutStatGraph` 已接入插件数值修正，当前 module 最终 stat = 模块基础值 + Core 修正 + Plugin 修正。
- 当前已可测的特殊效果通路：
  - 远程追踪：`WeaponModuleEffectFactory` 兼容 `Homing` / `Chase` / `ChasePlugin`
  - 近战反弹：`SawBladeModule` 在冲刺阶段可检测并反弹实现了 `IReflectableProjectile` 的敌方投射物
- 敌方投射物当前已接入统一反弹接口：
  - `EnemyBullet`
  - `EnemyProjectile`
- 当前新增枚举值：
  - `PluginType.ReflectProjectiles`
- 当前验证基线：
  - `dotnet build .\Assembly-CSharp.csproj -nologo`
  - `0 error`
  - `13 warning`
