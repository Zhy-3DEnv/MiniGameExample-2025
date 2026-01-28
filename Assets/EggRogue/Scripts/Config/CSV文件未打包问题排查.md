# CSV 文件未打包问题排查

## 问题现象

1. ✅ CSV 文件存在于：`Assets/EggRogue/StreamingAssets/gameconfig.csv`
2. ❌ 构建输出目录中没有：`D:/Project/MiniGameExample/Builds/EggRogue/StreamingAssets/gameconfig.csv`
3. ❌ CDN 返回 404：`{"code":5,"details":["not found"],"reason":"Not found"}`

## 可能原因

### 原因1：DeleteStreamingAssets 配置导致文件被删除

配置中 `DeleteStreamingAssets: 1` 表示构建后删除本地 StreamingAssets。

**影响**：
- 构建过程中，StreamingAssets 会被复制到输出目录
- 但构建完成后，可能会被删除
- 这导致 Instant Game Build & Upload 找不到文件上传

### 原因2：bundleExcludeExtensions 可能影响

配置中 `bundleExcludeExtensions: json;` 只排除了 json，但 CSV 可能也需要特殊处理。

### 原因3：Instant Game Build & Upload 配置问题

Instant Game 的 Build & Upload 可能没有正确识别 StreamingAssets 中的 CSV 文件。

---

## 解决方案

### 方案1：临时禁用 DeleteStreamingAssets（推荐用于测试）

1. 打开 `Assets/WX-WASM-SDK-V2/Editor/MiniGameConfig.asset`
2. 找到 `DeleteStreamingAssets: 1`
3. 改为 `DeleteStreamingAssets: 0`
4. 重新执行"生成并转化"
5. 检查构建输出目录，确认 CSV 文件存在
6. 执行 Build & Upload
7. 验证 CDN 访问

**注意**：这会导致包体增大，但可以确保文件被正确上传。

### 方案2：检查 Instant Game Build & Upload 配置

1. Unity 菜单：**Services > Instant Game > Build & Upload**
2. 在窗口中检查：
   - **Source Path**：应该指向构建输出目录
   - **StreamingAssets**：应该被包含在上传列表中
   - 查看上传日志，确认 CSV 文件是否被识别

### 方案3：手动上传 CSV 到 CDN（临时方案）

如果 Instant Game Build & Upload 无法上传 CSV，可以：

1. **手动复制文件**：
   - 从 `Assets/EggRogue/StreamingAssets/gameconfig.csv` 复制
   - 到构建输出目录：`D:/Project/MiniGameExample/Builds/EggRogue/StreamingAssets/gameconfig.csv`

2. **使用 UOS Dashboard 上传**：
   - 登录 Unity Dashboard
   - 找到对应的 Bucket (`EggRogue`)
   - 手动上传 `gameconfig.csv` 到 `StreamingAssets/` 路径

3. **或者使用命令行工具上传**（如果有 UOS CLI）

---

## 详细排查步骤

### 步骤1：确认文件在 Unity 中可见

1. 在 Unity Project 窗口中，导航到 `Assets/EggRogue/StreamingAssets/`
2. 确认能看到 `gameconfig.csv` 文件
3. 右键点击文件，选择 **Reimport**（确保 Unity 识别文件）

### 步骤2：检查构建过程

1. 执行"生成并转化"
2. **在构建过程中**（构建完成前），检查输出目录：
   - `D:/Project/MiniGameExample/Builds/EggRogue/StreamingAssets/`
   - 看看 CSV 文件是否在构建过程中出现
3. 如果构建过程中有，但构建完成后消失，说明 `DeleteStreamingAssets: 1` 在起作用

### 步骤3：检查 Instant Game Build & Upload

1. 执行 Build & Upload
2. 查看 Unity Console 的输出日志
3. 查找是否有关于 StreamingAssets 或 CSV 文件的日志
4. 确认文件是否被识别和上传

### 步骤4：验证 CDN 访问

上传完成后，等待几分钟（CDN 可能有延迟），然后访问：
```
https://a.unity.cn/client_api/v1/buckets/cf30919b-69ff-4e97-a86e-372f22b9ae52/release_by_badge/latest/content/StreamingAssets/gameconfig.csv
```

---

## 推荐操作流程

### 立即测试方案

1. **临时修改配置**：
   - 将 `DeleteStreamingAssets` 改为 `0`
   - 重新执行"生成并转化"
   - 确认构建输出目录中有 CSV 文件

2. **执行 Build & Upload**：
   - 确认文件被上传
   - 验证 CDN 访问

3. **如果成功**：
   - 可以改回 `DeleteStreamingAssets: 1`（如果包体大小是问题）
   - 或者保持 `0`（如果包体大小不是问题）

### 长期方案

1. **检查 Instant Game 配置**：
   - 确认 StreamingAssets 被正确识别
   - 可能需要配置哪些文件类型需要上传

2. **自动化流程**：
   - 考虑编写脚本，在构建后自动上传 CSV
   - 或者修改构建流程，确保 CSV 被包含

---

## 快速检查清单

- [ ] CSV 文件在 `Assets/EggRogue/StreamingAssets/gameconfig.csv`
- [ ] 文件在 Unity Project 窗口中可见
- [ ] 已尝试 Reimport 文件
- [ ] 已检查 `DeleteStreamingAssets` 配置
- [ ] 构建过程中检查输出目录（构建完成前）
- [ ] 已执行 Build & Upload
- [ ] 已查看 Build & Upload 日志
- [ ] 已等待几分钟后验证 CDN 访问

---

如果以上步骤都无法解决问题，可能需要：
1. 检查微信小游戏 SDK 的版本和文档
2. 查看 Instant Game 的详细配置选项
3. 联系 Unity 技术支持
