# 创建 EggRogue 输入系统 - 详细步骤

我已经为你创建了一个新的 Input Action Asset 专门给 EggRogue 使用：`EggRogueInputActions.inputactions`

---

## 步骤 1：在 Unity 中导入 Input Action Asset

1. 在 Unity 的 Project 窗口中，找到 `Assets/EggRogue/Input/` 文件夹（如果不存在，Unity 会自动创建）
2. 你应该能看到 `EggRogueInputActions.inputactions` 文件

---

## 步骤 2：配置 Input Action Asset

1. **选中** `EggRogueInputActions.inputactions` 文件
2. 在 Inspector 中，找到 **"Generate C# Class"** 选项
3. **勾选** "Generate C# Class"（这样 Unity 会自动生成 C# 脚本）
4. Unity 会自动生成 `EggRogueInputActions.cs` 文件

---

## 步骤 3：验证生成

1. 等待 Unity 编译完成（看 Console 是否还有错误）
2. 在 Project 窗口中，你应该能看到 `EggRogueInputActions.cs` 文件（自动生成）
3. 如果有错误，右键 `EggRogueInputActions.inputactions` → **Reimport**

---

## 步骤 4：检查脚本引用

CharacterController 脚本已经更新为使用 `EggRogueInputActions` 而不是 `PlayerInputAction`。

检查脚本：
- 脚本中应该使用 `EggRogueInputActions`
- 脚本会自动订阅 `Move` 动作

---

## 步骤 5：测试

1. 运行游戏
2. 使用 **WASD** 或**方向键**移动角色
3. 应该可以正常移动了！

---

## 已配置的按键绑定

Input Action Asset 已经配置好了：
- **W 或 ↑**：向上移动
- **S 或 ↓**：向下移动
- **A 或 ←**：向左移动
- **D 或 →**：向右移动

---

## 常见问题

### Q: Console 还是报错说找不到 EggRogueInputActions？
A: 确保：
1. 选中 `EggRogueInputActions.inputactions` 文件
2. 在 Inspector 中勾选 **"Generate C# Class"**
3. 右键文件 → **"Reimport"**
4. 等待 Unity 重新编译

### Q: 按键不工作？
A: 检查：
1. 确保 `EggRogueInputActions.cs` 文件已生成
2. CharacterController 脚本是否正确挂载到角色上
3. Console 是否有其他错误

### Q: 想要修改按键绑定？
A: 
1. 双击 `EggRogueInputActions.inputactions` 打开编辑器
2. 找到 Move 动作的 Bindings
3. 修改对应的按键绑定
4. 点击 **Save Asset** 保存

---

## 后续扩展

如果以后需要添加其他输入（例如攻击、技能、跳跃等）：

1. 双击 `EggRogueInputActions.inputactions` 打开编辑器
2. 在 Player Action Map 中添加新动作
3. 配置键位绑定
4. 保存后，Unity 会自动更新 `EggRogueInputActions.cs`
5. 在 CharacterController 中订阅新动作的事件

---

完成以上步骤后，你的角色应该可以正常移动了！
