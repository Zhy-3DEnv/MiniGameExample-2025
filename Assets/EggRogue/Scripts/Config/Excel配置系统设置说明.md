# Excel 配置系统设置说明

## 功能说明

本系统允许你使用 **Excel 表格**管理游戏配置，游戏运行时点击"更新"按钮即可重新读取配置，无需重新打包。

**工作流程：**
1. 在 Excel 中编辑配置表格
2. 另存为 CSV 文件
3. 将 CSV 放到 `StreamingAssets` 目录（打包进游戏）
4. 游戏运行时点击"更新配置"按钮，重新读取 CSV 并应用

---

## 一、创建 Excel 配置表格

### 步骤1：创建表格

在 Excel 中创建如下格式的表格：

| Key              | Value | Type  | Desc     |
|------------------|-------|-------|----------|
| PlayerDamage     | 10    | float | 玩家伤害 |
| PlayerAttackRange| 10    | float | 攻击范围 |
| PlayerFireRate   | 2     | float | 射速     |
| EnemyHealth      | 20    | float | 怪物血量 |
| EnemyMoveSpeed   | 3     | float | 怪物移速 |
| CoinDropMin      | 1     | int   | 最少金币 |
| CoinDropMax      | 2     | int   | 最多金币 |

**说明：**
- **Key**：配置项名称（必须唯一）
- **Value**：配置值
- **Type**：数据类型（`float`、`int`、`string`）
- **Desc**：描述（可选，仅用于说明）

### 步骤2：另存为 CSV

1. 在 Excel 中：**文件 > 另存为**
2. 格式选择：**CSV (逗号分隔)(*.csv)**
3. 命名为 `gameconfig.csv`
4. 保存

---

## 二、将 CSV 放入项目

### 方式1：手动复制（推荐）

1. 将 `gameconfig.csv` 复制到 `Assets/EggRogue/StreamingAssets/` 目录
2. 如果没有 `StreamingAssets` 文件夹，在 `Assets/EggRogue/` 下创建
3. 在 Unity 中，`StreamingAssets` 文件夹会被打包进游戏，运行时可以读取

### 方式2：使用编辑器工具

1. 在 Unity 菜单：**Tools > EggRogue > Excel 转 CSV 配置工具**
2. 选择你的 CSV 文件
3. 点击"转换"，会自动复制到 `Assets/EggRogue/Config/` 目录
4. 然后手动移动到 `StreamingAssets` 目录

---

## 三、设置 CSVConfigManager

### 步骤1：创建 CSVConfigManager 对象

1. 在 **GameScene** 中创建一个空物体，命名为 **CSVConfigManager**
2. 添加组件：`CSV Config Manager`
3. 在 Inspector 中配置：
   - `CSV File Name`: **gameconfig.csv**
   - `Allow Local File Read`: **勾选**（开发时可以从本地路径读取）
   - `Local File Path`: **留空**（或填写本地 CSV 完整路径，例如 `C:/Config/gameconfig.csv`）

### 步骤2：开发调试模式（可选）

如果你想在开发时从本地文件读取（不打包进游戏）：

1. 勾选 `Allow Local File Read`
2. 填写 `Local File Path`（例如：`C:/MyGameConfig/gameconfig.csv`）
3. 游戏会优先读取本地文件，如果没有则读取 StreamingAssets

---

## 四、创建更新配置按钮

### 步骤1：创建按钮

1. 在 **Canvas** 下创建一个 **Button**，命名为 `UpdateConfigButton`
2. 设置按钮文本为"更新配置"
3. 添加组件：`Config Update Button`

### 步骤2：连接引用

1. 选中 `UpdateConfigButton` 对象
2. 在 `Config Update Button` 组件中：
   - `Update Button`: 自动找到当前 Button（或手动拖入）
   - `Success Text`: （可选）创建一个 Text 显示"配置已更新"

### 步骤3：测试

1. 运行游戏
2. 点击"更新配置"按钮
3. 配置会重新从 CSV 文件读取并应用

---

## 五、使用流程

### 日常使用

1. **修改配置**：
   - 打开 Excel 文件
   - 修改参数（例如将 `PlayerDamage` 改为 15）
   - 另存为 CSV（覆盖原文件）

2. **更新到游戏**：
   - 将 CSV 复制到 `StreamingAssets` 目录（如果修改了本地文件）
   - 运行游戏
   - 点击"更新配置"按钮
   - 配置立即生效

### 打包后使用

1. **修改配置**：
   - 修改 Excel 并另存为 CSV
   - 将 CSV 放到游戏安装目录的 `StreamingAssets` 文件夹（或指定路径）

2. **更新配置**：
   - 运行游戏
   - 点击"更新配置"按钮
   - 配置从 StreamingAssets 读取并应用

---

## 六、CSV 文件格式说明

### 标准格式

```csv
Key,Value,Type,Desc
PlayerDamage,10,float,玩家伤害
EnemyHealth,20,float,怪物血量
CoinDropMin,1,int,最少金币
```

### 注意事项

1. **第一行是表头**，会被自动跳过
2. **Key 必须唯一**，不能重复
3. **Value 支持**：
   - 数字（整数或小数）
   - 字符串（如果包含逗号，需要用引号括起来）
4. **注释行**：以 `#` 开头的行会被忽略
5. **空行**：会被自动跳过

### 示例 CSV

```csv
Key,Value,Type,Desc
# 玩家配置
PlayerDamage,10,float,玩家单发伤害
PlayerAttackRange,10,float,攻击范围
PlayerFireRate,2,float,射速（每秒几发）

# 怪物配置
EnemyHealth,20,float,怪物最大生命值
EnemyMoveSpeed,3,float,怪物移动速度

# 掉落配置
CoinDropMin,1,int,最少掉落金币数
CoinDropMax,2,int,最多掉落金币数
```

---

## 七、支持的配置项

当前系统支持以下配置项（Key 名称）：

| Key              | 类型   | 默认值 | 说明           |
|------------------|--------|--------|----------------|
| PlayerDamage     | float  | 10     | 玩家单发伤害   |
| PlayerAttackRange| float  | 10     | 玩家攻击范围   |
| PlayerFireRate   | float  | 2      | 玩家射速       |
| EnemyHealth      | float  | 20     | 怪物最大生命值 |
| EnemyMoveSpeed   | float  | 3      | 怪物移动速度   |
| CoinDropMin      | int    | 1      | 最少掉落金币   |
| CoinDropMax      | int    | 2      | 最多掉落金币   |

**添加新配置项：**

1. 在 Excel 中添加新行
2. 在 `CSVConfigManager.ApplyConfig()` 中添加读取和应用逻辑
3. 在需要的地方调用 `CSVConfigManager.Instance.GetFloat("YourKey", defaultValue)`

---

## 八、文件路径说明

### StreamingAssets 路径

- **编辑器**：`Assets/EggRogue/StreamingAssets/gameconfig.csv`
- **打包后**：
  - Windows: `游戏目录/StreamingAssets/gameconfig.csv`
  - Mac: `游戏.app/Contents/StreamingAssets/gameconfig.csv`
  - 微信小游戏: `游戏数据目录/StreamingAssets/gameconfig.csv`

### 本地文件路径（开发调试）

- 可以填写任意本地路径，例如：`C:/MyConfig/gameconfig.csv`
- 仅在 `Allow Local File Read` 勾选时生效
- 优先级高于 StreamingAssets

---

## 九、常见问题

### Q: CSV 文件修改后，游戏读取不到新值？
A: 
- 确保 CSV 文件已保存
- 确保文件在 `StreamingAssets` 目录（或本地路径正确）
- 点击"更新配置"按钮重新读取
- 检查 Console 是否有错误信息

### Q: 微信小游戏无法读取 StreamingAssets？
A:
- 微信小游戏环境可能有限制
- 可以尝试使用 `Application.persistentDataPath` 路径
- 或者将 CSV 放在服务器，通过 HTTP 请求下载

### Q: 如何添加新的配置项？
A:
1. 在 Excel 中添加新行（Key, Value, Type, Desc）
2. 在 `CSVConfigManager.ApplyConfig()` 中添加读取和应用代码
3. 在需要的地方调用 `GetFloat("YourKey", defaultValue)`

### Q: 配置值类型错误怎么办？
A:
- 检查 CSV 中的 `Type` 列是否正确
- 确保 `Value` 列的值可以转换为对应类型
- 如果转换失败，会使用默认值

---

## 十、优势

1. **Excel 编辑**：使用熟悉的 Excel 工具，方便批量修改
2. **无需打包**：修改 CSV 后，游戏运行时点击"更新"即可
3. **跨平台**：CSV 是纯文本格式，所有平台都支持
4. **版本控制**：CSV 文件可以用 Git 管理
5. **易于扩展**：添加新配置项只需在 Excel 中添加一行

---

完成以上设置后，你就可以用 Excel 管理配置，游戏运行时点击"更新配置"按钮即可生效！
