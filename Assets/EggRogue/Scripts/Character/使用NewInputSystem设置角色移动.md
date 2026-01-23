# 使用 New Input System 设置角色移动 - 详细步骤

本指南将一步步教你如何使用 Unity 的新 Input System 来配置角色移动。

---

## 步骤 1：打开 Input Action Asset

1. 在 Unity 的 Project 窗口中，找到 `Assets/Input/PlayerInputAction.inputactions` 文件
2. **双击**这个文件，会打开 Input Actions 编辑器窗口
   - 如果没打开，可以右键文件 → **Open with** → **Input Actions Editor**

---

## 步骤 2：添加 Move 动作

### 2.1 找到 Player Action Map

在 Input Actions 编辑器窗口中，你应该能看到：
- 左侧：**Action Maps** 列表（应该有 "Player"）
- 中间：**Actions** 列表（目前应该有 "Jump"）
- 右侧：**Bindings** 和 **Properties**

### 2.2 添加新的 Move 动作

1. 确保选中了 **"Player"** Action Map（左侧列表）
2. 点击中间的 **"Actions"** 列表下方的 **"+"** 按钮（或右键 Actions 列表 → **Add Action**）
3. 新动作会出现，默认名称是 "Action"，改为 **"Move"**

---

## 步骤 3：配置 Move 动作类型

### 3.1 设置动作类型为 Value（2D 向量）

1. 选中刚刚创建的 **"Move"** 动作
2. 在右侧的 **Properties** 面板中：
   - **Action Type**: 选择 **"Value"**（不是 Button 或 Pass Through）
   - **Control Type**: 选择 **"Vector2"**（2D向量，用于上下左右移动）

---

## 步骤 4：配置 Move 动作的键位绑定

### 4.1 添加 WASD 和方向键绑定

选中 **"Move"** 动作后，在右侧的 **Bindings** 面板：

1. 点击 **"+"** 按钮（在 Bindings 列表下方）
2. 选择 **"2D Vector Composite"**（这会自动创建 4 个方向的绑定）

### 4.2 配置各个方向的按键

创建 Composite 后，你会看到：
- **Up** (默认绑定可能是空白)
- **Down** (默认绑定可能是空白)
- **Left** (默认绑定可能是空白)
- **Right** (默认绑定可能是空白)

#### 配置 Up（上）：

1. 展开 **Up** 绑定
2. 点击 **"+"** 按钮
3. 选择 **"Keyboard"** → **"w"**
4. 再次点击 **"+"** → **"Keyboard"** → **"upArrow"**

现在 Up 应该有：`[Keyboard]w` 和 `[Keyboard]upArrow`

#### 配置 Down（下）：

1. 展开 **Down** 绑定
2. 点击 **"+"** 按钮
3. 选择 **"Keyboard"** → **"s"**
4. 再次点击 **"+"** → **"Keyboard"** → **"downArrow"**

#### 配置 Left（左）：

1. 展开 **Left** 绑定
2. 点击 **"+"** 按钮
3. 选择 **"Keyboard"** → **"a"**
4. 再次点击 **"+"** → **"Keyboard"** → **"leftArrow"**

#### 配置 Right（右）：

1. 展开 **Right** 绑定
2. 点击 **"+"** 按钮
3. 选择 **"Keyboard"** → **"d"**
4. 再次点击 **"+"** → **"Keyboard"** → **"rightArrow"**

---

## 步骤 5：保存并生成代码

1. 点击 Input Actions 编辑器窗口右上角的 **"Save Asset"** 按钮（或按 `Ctrl+S`）
2. Unity 会自动生成 C# 脚本（`PlayerInputAction.cs`），你应该能在 Console 看到提示

---

## 步骤 6：在 Unity 中确认生成

1. 关闭 Input Actions 编辑器窗口
2. 在 Project 窗口中，展开 `Assets/Input/` 文件夹
3. 你应该能看到更新后的 `PlayerInputAction.cs` 文件
4. 选中 `PlayerInputAction.inputactions` 文件，在 Inspector 中确认 **"Generate C# Class"** 已勾选

---

## 步骤 7：修改角色控制器脚本

现在 CharacterController 脚本已经更新为使用新的 Input System！

脚本会自动：
1. 创建一个 `PlayerInputAction` 实例
2. 启用 Player Action Map
3. 订阅 Move 动作的 `performed` 和 `canceled` 事件
4. 读取 Move 输入的 Vector2 值

---

## 步骤 8：测试

1. 运行游戏
2. 使用 **WASD** 或**方向键**移动角色
3. 应该可以正常移动了！

---

## 常见问题

### Q: Input Actions 编辑器窗口打不开？
A: 确保你已经安装了 Input System 包：
- **Window** → **Package Manager** → 搜索 "Input System" → 确保已安装

### Q: Move 动作找不到 Vector2 类型？
A: 确保：
1. Action Type 设置为 **"Value"**
2. Control Type 设置为 **"Vector2"**

### Q: 按键绑定不生效？
A: 检查：
1. 是否保存了 Input Action Asset（点击 Save Asset）
2. 是否勾选了 "Generate C# Class"
3. CharacterController 脚本中是否正确启用了 Player Action Map

### Q: 控制台报错说找不到 PlayerInputAction？
A: 确保：
1. `PlayerInputAction.cs` 文件存在（Unity 自动生成）
2. 如果文件不存在，手动在 Input Action Asset 的 Inspector 中勾选 **"Generate C# Class"**，然后右键文件 → **"Reimport"**

---

## 后续扩展（触屏输入）

如果以后需要添加触屏输入（虚拟摇杆），你可以：
1. 在 Move 动作的 Bindings 中添加新的绑定
2. 选择 **"Virtual Mouse"** 或自定义触屏输入
3. 脚本会自动支持（因为已经订阅了 Move 动作）

---

完成以上步骤后，你的角色应该可以通过新的 Input System 正常移动了！
