# HTTP 服务器配置方案（推荐）

## 为什么使用 HTTP 服务器？

### 当前方案的问题

1. **流程复杂**：
   - 修改 CSV → 重新构建 → Build & Upload → 等待 CDN 更新
   - 每次更新都需要重新打包，不适合频繁调整

2. **依赖 CDN**：
   - 需要配置 Instant Game、UOS、CDN
   - 上传流程可能失败
   - CDN 缓存可能导致更新延迟

3. **不够灵活**：
   - 无法支持 A/B 测试
   - 无法支持版本控制
   - 无法支持动态配置

### HTTP 服务器方案的优势

1. **简单直接**：
   - 修改 CSV → 上传到服务器 → 游戏自动下载
   - 不需要重新构建游戏

2. **实时更新**：
   - 修改后立即生效
   - 支持运行时更新（点击"更新配置"按钮）

3. **灵活强大**：
   - 支持版本控制（不同版本的游戏读取不同配置）
   - 支持 A/B 测试
   - 支持按地区/用户分发不同配置
   - 可以添加认证、加密等安全措施

---

## 实现方案

### 代码已更新

`CSVConfigManager` 已支持从 HTTP 服务器加载配置：

1. **新增配置项**：
   - `Use Http Server`: 是否启用 HTTP 服务器
   - `Http Server Url`: HTTP 服务器 URL

2. **优先级**：
   - HTTP 服务器（如果启用）
   - StreamingAssets/CDN（备用）
   - 本地文件（备用）
   - 默认配置（最后）

3. **自动降级**：
   - 如果 HTTP 服务器失败，自动尝试备用方案
   - 确保游戏始终能正常运行

---

## 使用步骤

### 步骤1：设置 HTTP 服务器

#### 方案A：使用简单的静态文件服务器

1. **使用 GitHub Pages**（免费）：
   - 创建 GitHub 仓库
   - 上传 `gameconfig.csv` 文件
   - 启用 GitHub Pages
   - 获取 URL：`https://your-username.github.io/repo-name/gameconfig.csv`

2. **使用 Netlify/Vercel**（免费）：
   - 上传 CSV 文件
   - 获取 CDN URL

3. **使用自己的服务器**：
   - 将 CSV 文件放在 Web 服务器上
   - 确保支持 CORS（跨域请求）

#### 方案B：使用云存储服务

1. **阿里云 OSS / 腾讯云 COS / AWS S3**：
   - 上传 CSV 文件
   - 设置为公开读取
   - 获取文件 URL

2. **优点**：
   - 稳定可靠
   - 支持 CDN 加速
   - 可以设置访问权限

### 步骤2：配置 CSVConfigManager

1. 在 Unity 中，找到 `CSVConfigManager` 对象
2. 设置：
   - ✅ **Use Http Server**: `true`
   - **Http Server Url**: `https://your-server.com/gameconfig.csv`
3. 保存场景

### 步骤3：测试

1. 运行游戏
2. 查看 Console，应该看到：
   ```
   CSVConfigManager: 从 HTTP 服务器加载 CSV: https://...
   CSVConfigManager: 从 HTTP 服务器加载成功，已加载 X 个配置项
   ```

### 步骤4：更新配置

1. 修改 CSV 文件
2. 上传到服务器（覆盖旧文件）
3. 在游戏中点击"更新配置"按钮
4. 配置立即生效 ✅

---

## 推荐服务器方案

### 最简单：GitHub Pages（免费）

1. **创建仓库**：
   ```
   https://github.com/your-username/game-config
   ```

2. **上传 CSV 文件**：
   - 将 `gameconfig.csv` 上传到仓库根目录

3. **启用 GitHub Pages**：
   - Settings → Pages
   - Source: main branch
   - 保存

4. **获取 URL**：
   ```
   https://your-username.github.io/game-config/gameconfig.csv
   ```

5. **配置 CSVConfigManager**：
   - Http Server Url: `https://your-username.github.io/game-config/gameconfig.csv`

**优点**：
- ✅ 完全免费
- ✅ 版本控制（Git）
- ✅ 支持 HTTPS
- ✅ 全球 CDN

**缺点**：
- ⚠️ 更新需要 Git 操作（但可以自动化）

### 更专业：云存储服务

1. **阿里云 OSS**：
   - 创建 Bucket
   - 上传 CSV 文件
   - 设置为公开读取
   - 获取 URL：`https://your-bucket.oss-cn-hangzhou.aliyuncs.com/gameconfig.csv`

2. **腾讯云 COS**：
   - 类似操作
   - 获取 URL：`https://your-bucket.cos.ap-shanghai.myqcloud.com/gameconfig.csv`

**优点**：
- ✅ 稳定可靠
- ✅ 支持 CDN 加速
- ✅ 可以设置访问权限
- ✅ 支持版本管理

---

## 安全考虑

### 1. HTTPS 必须

- 确保服务器支持 HTTPS
- 避免配置被中间人攻击

### 2. 验证配置

- 在 `CSVConfigManager` 中可以添加配置验证
- 检查配置格式、值范围等

### 3. 访问控制（可选）

如果需要限制访问，可以：
- 使用 Token 认证
- 使用签名验证
- 使用 IP 白名单

---

## 工作流程对比

### 旧方案（CDN）

```
修改 CSV
  ↓
重新构建游戏
  ↓
Build & Upload
  ↓
等待 CDN 更新（可能需要几分钟）
  ↓
游戏重新加载配置
```

**耗时**：10-30 分钟

### 新方案（HTTP 服务器）

```
修改 CSV
  ↓
上传到服务器（几秒钟）
  ↓
游戏点击"更新配置"按钮
  ↓
配置立即生效
```

**耗时**：几秒钟

---

## 配置示例

### Unity Inspector 设置

```
CSVConfigManager
├─ CSV File Name: gameconfig.csv
├─ Use Http Server: ✅ true
├─ Http Server Url: https://your-server.com/gameconfig.csv
├─ Allow Local File Read: false
└─ Local File Path: (留空)
```

### CSV 文件 URL 示例

```
# GitHub Pages
https://username.github.io/repo/gameconfig.csv

# 阿里云 OSS
https://bucket.oss-cn-hangzhou.aliyuncs.com/gameconfig.csv

# 腾讯云 COS
https://bucket.cos.ap-shanghai.myqcloud.com/gameconfig.csv

# 自己的服务器
https://api.yourgame.com/config/gameconfig.csv
```

---

## 优势总结

1. ✅ **简单**：不需要复杂的构建流程
2. ✅ **快速**：修改后几秒钟生效
3. ✅ **灵活**：支持版本控制、A/B 测试
4. ✅ **可靠**：自动降级到备用方案
5. ✅ **跨平台**：所有平台都支持 HTTP 请求

---

## 迁移建议

1. **保留现有功能**：
   - StreamingAssets 作为备用方案
   - 如果 HTTP 失败，自动使用本地文件

2. **逐步迁移**：
   - 先测试 HTTP 方案
   - 确认稳定后，可以禁用 StreamingAssets

3. **生产环境**：
   - 使用稳定的云存储服务
   - 配置监控和告警
   - 定期备份配置

---

完成以上设置后，你就可以用最简单的方式管理游戏配置了！
