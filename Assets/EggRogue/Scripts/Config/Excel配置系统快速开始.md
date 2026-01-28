# Excel 配置系统 - 快速开始

## 5 分钟快速设置

### 步骤1：创建 Excel 表格

1. 打开 Excel，创建如下表格：

```
Key              | Value | Type  | Desc
PlayerDamage     | 10    | float | 玩家伤害
EnemyHealth      | 20    | float | 怪物血量
CoinDropMin      | 1     | int   | 最少金币
CoinDropMax      | 2     | int   | 最多金币
```

2. **另存为 CSV**：文件 > 另存为 > 格式选择 "CSV (逗号分隔)(*.csv)"
3. 命名为 `gameconfig.csv`

### 步骤2：放入项目

1. 在 Unity Project 窗口，找到 `Assets/EggRogue/StreamingAssets/` 目录
2. 如果没有 `StreamingAssets` 文件夹，创建它：
   - 右键 `Assets/EggRogue/` > Create > Folder
   - 命名为 `StreamingAssets`（注意大小写）
3. 将 `gameconfig.csv` 拖到 `StreamingAssets` 文件夹

### 步骤3：设置 CSVConfigManager

1. 在 **GameScene** 中创建空物体，命名为 `CSVConfigManager`
2. 添加组件：`CSV Config Manager`
3. 配置：
   - `CSV File Name`: **gameconfig.csv**
   - `Allow Local File Read`: **勾选**（开发调试用）
   - `Local File Path`: **留空**（或填写本地 CSV 完整路径）

### 步骤4：创建更新按钮（可选）

1. 在 Canvas 下创建 Button，文本为"更新配置"
2. 添加组件：`Config Update Button`
3. 运行游戏，点击按钮即可更新配置

---

## 开发调试模式（从本地文件读取）

如果你想从电脑本地路径读取 CSV（不打包进游戏）：

1. 在 `CSVConfigManager` 组件中：
   - 勾选 `Allow Local File Read`
   - 填写 `Local File Path`，例如：`C:/MyGameConfig/gameconfig.csv`
2. 修改 Excel 后，另存为 CSV 到该路径
3. 游戏运行时点击"更新配置"，会从本地文件读取

---

## 使用流程

1. **修改配置**：打开 Excel → 修改参数 → 另存为 CSV
2. **更新到游戏**：
   - 如果使用 StreamingAssets：复制 CSV 到 `Assets/EggRogue/StreamingAssets/`
   - 如果使用本地路径：保存 CSV 到指定路径
3. **应用配置**：运行游戏 → 点击"更新配置"按钮

---

## 支持的配置项

| Key              | 类型   | 说明           |
|------------------|--------|----------------|
| PlayerDamage     | float  | 玩家单发伤害   |
| PlayerAttackRange| float  | 玩家攻击范围   |
| PlayerFireRate   | float  | 玩家射速       |
| EnemyHealth      | float  | 怪物最大生命值 |
| EnemyMoveSpeed   | float  | 怪物移动速度   |
| CoinDropMin      | int    | 最少掉落金币   |
| CoinDropMax      | int    | 最多掉落金币   |

---

完成！现在你可以用 Excel 管理配置，游戏运行时点击"更新配置"即可生效。
