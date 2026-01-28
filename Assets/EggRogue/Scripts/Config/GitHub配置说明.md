# GitHub 配置 CSV 文件说明

## 重要：使用 Raw URL

GitHub 提供了两种 URL：
- ❌ **Blob URL**（网页浏览）：`https://github.com/.../blob/.../gameconfig.csv`
- ✅ **Raw URL**（原始文件）：`https://raw.githubusercontent.com/.../gameconfig.csv`

**必须使用 Raw URL**，因为游戏需要直接下载文件内容，而不是 HTML 页面。

---

## 你的配置

### 当前文件位置
```
仓库：Zhy-3DEnv/MiniGameExample-2025
分支/提交：fae1267ce0b90e7adee756f618119496598fd92b
路径：Assets/EggRogue/StreamingAssets/gameconfig.csv
```

### 正确的 Raw URL

```
https://raw.githubusercontent.com/Zhy-3DEnv/MiniGameExample-2025/fae1267ce0b90e7adee756f618119496598fd92b/Assets/EggRogue/StreamingAssets/gameconfig.csv
```

### 或者使用主分支（推荐）

如果你想让文件自动更新，建议将文件放在主分支（main 或 master），然后使用：

```
https://raw.githubusercontent.com/Zhy-3DEnv/MiniGameExample-2025/main/Assets/EggRogue/StreamingAssets/gameconfig.csv
```

或者：

```
https://raw.githubusercontent.com/Zhy-3DEnv/MiniGameExample-2025/master/Assets/EggRogue/StreamingAssets/gameconfig.csv
```

**优点**：
- ✅ 更新文件后，URL 不变
- ✅ 不需要修改 Unity 配置
- ✅ 更简单易用

---

## Unity 配置步骤

### 步骤1：打开 CSVConfigManager

1. 在 Unity 中，找到 `CSVConfigManager` 对象（通常在 GameScene）
2. 在 Inspector 中查看配置

### 步骤2：配置 HTTP 服务器

设置以下参数：

- ✅ **Use Http Server**: `true`（勾选）
- **Http Server Url**: 
  ```
  https://raw.githubusercontent.com/Zhy-3DEnv/MiniGameExample-2025/fae1267ce0b90e7adee756f618119496598fd92b/Assets/EggRogue/StreamingAssets/gameconfig.csv
  ```

  或者使用主分支（推荐）：
  ```
  https://raw.githubusercontent.com/Zhy-3DEnv/MiniGameExample-2025/main/Assets/EggRogue/StreamingAssets/gameconfig.csv
  ```

### 步骤3：保存场景

按 `Ctrl+S` 保存场景

---

## 测试步骤

### 步骤1：运行游戏

1. 点击 Play 按钮
2. 查看 Unity Console

### 步骤2：检查日志

应该看到：
```
CSVConfigManager: 从 HTTP 服务器加载 CSV: https://raw.githubusercontent.com/...
CSVConfigManager: 从 HTTP 服务器加载成功，已加载 7 个配置项
```

### 步骤3：测试更新配置

1. 修改 GitHub 上的 CSV 文件
2. 提交更改
3. 在游戏中点击"更新配置"按钮
4. 查看 Console，应该看到配置已更新

---

## 如何获取 Raw URL

### 方法1：在 GitHub 网页上

1. 打开文件页面（blob URL）
2. 点击右上角的 **Raw** 按钮
3. 复制浏览器地址栏中的 URL

### 方法2：手动转换

将 blob URL：
```
https://github.com/user/repo/blob/branch/path/file.csv
```

转换为 raw URL：
```
https://raw.githubusercontent.com/user/repo/branch/path/file.csv
```

**规则**：
- `github.com` → `raw.githubusercontent.com`
- 删除 `/blob/`
- 其他部分保持不变

---

## 推荐：使用主分支

### 为什么使用主分支？

1. **URL 稳定**：不需要每次更新都改 URL
2. **自动更新**：修改文件后，游戏可以立即获取最新版本
3. **更简单**：不需要记住提交哈希

### 如何设置

1. 确保 CSV 文件在 `main` 或 `master` 分支
2. 使用主分支的 raw URL：
   ```
   https://raw.githubusercontent.com/Zhy-3DEnv/MiniGameExample-2025/main/Assets/EggRogue/StreamingAssets/gameconfig.csv
   ```
3. 在 Unity 中配置这个 URL

### 更新流程

```
修改 CSV 文件
  ↓
提交到 GitHub（main 分支）
  ↓
游戏点击"更新配置"按钮
  ↓
自动获取最新配置 ✅
```

---

## 常见问题

### Q: 为什么使用 Raw URL？

A: Blob URL 返回的是 HTML 页面，Raw URL 返回的是纯文本文件内容。游戏需要直接读取文件内容。

### Q: 如何确保文件更新后立即生效？

A: 
1. 使用主分支的 URL（而不是提交哈希）
2. 或者每次更新后修改 Unity 中的 URL

### Q: 如果 GitHub 访问慢怎么办？

A: 
1. 可以使用 GitHub 的 CDN（自动）
2. 或者使用国内镜像（如果有）
3. 或者使用其他云存储服务（阿里云 OSS、腾讯云 COS）

### Q: 如何验证 URL 是否正确？

A: 在浏览器中直接访问 URL，应该看到 CSV 文件内容，而不是 HTML 页面。

---

## 验证 URL

在浏览器中访问以下 URL，应该能看到 CSV 内容：

```
https://raw.githubusercontent.com/Zhy-3DEnv/MiniGameExample-2025/fae1267ce0b90e7adee756f618119496598fd92b/Assets/EggRogue/StreamingAssets/gameconfig.csv
```

如果看到：
```
Key,Value,Type,Desc
PlayerDamage,10,float,玩家单发伤害
...
```

说明 URL 正确 ✅

如果看到 HTML 页面，说明 URL 错误 ❌

---

完成以上配置后，你的游戏就可以从 GitHub 加载配置了！
