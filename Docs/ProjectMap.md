# Project Map

> 本文件只记录项目结构和目录职责。  
> 不记录任务进度、历史记忆、待办、入口流程细节。  
> 入口流程请看 `ProjectEntryPoints.md`；协作记忆和当前进度请看 `ProjectMemory.md`。

## 项目概览

`NeonPulse` 是一个 Unity 2D 动作项目，当前代码库同时保留了早期 GameJam 框架能力和正在重建的 `NeonPulse` 局外组装 / 局内循环玩法系统。

当前项目里并存两套历史层级：

- 旧运行时模块 / 敌人 / 波次 / UI 体系：仍有兼容脚本和场景序列化引用残留。
- 新组装与 InRun 体系：以 `GameConfigDatabase`、Frame / Module / Core / Plugin 配置、`Run.loadout`、`Assets/Script/InRun/` 为中心。

## 根目录结构

```text
NeonPulse/
├── Assets/                  Unity 主资源、场景、脚本、配置、预制体
├── Docs/                    系统设计文档
├── Packages/                Unity package 依赖
├── ProjectSettings/         Unity 项目设置
├── ProjectMap.md            项目结构图，只记录目录和模块职责
├── ProjectEntryPoints.md    系统入口、流程入口、调试入口索引
├── ProjectMemory.md         任务记忆、当前进度、协作偏好、历史决策
└── README.md                早期 Unity GameJam Framework 说明
```

生成目录如 `Library/`、`Temp/`、`Logs/`、`obj/` 不应作为项目结构记录重点。

## Docs

```text
Docs/
└── NeonPulse_InRunSystemDesign.md
```

- `NeonPulse_InRunSystemDesign.md`
  - InRun 系统设计文档。
  - 用于记录局内循环的更完整设计，而不是代码目录地图。

## Assets 顶层结构

```text
Assets/
├── Plugins/                 第三方插件，例如 DOTween、ProCamera2D、Odin Inspector
├── Resources/               运行期通过 Resources 加载的配置、预制体、音频、美术等
├── Scenes/                  Unity 场景
└── Script/                  游戏和框架脚本
```

## Assets/Resources 结构

```text
Assets/Resources/
├── Animation/               动画资源
├── Arts/                    美术资源
├── Audio/                   音频资源
├── Configs/                 ScriptableObject 配置资源
├── JMO Assets/              外部 / 美术资源包内容
├── Materials/               材质
├── ParticleSystem/          粒子资源
├── Prefabs/                 运行期和 UI 预制体
├── Shaders/                 Shader 资源
├── Text/                    文本资源
├── Texture/                 贴图资源
└── UIResource/              UI 相关资源
```

重要约定：

- `GameConfigDatabase` 从 `Resources/Configs/GameConfigDatabase` 加载。
- Module / Frame / Core / Plugin / Stat 等配置资源集中放在 `Resources/Configs/` 相关子目录。
- 运行期 Player、Frame Core、Module、UI 等动态加载对象集中放在 `Resources/Prefabs/` 相关子目录。

## Assets/Script 结构

```text
Assets/Script/
├── BaseScript/              基础类，例如 UIBase、GameState
├── CoreScript/              核心管理器、数据模型、服务、工具
├── Enemys/                  敌人行为、边界行为、兼容敌人系统
├── GameStatus/              高层游戏状态，例如 Menu、Assemble、MainGame
├── InRun/                   新局内循环系统
├── Interface/               通用接口
├── Physics/                 通用物理辅助层
├── Player/                  玩家控制器、模块、子弹、旧模块行为
├── Tools/                   杂项工具
├── UI/                      UI 面板脚本
└── VFXEffect/               背景和视觉效果脚本
```

## Assets/Script/CoreScript 结构

```text
Assets/Script/CoreScript/
├── Data/                    存档数据、配置数据、运行时数据模型
├── Editor/                  编辑器辅助脚本
├── GameDataTool/            Unity 可编译版数值组合工具
├── Manager/                 Data/UI/Audio/Loadout/Player/GameMgr 等管理器
├── Services/                独立服务层
├── StaticManager/           静态管理器 / 框架级工具
└── Tools/                   核心工具类
```

### CoreScript/Data

```text
Assets/Script/CoreScript/Data/
├── CoreData/                SaveRoot、RunSaveData、MetaProgressData 等核心数据
├── LoadoutData/             组装 / loadout / runtime stat 相关数据
├── ModData/                 模组相关数据
├── ModuleData/              旧模块数据残留或兼容目录
├── ShakePreset/             相机 / 震动配置
├── VisualThemePreset/       背景视觉主题配置
├── BossConfig.asset         旧 Boss 配置资源
├── CharacterConfig.cs       角色配置类型
└── MaskConfig.cs            面具配置类型
```

## Assets/Script/InRun 结构

```text
Assets/Script/InRun/
├── Boss/                    Boss 遭遇、Boss 场地限制、Boss 配置
├── Core/                    InRunDirector、流程 runner、恢复协调、runtime context
├── Loop/                    战斗循环计时和 loop runtime 数据
├── Pulse/                   Pulse 触发 / 清场 / 结算入口
├── Reward/                  loop reward / boss reward 奖励流程
├── Score/                   分数和评级结算
├── Shop/                    商店、商品、商店库存快照
├── Spawn/                   主题敌人池、刷怪预算、刷怪点、敌人缩放
├── Theme/                   BattleThemeConfig 和主题运行时数据
└── UI/                      InRun 调试 HUD / 运行时 HUD
```

## Assets/Script/GameStatus 结构职责

- `MenuState`
  - 主菜单状态。
  - 打开 `StartUI`。
- `AssembleGameState`
  - 组装状态。
  - 打开 `AssembleUI`。
- `MainGameState`
  - 局内运行状态。
  - 准备 run snapshot、生成运行时玩家、启动 InRun。
- 其他状态脚本
  - 保留早期框架或兼容状态逻辑。

## Assets/Script/UI 结构职责

- `StartUI`
  - 主菜单入口 UI。
- `AssembleUI`
  - 当前主要组装 UI。
- 旧局内 UI
  - `HUDUI`、`HpBarUI`、`ExpBarUI`、`LevelUpUI`、`MaskGachaUI`、`GameOverUI`、`EndUI`、`PreviewGetConfigUI` 等属于旧运行时 UI 残留或兼容层。

## Assets/Script/Player 结构职责

- `PlayerController`
  - 玩家移动和物理输入承载。
- `PlayerBullet`
  - 当前子弹行为，支持从 projectile spawn data 接收参数。
- `Modules/`
  - 旧 `PlayerModule` 体系和逐步迁移中的新 weapon module 基类。
  - 当前仍承担运行时 module prefab 行为。

## Assets/Script/Enemys 结构职责

- `EnemyBase`
  - 敌人基础行为和运行时注册点。
- 具体敌人脚本
  - Bomber、Chaser、Dasher、Drifter、Shooter、Boss 等。
- 边界行为
  - `EnemyBoundaryService`
  - `EnemyBoundaryConstraint`
  - `EnemyBoundaryReaction`
  - 旧 `EnemyManager` 仍作为兼容壳存在。

## Assets/Script/Physics 结构职责

- `ContinuousPhysicsMotor2D`
  - 玩家和普通敌人的共享连续速度物理层。
  - 负责目标速度、冲量、边界夹取时的速度修正。

## GameDataTool 结构

```text
Assets/Script/CoreScript/GameDataTool/
├── SealedValue.cs
├── ModifierGroup.cs
└── SealedGameData.cs

GameDataTool/
├── SealedValue.cs
├── ModifierGroup.cs
├── SealedGameData.cs
└── Example.cs
```

- `Assets/Script/CoreScript/GameDataTool/` 是 Unity 编译路径中的实际运行版本。
- 根目录 `GameDataTool/` 更像源参考 / 示例副本。
- 该工具用于数值组合，例如 module/core/plugin/stat 的运行时数值管线。
