# Project Entry Points

> 本文件只记录“从哪里进、调用链怎么走、调试时该看哪里”。  
> 不记录历史任务记忆；记忆请放 `ProjectMemory.md`。  
> 不记录完整目录地图；结构请放 `ProjectMap.md`。

## 文档入口

- 项目结构：`ProjectMap.md`
- 系统入口索引：`ProjectEntryPoints.md`
- 协作记忆 / 当前进度：`ProjectMemory.md`
- InRun 系统设计：`Docs/NeonPulse_InRunSystemDesign.md`
- 早期框架说明：`README.md`

## 启动 / 管理器入口

### 中央管理器

- 文件：`Assets/Script/CoreScript/Manager/GameMgr.cs`
- 职责：新代码优先通过 `GameMgr.Instance` 访问核心 manager。
- 常用访问：
  - `GameMgr.Instance.Data`
  - `GameMgr.Instance.UI`
  - `GameMgr.Instance.Audio`
  - `GameMgr.Instance.Loadout`
  - `GameMgr.Instance.Player`
  - `GameMgr.Instance.Get<T>()`

### UI 管理器特殊入口

- 文件：`Assets/Script/CoreScript/Manager/UIManager.cs`
- 注意：`UIManager` 必须在 Canvas 上，不应该被普通挂在 `GameMgr` 根节点下。
- 层级：
  - `Layer_FullScreen`
  - `Layer_Panel`
  - `Layer_Popup`

## 游戏状态入口

### 主菜单入口

```text
GameManager / GameMgr
└── MenuState
    └── StartUI
```

- `MenuState`
  - 文件：`Assets/Script/GameStatus/MenuState.cs`
  - 进入时打开 `StartUI`。
- `StartUI`
  - 文件：`Assets/Script/UI/StartUI.cs`
  - 新游戏：进入 `AssembleGameState`。
  - 继续游戏：进入 `MainGameState(true)`。

### 组装入口

```text
StartUI
└── AssembleGameState
    └── AssembleUI
        └── LoadoutManager / LocalLoadoutProvider
            └── DataManager.Meta.frameLoadouts
```

- `AssembleGameState`
  - 文件：`Assets/Script/GameStatus/AssembleGameState.cs`
  - 新游戏进入组装前会清理 stale active run，避免组装 UI 写到旧 `Run.loadout`。
- `AssembleUI`
  - 文件：`Assets/Script/UI/AssembleUI.cs`
  - 负责选择 frame、slot、module、core。
  - 通过 `GameMgr.Instance.Loadout` 读写当前 frame loadout。
  - 组装完成后进入 `MainGameState`。

### 运行入口

```text
AssembleUI / StartUI Continue
└── MainGameState
    ├── DataManager.StartNewRun(...) 或恢复已有 Run
    ├── PlayerManager materialize runtime player
    └── InRunDirector.BeginRun(isContinue)
```

- `MainGameState`
  - 文件：`Assets/Script/GameStatus/MainGameState.cs`
  - 新开局：准备 run snapshot。
  - 继续：恢复已有 run snapshot。
  - 运行时：生成玩家、启动 InRun。
  - 退出：保存玩家位置 / hp 等 run 数据。

## 存档 / 数据入口

### 总存档入口

- 文件：`Assets/Script/CoreScript/Manager/DataManager.cs`
- 核心对象：
  - `SaveRoot`
  - `MetaProgressData`
  - `RunSaveData`
  - `RunLoadoutData`
  - `InRunRuntimeSaveData`

### 持久组装数据

```text
DataManager.Meta
└── frameLoadouts
    └── FrameLoadoutSaveData
        └── SlotSaveData
```

用途：局外长期装配状态。

### 单局快照数据

```text
DataManager.Run
├── loadout
│   └── RunLoadoutData
└── inRun
    └── InRunRuntimeSaveData
```

用途：单局运行时快照，包括当前装配、InRun 阶段、货币、奖励、商店库存、仓库等。

### 新开局快照入口

- 方法：`DataManager.StartNewRun(seed, frameId)`
- 作用：从 `Meta.frameLoadouts` 复制当前 frame 的装配到 `Run.loadout`。
- 规则：`Meta` 是局外可编辑源；`Run.loadout` 是单局快照。

## Loadout / 组装入口

### 主要访问层

- `Assets/Script/CoreScript/Manager/LoadoutManager.cs`
  - 对 UI 和运行时提供统一 loadout facade。
- `Assets/Script/CoreScript/Data/CoreData/LocalPlayerProgressionProvider.cs`
  - 当前本地玩家 loadout provider。
  - 非 run 状态写 `Meta.frameLoadouts`。
  - active run 状态写 `Run.loadout`。

### Runtime 解析入口

- `Assets/Script/CoreScript/Data/LoadoutData/LoadoutModuleRuntimeData.cs`
  - 单个 module/core/plugin 的运行时数据包装。
- `Assets/Script/CoreScript/Data/LoadoutData/LoadoutStatGraph.cs`
  - 使用 `GameDataTool` 构建 stat 计算图。
- `LoadoutModuleRuntimeBuilder`
  - 从保存数据和配置生成可运行的 module runtime 数据。

### Module 身份规则

- 新路径优先使用：`moduleId` / `ModuleConfig`。
- `ModuleType` 仍可能作为旧兼容桥存在，但不应再作为唯一身份扩展新功能。
- slot 兼容性优先看 category / config 规则，而不是把 `ModuleType` 当唯一身份。

## 配置入口

### 主数据库

- 资源路径：`Assets/Resources/Configs/GameConfigDatabase`
- 类型：`GameConfigDatabase`
- 主要内容：
  - `FrameConfig`
  - `ModuleConfig`
  - `CoreConfig`
  - `PluginConfig`
  - Stat definitions / schemas

### InRun 数据库

- 类型：`InRunConfigDatabase`
- 主要内容：
  - `BattleThemeConfig`
  - `CombatLoopGlobalConfig`
  - `ScoreConfig`
  - `PulseConfig`

### 主题配置

- 类型：`BattleThemeConfig`
- 用于驱动：
  - theme id / display name
  - 背景视觉 preset
  - enemy pool
  - loop enemy plans
  - shop catalog
  - boss config

## 组装预览入口

```text
AssembleUI
└── PreviewManager.ShowAssemblyPreview(snapshot)
    └── AssemblyLoadoutPreviewHost
        ├── clone Player.prefab
        ├── mount frame core visual
        └── instantiate ModuleConfig.previewPrefab
```

- `Assets/Script/CoreScript/Manager/AssemblyLoadoutPreviewHost.cs`
  - 当前组装预览实体 host。
  - 预览模块使用 `ModuleConfig.previewPrefab`。
  - module prefab 默认 transform 被视为作者权威，不再强制归零。
  - 预览实例会剥离 gameplay MonoBehaviour，只保留视觉 / passive shell。

## 运行时玩家实体入口

```text
MainGameState
└── PlayerManager
    ├── instantiate Player.prefab
    ├── mount frame core under Player/Core
    ├── instantiate equipped module prefabs under Player/Modules
    └── register spawned modules with ModuleManager
```

- `Assets/Script/CoreScript/Manager/PlayerManager.cs`
  - 当前负责把 `Run.loadout` 实体化成运行时 player。
- `Assets/Resources/Prefabs/Mono/Player/Player.prefab`
  - 运行时 player 预制体。
- `Assets/Resources/Prefabs/Mono/Frame/Core/Core_{FramePrefabName}.prefab`
  - frame core visual 主要解析路径。
- `ModuleConfig.previewPrefab` / module runtime prefab
  - 当前模块视觉和运行时行为仍与旧 `PlayerModule` 体系有耦合。

## 武器 / 子弹入口

### 基础类

- `Assets/Script/Player/Modules/PlayerModule.cs`
- `Assets/Script/Player/Modules/WeaponModuleBase.cs`
- `Assets/Script/Player/Modules/ProjectileWeaponModule.cs`

### 当前主武器入口

- `Assets/Script/Player/Modules/OriginShooterModule.cs`
- 当前 stat contract：
  - `weapon.damage`
  - `weapon.shotspeed`
  - `weapon.critchance`
  - `weapon.critdamage`
  - `weapon.weaponcount`

### 子弹入口

- `Assets/Script/Player/PlayerBullet.cs`
- 通过 `IProjectileSpawnReceiver` 接收 `ProjectileSpawnData`。
- 当前已有第一版 homing projectile 支持。

### 插件效果入口

- `WeaponModuleEffectFactory`
- 当前已接入：`PluginType.Homing` / `effectId == "Homing"`。
- 后续额外枪口、分裂、反弹等效果应继续走 `IWeaponModuleEffect`，不要回到 `OriginShooterModule` 硬编码。

## InRun 入口

### 总入口

```text
MainGameState
└── InRunDirector.GetOrCreate()
    └── BeginRun(isContinue)
```

- 文件：`Assets/Script/InRun/Core/InRunDirector.cs`
- 当前持有的运行时子系统：
  - `CombatLoopController`
  - `PulseSystem`
  - `EnemySpawnDirector`
  - `EnemyBoundaryService`
  - `BossEncounterDirector`
  - `RewardDirector`
  - `ShopDirector`
  - `InRunFlowRunner`
  - `InRunResumeCoordinator`

### 新开局流程入口

```text
InRunDirector.BeginRun(false)
└── InRunFlowRunner.RunFreshStateFlow(this)
```

### 恢复流程入口

```text
InRunDirector.BeginRun(true)
└── InRunResumeCoordinator.ResumeStateFlow(this)
```

### 当前 phase 入口

主要枚举：`InRunPhase`

常见阶段：

```text
Bootstrap
ThemeSelecting
ThemeIntro
CombatLoopPreparing
CombatLoopActive
CombatLoopComplete
PulseReady
PulseResolving
LoopReward
Shop
BossPreparing
BossActive
BossReward
NextTheme
RunEnded
```

## InRun 战斗循环入口

```text
InRunDirector.RunCombatLoop(resumeTimer)
├── EnterState(CombatLoopActive)
├── CombatLoopController.StartLoop(...)
├── PulseSystem.Arm(...)
├── EnemySpawnDirector.BeginLoop(...)
├── wait loop complete or pulse triggered
├── EnemySpawnDirector.StopLoop()
└── EnterState(CombatLoopComplete)
```

- `CombatLoopController`
  - 管理 loop timer。
- `PulseSystem`
  - 管理 pulse armed / trigger。
- `EnemySpawnDirector`
  - 管理 loop 内刷怪。

## InRun 刷怪入口

- 文件：`Assets/Script/InRun/Spawn/EnemySpawnDirector.cs`
- 输入：
  - `BattleThemeConfig`
  - `CombatLoopGlobalConfig`
  - theme index
  - loop index
  - normalized loop time
- 关键职责：
  - 根据 theme enemy pool 和 loop enemy plan 选怪。
  - 按 spawn budget 和 active threat cap 控制刷怪。
  - 通过 off-camera ring spawn 选择生成点。
  - 应用 `EnemyRuntimeSpawnData` 缩放 hp / speed / score / threat。

## InRun Pulse / Reward / Shop 入口

### Pulse 到奖励

```text
InRunDirector.RunPulseAndReward(...)
├── PulseReady
├── PulseResolving
├── EnemySpawnDirector.DespawnAllTrackedEnemies()
├── RunLoopRewardPhase(...)
└── RunShopPhase(...)
```

### 奖励入口

- 文件：`Assets/Script/InRun/Reward/RewardDirector.cs`
- loop reward：`OpenLoopReward(...)`
- boss reward：`OpenBossReward(...)`
- 当前仍偏调试交互，后续需要正式 UI。

### 商店入口

- 文件：`Assets/Script/InRun/Shop/ShopDirector.cs`
- 入口：`OpenShop(theme, runtime)`
- 主要行为：
  - 从 `BattleThemeConfig.shopCatalog` 生成商品。
  - 没有 catalog 时生成 placeholder 商品。
  - 可从 `runtime.shopInventory` 恢复商品快照。
  - 购买后扣 `runtime.runCurrency`。
  - 商品写入 `pendingRewards`。
  - 非货币商品尝试写入运行期 warehouse。

## Boss 入口

```text
InRunDirector.RunBossFresh(themeIndex)
├── BossPreparing
├── EnemySpawnDirector.DespawnAllTrackedEnemies()
├── BossEncounterDirector.BeginEncounter(...)
├── BossActive
├── wait boss complete
├── BossEncounterDirector.CleanupEncounter()
├── context.MarkBossDefeated()
└── RunBossRewardPhase(...)
```

- `Assets/Script/InRun/Boss/BossEncounterDirector.cs`
- `Assets/Script/InRun/Boss/BossEncounterConfig.cs`
- `Assets/Script/InRun/Boss/BossArenaLimiter.cs`

当前 boss arena 限制走玩家位置 clamp，不再创建物理墙 collider。

## 敌人边界入口

- `Assets/Script/Enemys/EnemyBoundaryService.cs`
  - 新的集中边界服务。
- `Assets/Script/Enemys/EnemyBoundaryConstraint.cs`
  - 持续约束，例如不能离开边界。
- `Assets/Script/Enemys/EnemyBoundaryReaction.cs`
  - 触发反应，例如 teleport / death / bounce。
- `EnemyBase`
  - InRun active 时向 `InRunDirector` 注册边界感知。
- `EnemyManager`
  - 仍保留为兼容壳。

## 物理运动入口

- `Assets/Script/Physics/ContinuousPhysicsMotor2D.cs`
- 使用方：
  - `PlayerController`
  - 多数普通敌人 `EnemyBase` 子类
- 能力：
  - desired velocity
  - impulse
  - angular damping
  - bounds clamp
  - clamp 时修正向外速度和 desired velocity

## 调试 / 临时输入入口

当前部分 InRun 交互仍是 debug input：

- `InRunHUD`
  - OnGUI debug HUD。
- Pulse：
  - 默认按 `R`。
- Reward：
  - 数字键选择奖励。
- Shop：
  - 数字键购买商品。
  - `Space` / `Return` / `N` 结束商店。

这些入口后续应由正式 UI 替换。

## 构建验证入口

常用命令：

```bash
dotnet build Assembly-CSharp.csproj --no-restore
```

或：

```bash
dotnet build .\Assembly-CSharp.csproj -nologo
```

当前历史记录中，多次验证为 `0 error`，剩余 warning 多为旧 Enemy / Player / UI / boss utility 脚本警告。
