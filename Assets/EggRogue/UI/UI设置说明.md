# UI系统设置说明

本文档说明如何在Unity中设置UIManager和两个Panel，让UI系统正常工作。

---

## 一、创建UI根结构（UIRoot）

### 步骤1：在MainMenu场景中创建UIRoot

1. 在MainMenu场景中，创建一个空物体，命名为 `UIRoot`
2. 在 `UIRoot` 下添加组件：
   - `Canvas`（Render Mode: Screen Space - Overlay）
   - `Canvas Scaler`（UI Scale Mode: Scale With Screen Size，Reference Resolution: 1920x1080）
   - `Graphic Raycaster`
3. 在 `UIRoot` 下创建一个空物体，命名为 `EventSystem`
   - 添加组件：`EventSystem`
   - 添加组件：`Input System UI Input Module`（删除旧的 StandaloneInputModule）

### 步骤2：创建MainMenuPanel

1. 在 `UIRoot` 下创建一个 `Panel`（右键 UIRoot > UI > Panel），命名为 `MainMenuPanel`
2. 在 `MainMenuPanel` 下创建一个 `Button`（右键 MainMenuPanel > UI > Button - TextMeshPro），命名为 `StartGameButton`
   - 修改按钮文本为"开始游戏"
3. 选中 `MainMenuPanel`，添加组件：`MainMenuPanel`（脚本）
4. 在 Inspector 中，将 `StartGameButton` 拖到 `Start Game Button` 字段

### 步骤3：创建GameHudPanel

1. 在 `UIRoot` 下创建一个 `Panel`，命名为 `GameHudPanel`
2. 在 `GameHudPanel` 下创建一个 `Button`，命名为 `ReturnToMenuButton`
   - 修改按钮文本为"返回主菜单"
3. 选中 `GameHudPanel`，添加组件：`GameHudPanel`（脚本）
4. 在 Inspector 中，将 `ReturnToMenuButton` 拖到 `Return To Menu Button` 字段

### 步骤4：设置UIManager

1. 在 `UIRoot` 上添加组件：`UIManager`（脚本）
2. 在 Inspector 中：
   - 将 `MainMenuPanel` 拖到 `Main Menu Panel` 字段
   - 将 `GameHudPanel` 拖到 `Game Hud Panel` 字段

### 步骤5：设置UIRoot常驻

1. 选中 `UIRoot`，在 Inspector 中勾选 `Dont Destroy On Load`（或添加一个脚本调用 `DontDestroyOnLoad`）
   - 注意：UIManager脚本内部已经处理了DontDestroyOnLoad，但为了保险，你也可以手动设置

---

## 二、设置GameManager

1. 在MainMenu场景中，找到你的 `GameManager` 对象
2. 确保 `GameManager` 脚本中的场景名称配置正确：
   - `Main Menu Scene Name`: `MainMenu`
   - `Game Scene Name`: `GameScene`

---

## 三、测试流程

1. 运行游戏，应该看到主菜单界面（MainMenuPanel显示，GameHudPanel隐藏）
2. 点击"开始游戏"按钮，应该：
   - 切换到GameScene场景
   - UI自动切换为GameHudPanel（主菜单隐藏，游戏HUD显示）
3. 在GameScene中点击"返回主菜单"按钮，应该：
   - 切换回MainMenu场景
   - UI自动切换回MainMenuPanel

---

## 四、常见问题

### Q: 场景切换后UI没有自动切换？
A: 检查：
- UIManager.Instance 是否存在（查看Console是否有警告）
- GameManager的OnSceneLoaded回调是否正常触发
- 场景名称是否与GameManager中配置的一致

### Q: 按钮点击没有反应？
A: 检查：
- EventSystem是否存在，且使用的是 `Input System UI Input Module`
- 按钮的引用是否正确拖入到Panel脚本的Inspector字段中
- Canvas的Graphic Raycaster组件是否存在

### Q: UI在场景切换后消失了？
A: 确保：
- UIRoot（或挂载UIManager的对象）设置了DontDestroyOnLoad
- UIManager脚本的Awake方法正常执行（检查Console日志）

---

## 五、后续扩展

当你需要添加新的UI面板时：

1. 创建一个新的Panel脚本，继承 `BaseUIPanel`
2. 在Unity中创建对应的Panel Prefab
3. 在 `UIManager` 中添加该Panel的引用字段
4. 在 `UIManager` 中添加 `ShowXXX()` 方法
5. 在需要显示该面板的地方调用 `UIManager.Instance.ShowXXX()`

---

## 六、目录结构建议

```
Assets/EggRogue/
├── Scripts/
│   └── UI/
│       ├── UIManager.cs
│       ├── BaseUIPanel.cs
│       ├── MainMenuPanel.cs
│       └── GameHudPanel.cs
└── UI/
    ├── Prefabs/          （后续可以将Panel做成Prefab）
    │   ├── MainMenuPanel.prefab
    │   └── GameHudPanel.prefab
    └── UI设置说明.md     （本文件）
```
