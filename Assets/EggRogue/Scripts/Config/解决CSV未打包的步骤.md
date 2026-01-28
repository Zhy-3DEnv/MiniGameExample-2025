# 解决 CSV 未打包问题的步骤

## 问题分析

1. ✅ CSV 文件存在：`Assets/EggRogue/StreamingAssets/gameconfig.csv`
2. ❌ 构建输出目录没有文件（因为 `DeleteStreamingAssets: 1`）
3. ❌ CDN 返回 404（文件未上传）

## 解决方案

### 步骤1：临时禁用 DeleteStreamingAssets

我已经帮你修改了配置：
- 将 `DeleteStreamingAssets: 1` 改为 `DeleteStreamingAssets: 0`
- 这样构建后 StreamingAssets 不会被删除

### 步骤2：重新执行"生成并转化"

1. Unity 菜单：**微信小游戏 > 转换小游戏**
2. 等待构建完成
3. **立即检查构建输出目录**（在构建完成后立即检查）：
   ```
   D:/Project/MiniGameExample/Builds/EggRogue/StreamingAssets/gameconfig.csv
   ```
4. 确认文件存在

### 步骤3：执行 Build & Upload

1. Unity 菜单：**Services > Instant Game > Build & Upload**
2. 在窗口中：
   - 确认 **Bucket**: `EggRogue`
   - 确认 **Badge**: `latest`
   - 点击 **Build & Upload**
3. **查看 Unity Console 日志**：
   - 查找是否有关于 `gameconfig.csv` 或 `StreamingAssets` 的日志
   - 确认文件是否被识别和上传

### 步骤4：验证上传

1. **等待 1-2 分钟**（CDN 可能有延迟）
2. **在浏览器中访问**：
   ```
   https://a.unity.cn/client_api/v1/buckets/cf30919b-69ff-4e97-a86e-372f22b9ae52/release_by_badge/latest/content/StreamingAssets/gameconfig.csv
   ```
3. 如果能看到 CSV 内容，说明上传成功 ✅
4. 如果还是 404，继续下一步

### 步骤5：如果 Build & Upload 没有上传 CSV

如果 Instant Game 的 Build & Upload 没有自动上传 CSV 文件，可以：

#### 方案A：手动复制文件到构建目录

1. 从 `Assets/EggRogue/StreamingAssets/gameconfig.csv` 复制
2. 到 `D:/Project/MiniGameExample/Builds/EggRogue/StreamingAssets/gameconfig.csv`
3. 确保目录存在（如果不存在，先创建 `StreamingAssets` 文件夹）
4. 重新执行 Build & Upload

#### 方案B：使用 Unity Dashboard 手动上传

1. 登录 Unity Dashboard
2. 导航到 **Cloud Content Delivery (CCD)**
3. 找到 Bucket: `EggRogue`
4. 找到 Badge: `latest`
5. 手动上传 `gameconfig.csv` 到 `StreamingAssets/` 路径

---

## 验证步骤

### 验证1：检查构建输出

```bash
# 在文件管理器中检查
D:/Project/MiniGameExample/Builds/EggRogue/StreamingAssets/gameconfig.csv
```

### 验证2：检查 CDN

在浏览器中访问：
```
https://a.unity.cn/client_api/v1/buckets/cf30919b-69ff-4e97-a86e-372f22b9ae52/release_by_badge/latest/content/StreamingAssets/gameconfig.csv
```

应该看到：
```
Key,Value,Type,Desc
PlayerDamage,10,float,玩家单发伤害
...
```

### 验证3：在游戏中测试

1. 在微信开发者工具中运行游戏
2. 查看 Console，应该看到：
   ```
   CSVConfigManager: 从 StreamingAssets/CDN 加载成功，已加载 X 个配置项
   ```

---

## 如果还是不行

### 检查 Instant Game Build & Upload 配置

1. Unity 菜单：**Services > Instant Game > Build & Upload**
2. 检查窗口中的配置选项
3. 查看是否有"包含 StreamingAssets"或类似的选项
4. 确认 StreamingAssets 被包含在上传列表中

### 检查构建日志

1. 查看 Unity Console 中的构建日志
2. 查找关于 StreamingAssets 的信息
3. 查找错误或警告信息

### 临时解决方案

如果 Instant Game Build & Upload 无法上传 CSV，可以：

1. **保持 `DeleteStreamingAssets: 0`**
2. **手动将 CSV 复制到构建输出目录**
3. **在微信开发者工具中测试**（使用本地文件）

---

## 关于 DeleteStreamingAssets 配置

### 当前设置：`DeleteStreamingAssets: 0`

**优点**：
- ✅ 确保 StreamingAssets 文件在构建输出目录中
- ✅ 方便调试和测试
- ✅ 确保 Instant Game Build & Upload 能找到文件

**缺点**：
- ⚠️ 会增加包体大小（CSV 文件很小，影响不大）

### 如果改为 `DeleteStreamingAssets: 1`

**优点**：
- ✅ 减小包体大小

**缺点**：
- ❌ 构建后文件被删除，Instant Game Build & Upload 可能找不到文件
- ❌ 需要确保文件已上传到 CDN 才能正常运行

**建议**：
- 开发/测试阶段：使用 `0`
- 生产环境：如果 CSV 已成功上传到 CDN，可以改为 `1`

---

完成以上步骤后，CSV 文件应该能正常工作了！
