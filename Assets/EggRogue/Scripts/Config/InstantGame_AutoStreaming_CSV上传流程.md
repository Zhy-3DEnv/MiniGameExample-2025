# Instant Game AutoStreaming CSV 上传流程

## 一、流程确认

你的理解是**正确的**！正确的流程是：

1. **第一步：微信小游戏转换工具 - 生成并转化**
   - Unity 菜单：**微信小游戏 > 转换小游戏**
   - 这会生成 WebGL 构建和微信小游戏所需的文件
   - 输出目录：`D:/Project/MiniGameExample/Builds/EggRogue/`

2. **第二步：Instant Game - Build & Upload**
   - Unity 菜单：**Services > Instant Game > Build & Upload**
   - 这会：
     - 分析 StreamingAssets 中的资源
     - 上传到 UOS (Unity Online Services) CDN
     - 生成资源清单和版本信息

---

## 二、关键配置检查

### 1. AutoStreaming 配置

你的项目已配置：
- **Bucket UUID**: `cf30919b-69ff-4e97-a86e-372f22b9ae52`
- **Bucket Name**: `EggRogue`
- **Badge Name**: `latest`
- **CDN URL**: `https://a.unity.cn/client_api/v1/buckets/cf30919b-69ff-4e97-a86e-372f22b9ae52/release_by_badge/latest/content`

### 2. 微信小游戏配置

`MiniGameConfig.asset` 显示：
- **CDN**: 已配置为 UOS CDN
- **bundlePathIdentifier**: `StreamingAssets;AS;CUS/CustomAB`
  - 这意味着 `StreamingAssets` 文件夹会被识别并上传
- **DeleteStreamingAssets**: `1` ⚠️
  - **重要**：这意味着构建后，本地的 StreamingAssets 会被删除
  - 游戏运行时只能从 CDN 加载 StreamingAssets

---

## 三、确保 CSV 文件被上传

### 步骤1：确认 CSV 文件在 StreamingAssets

1. 检查文件位置：`Assets/EggRogue/StreamingAssets/gameconfig.csv`
2. 确认文件在 Unity Project 窗口中可见
3. 确认文件已保存（不是临时文件）

### 步骤2：执行"生成并转化"

1. Unity 菜单：**微信小游戏 > 转换小游戏**
2. 等待构建完成
3. **检查构建输出目录**：
   - 路径：`D:/Project/MiniGameExample/Builds/EggRogue/StreamingAssets/`
   - 确认 `gameconfig.csv` 文件存在
   - ⚠️ 注意：如果 `DeleteStreamingAssets: 1`，构建完成后文件可能被删除

### 步骤3：执行 Build & Upload

1. Unity 菜单：**Services > Instant Game > Build & Upload**
2. 在 Build & Upload 窗口中：
   - 确认 **Bucket** 选择正确（`EggRogue`）
   - 确认 **Badge** 选择正确（`latest`）
   - 点击 **Build & Upload**
3. 等待上传完成

### 步骤4：验证上传

1. **在浏览器中访问**：
   ```
   https://a.unity.cn/client_api/v1/buckets/cf30919b-69ff-4e97-a86e-372f22b9ae52/release_by_badge/latest/content/StreamingAssets/gameconfig.csv
   ```
2. 如果能看到 CSV 文件内容，说明上传成功
3. 如果返回 404，说明文件未上传

---

## 四、常见问题排查

### Q1: CSV 文件没有被上传到 CDN？

**可能原因**：
1. **文件不在 StreamingAssets 目录**
   - 解决：确保文件在 `Assets/EggRogue/StreamingAssets/gameconfig.csv`

2. **文件被 .gitignore 忽略**
   - 解决：检查 `.gitignore`，确保 CSV 文件不被忽略

3. **AutoStreaming 配置问题**
   - 解决：检查 `AutoStreamingSettings.asset` 中的配置

4. **Build & Upload 时文件被排除**
   - 解决：检查 Instant Game 的 Build & Upload 窗口，确认 StreamingAssets 被包含

### Q2: 为什么 DeleteStreamingAssets 是 1？

**原因**：
- 微信小游戏配置中 `DeleteStreamingAssets: 1` 表示构建后删除本地 StreamingAssets
- 这样可以减小包体大小，所有 StreamingAssets 资源都从 CDN 加载

**影响**：
- 游戏运行时，`Application.streamingAssetsPath` 指向 CDN
- 如果 CSV 没有上传到 CDN，会出现 404 错误

**解决方案**：
- 确保执行了 **Build & Upload** 步骤
- 或者将 `DeleteStreamingAssets` 改为 `0`（不推荐，会增加包体）

### Q3: 如何确认 CSV 是否在 CDN 上？

**方法1：浏览器访问**
```
https://a.unity.cn/client_api/v1/buckets/cf30919b-69ff-4e97-a86e-372f22b9ae52/release_by_badge/latest/content/StreamingAssets/gameconfig.csv
```

**方法2：查看 Build & Upload 日志**
- 在 Unity Console 中查看 Build & Upload 的输出
- 应该能看到上传的文件列表

**方法3：在游戏中查看日志**
- 运行游戏，查看 Console
- 应该看到：`CSVConfigManager: 从 StreamingAssets/CDN 加载成功`

---

## 五、推荐工作流程

### 开发阶段

1. 修改 `Assets/EggRogue/StreamingAssets/gameconfig.csv`
2. 在编辑器中测试（直接读取本地文件）

### 测试/发布阶段

1. **生成并转化**：
   - Unity 菜单：**微信小游戏 > 转换小游戏**
   - 等待构建完成

2. **Build & Upload**：
   - Unity 菜单：**Services > Instant Game > Build & Upload**
   - 确认配置正确（Bucket、Badge）
   - 点击 **Build & Upload**
   - 等待上传完成

3. **验证**：
   - 在浏览器中访问 CDN URL，确认文件可访问
   - 在微信开发者工具中运行游戏
   - 查看 Console，确认 CSV 加载成功

4. **测试更新配置**：
   - 修改 CSV 文件
   - 重新执行步骤 1-2
   - 在游戏中点击"更新配置"按钮
   - 确认新配置生效

---

## 六、注意事项

1. **每次修改 CSV 后**：
   - 需要重新执行"生成并转化"
   - 需要重新执行"Build & Upload"
   - 否则 CDN 上的文件不会更新

2. **版本管理**：
   - 如果使用不同的 Badge（如 `v1.0`, `v1.1`），需要确保 CDN URL 中的 Badge 名称正确

3. **缓存问题**：
   - CDN 可能有缓存，上传后可能需要等待几分钟才能访问
   - 或者使用不同的 Badge 名称来避免缓存

4. **文件大小**：
   - CSV 文件很小，上传应该很快
   - 如果上传失败，检查网络连接和 UOS 配置

---

## 七、快速检查清单

- [ ] CSV 文件在 `Assets/EggRogue/StreamingAssets/gameconfig.csv`
- [ ] 已执行"生成并转化"
- [ ] 构建输出目录中有 `StreamingAssets/gameconfig.csv`（构建时）
- [ ] 已执行"Build & Upload"
- [ ] 浏览器可以访问 CDN URL
- [ ] 游戏中 Console 显示"从 StreamingAssets/CDN 加载成功"

---

完成以上步骤后，CSV 文件应该能正常从 CDN 加载了！
