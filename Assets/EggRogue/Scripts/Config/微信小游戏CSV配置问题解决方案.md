# 微信小游戏 CSV 配置 404 问题解决方案

## 问题描述

在微信小游戏环境中，`CSVConfigManager` 尝试从 CDN 加载 CSV 文件时返回 404 错误：

```
GET https://a.unity.cn/client_api/v1/buckets/.../StreamingAssets/gameconfig.csv 404
```

## 问题原因

1. **微信小游戏配置了 CDN**：`Application.streamingAssetsPath` 指向 CDN URL
2. **CSV 文件未上传到 CDN**：构建时 CSV 文件被打包到本地，但没有上传到 CDN
3. **UnityWebRequest 尝试从 CDN 读取**：代码使用 `UnityWebRequest.Get(streamingPath)` 时，路径指向 CDN

## 解决方案

### 方案1：上传 CSV 到 CDN（推荐用于生产环境）

1. **确认 CSV 文件已打包**：
   - 检查构建输出目录：`D:/Project/MiniGameExample/Builds/EggRogue/StreamingAssets/`
   - 确认 `gameconfig.csv` 文件存在

2. **上传到 CDN**：
   - 将 `StreamingAssets/gameconfig.csv` 上传到配置的 CDN
   - CDN 路径：`https://a.unity.cn/client_api/v1/buckets/cf30919b-69ff-4e97-a86e-372f22b9ae52/release_by_badge/latest/content/StreamingAssets/gameconfig.csv`

3. **验证**：
   - 在浏览器中访问 CDN URL，确认文件可以下载
   - 重新运行游戏，应该能正常加载

### 方案2：使用本地文件（推荐用于开发测试）

代码已优化，支持多种读取方式（按优先级）：

1. **CDN/StreamingAssets**（如果文件已上传）
2. **本地构建目录**（如果文件在本地）
3. **persistentDataPath**（用户手动放置的文件）

**使用方法**：

1. **将 CSV 复制到 persistentDataPath**：
   - 在微信开发者工具的 Console 中查看 `Application.persistentDataPath` 路径
   - 手动将 `gameconfig.csv` 复制到该路径
   - 游戏会自动读取

2. **或者修改代码使用本地路径**：
   - 在 `CSVConfigManager` 中设置 `Allow Local File Read = true`
   - 设置 `Local File Path` 为本地文件路径（仅编辑器有效）

### 方案3：修改微信小游戏配置（不推荐）

如果不想使用 CDN，可以修改 `MiniGameConfig.asset`：

1. 将 `CDN` 字段清空
2. 将 `StreamCDN` 字段清空
3. 重新打包

**注意**：这会影响其他 StreamingAssets 资源的加载方式。

---

## 代码优化说明

`CSVConfigManager` 已优化，支持多种读取方式：

### WebGL 环境下的读取顺序

1. **CDN/StreamingAssets**：尝试从 `Application.streamingAssetsPath` 读取（可能是 CDN）
2. **本地文件**：如果 CDN 失败，尝试从本地文件系统读取
3. **persistentDataPath**：尝试从用户数据目录读取
4. **默认配置**：如果都失败，使用代码中的默认值

### 调试信息

代码会输出详细的日志，帮助诊断问题：

```
CSVConfigManager: WebGL 异步加载 CSV，尝试路径: xxx
CSVConfigManager: Application.streamingAssetsPath = xxx
CSVConfigManager: 从 StreamingAssets/CDN 加载失败: 404，尝试其他路径
CSVConfigManager: 从 persistentDataPath 读取成功，已加载 X 个配置项
```

---

## 快速测试步骤

### 测试1：使用 persistentDataPath（最简单）

1. 运行游戏，查看 Console 日志
2. 找到 `Application.persistentDataPath` 的值
3. 将 `gameconfig.csv` 复制到该路径
4. 点击"更新配置"按钮
5. 应该能看到"从 persistentDataPath 读取成功"

### 测试2：上传到 CDN

1. 确认构建输出目录有 `StreamingAssets/gameconfig.csv`
2. 上传到 CDN（根据你的 CDN 配置）
3. 在浏览器中验证 URL 可访问
4. 重新运行游戏，应该能从 CDN 加载

---

## 常见问题

### Q: 为什么 CSV 文件没有自动上传到 CDN？

A: 微信小游戏的构建工具可能不会自动上传 StreamingAssets 中的所有文件到 CDN。需要手动上传或配置自动上传流程。

### Q: 我可以将 CSV 放在 Resources 文件夹吗？

A: 可以，但 Resources 文件夹会增加包体大小，且无法在运行时更新。推荐使用 StreamingAssets + persistentDataPath 的组合。

### Q: 如何实现运行时更新配置？

A: 
1. 将新 CSV 上传到服务器/CDN
2. 使用 `UnityWebRequest` 下载新 CSV
3. 保存到 `persistentDataPath`
4. 调用 `UpdateConfig()` 重新加载

---

## 推荐工作流程

### 开发阶段

1. 使用编辑器直接修改 `Assets/EggRogue/StreamingAssets/gameconfig.csv`
2. 在编辑器中测试，点击"更新配置"按钮

### 测试阶段

1. 打包后，将 CSV 复制到 `persistentDataPath`
2. 在微信开发者工具中测试

### 生产环境

1. 将 CSV 上传到 CDN
2. 游戏自动从 CDN 加载
3. 需要更新时，上传新 CSV 到 CDN，用户点击"更新配置"即可

---

完成以上步骤后，CSV 配置系统应该能在微信小游戏中正常工作了！
