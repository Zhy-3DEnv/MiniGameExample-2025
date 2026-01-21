# Cursor 对话历史同步指南

## 📋 概述

Cursor 的对话历史存储在 `.specstory/history/` 目录下，以 Markdown 格式保存。通过 Git 同步，您可以在公司电脑和家里电脑之间同步所有对话记录。

## ✅ 方案一：使用 Git 同步（推荐）

### 设置步骤

#### 1. 确保对话历史文件被 Git 跟踪

当前项目中的对话历史文件大部分已经被 Git 跟踪。如果发现新的对话文件未被跟踪，执行：

```bash
# 添加所有对话历史文件
git add .specstory/history/*.md

# 提交
git commit -m "添加 Cursor 对话历史"

# 推送到远程仓库
git push
```

#### 2. 在公司电脑上同步

**每次对话结束后：**
```bash
# 检查是否有新的对话文件
git status .specstory/history/

# 如果有新文件，添加到 Git
git add .specstory/history/*.md

# 提交并推送
git commit -m "更新对话历史"
git push
```

**或者使用自动化脚本（见下方）**

#### 3. 在家里电脑上同步

**开始工作前：**
```bash
# 拉取最新的对话历史
git pull
```

### 自动化同步脚本

您可以创建一个简单的脚本来自动同步对话历史：

**Windows (PowerShell) - `sync-chat.ps1`:**
```powershell
# 同步 Cursor 对话历史
git add .specstory/history/*.md
git commit -m "自动同步对话历史 $(Get-Date -Format 'yyyy-MM-dd HH:mm')" 2>$null
git push
```

**使用方式：**
- 在公司电脑：每次对话结束后运行 `.\sync-chat.ps1`
- 在家里电脑：开始工作前运行 `git pull`

## 🔄 方案二：使用云存储同步

如果您的项目不在 Git 仓库中，可以使用云存储服务：

### OneDrive / Dropbox / Google Drive

1. 将 `.specstory` 目录添加到云存储同步文件夹
2. 在两台电脑上安装相同的云存储客户端
3. 对话历史会自动同步

**注意：** 确保两台电脑的云存储客户端都设置为同步 `.specstory` 目录。

## 📝 方案三：手动同步

如果以上方案都不适用，可以手动复制：

1. 在公司电脑：复制 `.specstory/history/` 目录下的所有 `.md` 文件
2. 通过 U盘/网盘/邮件等方式传输
3. 在家里电脑：将文件粘贴到相同位置

## ⚠️ 注意事项

1. **文件冲突：** 如果两台电脑同时创建了对话文件，Git 可能会提示冲突，需要手动解决
2. **隐私考虑：** 对话历史可能包含敏感信息，确保 Git 仓库是私有的
3. **定期同步：** 建议每次对话后立即同步，避免丢失记录

## 🔍 检查同步状态

```bash
# 查看哪些对话文件已被跟踪
git ls-files .specstory/history/

# 查看哪些对话文件未被跟踪
git status .specstory/history/
```

## 💡 最佳实践

1. **定期提交：** 建议每次重要对话后立即提交
2. **使用分支：** 如果担心影响主分支，可以创建专门的 `chat-history` 分支
3. **备份：** 定期备份 `.specstory` 目录到其他位置

---

**提示：** 当前项目中的对话历史文件大部分已经被 Git 跟踪，您只需要：
- 在公司电脑：`git add .specstory/history/*.md && git commit -m "更新对话" && git push`
- 在家里电脑：`git pull`
