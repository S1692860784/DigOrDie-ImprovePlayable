# DigOrDie_Mod_ImprovePlayable (improve playable)

> 《Dig or Die》（挖或死）高可玩性扩展模组框架。
> 采用 **Module 领域解耦** 与 **JSON 双层数据驱动** 架构设计，支持免编译自由扩展怪物、矿脉、武器、电力与防御设施。

---

## 目录与架构

```text
├── Core/          # [核心生命周期] 模组入口 (ImprovePlayablePlugin)、编排中心 (ModManager)、数据与物理桥 (GameDataManager)、配方管理器 (RecipeManager)
├── Modules/       # [领域内容模块] Weapon/Block/Mining/Machine/Defense/Device/Monster/Recipe 等领域模块
├── Factories/     # [资产底层工厂] Block/Bullet/Defense/Device/Item/Monster/Surface/WeaponFactory
├── Patches/       # [Harmony 运行时切片补丁] 怪物 AI 修复、物理碰撞穿透、创造模式兼容等挂钩
├── Systems/       # [游戏子系统] 动态刷怪系统 (SpawnManager)、刺激剂环境系统 (StimEnvironmentManager)
├── Models/        # [数据实体] 强类型 JSON 实体配置 (MonsterConfig, ItemConfig, BulletConfig)
├── Debugging/     # [调试工具] 调试热键监听 (InputHandler)、实体诊断 (DebugHelper)
├── Common/        # [基础设施] JSON 容灾加载器 (JsonConfigHelper)、内嵌资源提取 (ResourceLoader)
└── Resources/     # [内置素材与配置模板] Items.json, Monsters.json, Recipes.json, PNG贴图
```

---

## 模块按需开关指南

若在启动游戏时不需要某些模块，提供以下两种轻量修改方式：

### 方式 1：通过 BepInEx 配置文件（开关整个功能模块）
首次启动后，在游戏目录的 `BepInEx/config/com.shuaji.digordie.improveplayable.cfg` 中直接将对应项改为 `false`：

```ini
[Content]
EnableBlocks = true       # 自定义建筑与方块
EnableDefenses = true     # 自动防御炮塔
EnableMachines = true     # 电力发电机与模组制造机
EnableWeapons = true      # 自定义武器与专属弹道
EnableDevices = true      # 功能装备（护甲、传送器、能量手电）
EnableOres = true         # 矿脉自然生成、材料与矿物探测器
EnableMonsters = true     # 自定义怪物生态与 AI
EnableRecipes = true      # 注入工作台合成配方
EnableLocalization = true # 中文本地化文本

[Debug]
EnableHotkeys = false     # 调试热键（F1~F12）
```

### 方式 2：通过 JSON 配置文件（精准移除/停用具体单品）
在 `BepInEx/config/ImprovePlayable/` 下找到对应 JSON 文件，直接删除不需要的条目即可，游戏启动时不会注册该物品。

---

## 底层功能扩展指南（如何添加新内容）

所有数据均支持在 `BepInEx/config/ImprovePlayable/` 外部 JSON 中热改，无需重新编译 C# 代码。

### 1. 新增一种怪物（Monsters.json）

在 `monsters` 数组中追加对象：
```json
{
  "codeName": "MyMod_FlameBat",
  "displayName": "熔岩蝙蝠",
  "textureResource": ".Resources.FlameLizard.png",
  "hp": 250,
  "speed": 7.5,
  "damage": 25,
  "range": 15.0,
  "attackCooldown": 1.2,
  "aiType": "FlyingRanged",
  "bulletTexture": ".Resources.PurpleBullet.png",
  "bulletSpeed": 18.0,
  "spawnMinY": 500.0,
  "spawnMaxY": 1200.0,
  "spawnSkyOnly": true
}
```
- **aiType 支持**：`Ground`（地面近战）、`Flying`（飞行冲刺）、`GroundRanged`（地面远程拉扯）、`FlyingRanged`（空中悬浮射击）。
- **底层特性**：AI 补丁已实现全向矢量控制与射程阻断，地面远程怪不会无脑冲撞玩家，飞行怪自带持续浮力补偿与视线锁定。

### 2. 新增矿石与自然矿脉生成（Items.json -> ores）

在 `ores` 数组中追加：
```json
{
  "codeName": "ImprovePlayable_TitaniumOre",
  "displayName": "钛矿石",
  "description": "高强度金属矿。耐久: 80",
  "health": 80,
  "textureResource": ".Resources.EnergyCrystalOre.png",
  "dropItemCodeName": "aluminium",
  "minDrop": 1,
  "maxDrop": 4,
  "replaceBlockCodeName": "hardRock",
  "spawnMinY": 600.0,
  "spawnMaxY": 1500.0,
  "veinSize": 10,
  "frequency": 0.15
}
```
- 底层会自动注册为自然方块，并挂钩世界生成算法，在深度 `spawnMinY` ~ `spawnMaxY` 替换指定的岩石块，且自动派生该矿石的专用探测器。

### 3. 新增电力设备 / 发电机（Items.json -> machines）

```json
{
  "codeName": "ImprovePlayable_SolarReactor",
  "displayName": "聚能核反应堆",
  "description": "持续输出高额清洁电力的发电机（15kW）。",
  "templateCodeName": "reactor",
  "electricValue": 15000
}
```
- **electricValue**：电力值。正数为**输出发电**（单位：瓦特 W，15000 即 15kW），负数为耗电。
- 底层已通过 `GameDataManager` 将其物理标记设置为 `m_isBlock = 0`，放置后正常通电且不阻碍角色移动与水流。

### 4. 新增自动防御炮塔（Items.json -> defenses）

```json
{
  "codeName": "ImprovePlayable_SniperTurret",
  "displayName": "高能狙击炮塔",
  "description": "超远距离等离子防御塔。伤害: 40 射程: 30",
  "hp": 100,
  "rangeDetection": 35.0,
  "attackRange": 30.0,
  "attackDamage": 40,
  "attackNb": 1,
  "attackCooldown": 1.5,
  "bulletTexture": ".Resources.PurpleBullet.png"
}
```
- 底层基于 `CItem_Defense` 扩展，拥有独立的攻击参数与专属弹道，物理层自动注册为可通行防御设施。

### 5. 新增合成配方（Recipes.json）

```json
{
  "resultCodeName": "ImprovePlayable_SniperTurret",
  "machineLevel": 0,
  "ingredients": [
    { "itemCodeName": "ImprovePlayable_EnergyCrystal", "count": 15 },
    { "itemCodeName": "iron", "count": 10 }
  ]
}
```
- **machineLevel**：`0` 代表模组专用制造台（modV1），`1`~`6` 对应原版工作台 MK I ~ ULTIMATE。
- 支持 1~3 种原版或模组材料任意组合。

---

## 可视化调试控制面板（按 F1 打开/关闭）

模组内置了轻量级、全功能的游戏内可视化调试窗口（可在屏幕上任意拖拽位置）：
- **玩家与世界标签页**：实时查看玩家坐标与血量、一键瞬移至鼠标光标处、满血治愈、10x 时间加速、手动触发兴奋剂事件。
- **物品给予标签页**：一键生成模组所有新武器、防具、电力发电机、防御炮塔、探测器与材料（支持 x1 / x20 批量获取）。
- **怪物生成标签页**：实时展示所有已注册怪物（含外部 JSON 动态加载的怪物），一键在身边生成 1 只 / 3 只进行实战测试。
- **矿物与方块标签页**：在鼠标光标处直接精准放置方块、强行触发世界矿脉生成测试。

*(若习惯纯热键，亦可继续使用 F5 快速给枪、F8 放矿、F9 刷矿脉、F11/F12 召唤怪)*
