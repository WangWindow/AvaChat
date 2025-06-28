---
mode: 'agent'
tools: ['changes', 'codebase', 'editFiles', 'fetch', 'githubRepo', 'new', 'runCommands', 'search', 'searchResults', 'terminalLastCommand', 'terminalSelection', 'usages']
description: '基于 Avalonia UI 的跨平台实时聊天系统 - 客户端/服务器架构'
---

# AvaChat - 实时聊天系统

## 技术栈
- **UI 框架:** Avalonia UI
- **MVVM:** CommunityToolkit.Mvvm
- **运行时:** .NET 9
- **数据库:** SQLite + Entity Framework Core
- **依赖注入:** Microsoft.Extensions.DependencyInjection

## 全局引用配置

### GlobalUsings.cs 管理
- 在每个项目的根目录下创建 `GlobalUsings.cs` 文件
- 统一管理常用命名空间的全局引用
- 减少重复的 `using` 语句，提高代码整洁度

## 客户端功能 (AvaChat.Client)

### 🔐 登录模块
- 用户号码和密码登录
- 建立与服务器的 TCP/WebSocket 连接
- 显示连接状态和登录错误信息
- 记住登录信息，支持历史账号快速选择
- 显示登录时间和在线时长

### 🎨 界面设计 (类QQ布局) - 组件化架构
- **主窗口 (MainWindow):** 三栏布局容器窗口
- **组件化设计:**
  - **NavigationView (UserControl):** 左侧导航栏组件
    - 聊天、好友、设置模块切换按钮
    - 新消息红点提醒指示器
    - 用户头像和在线状态显示
  - **FriendListView (UserControl):** 中间好友列表组件
    - 顶部搜索框支持昵称/用户号搜索
    - 好友项显示：头像、昵称、个性签名、在线状态
    - 右键上下文菜单：发送消息、查看资料、删除好友
    - 分组折叠/展开功能
  - **ChatView (UserControl):** 右侧聊天窗口组件
    - 聊天头部信息栏 (ChatHeaderView)
    - 消息列表区域 (MessageListView)
    - 消息输入区域 (MessageInputView)
  - **SettingsView (UserControl):** 设置页面组件
    - 个人信息设置
    - 应用程序设置选项
    - 主题切换和通知设置
- **独立窗口组件:**
  - **LoginWindow:** 登录窗口
  - **ProfileWindow:** 个人资料编辑窗口
  - **SettingsWindow:** 详细设置窗口 (可选独立窗口)
- **聊天功能组件:**
  - 消息气泡样式显示
  - 底部输入框 + 表情选择器 + 文件发送 + 发送按钮
  - 支持 Enter 换行，Ctrl+Enter 发送
  - 消息状态指示（发送中/已读/未读）

### 💬 聊天功能
- 支持文本消息和表情符号
- 消息发送状态指示（发送中/已送达）
- 本地 SQLite 存储聊天记录
- 按日期、关键词搜索历史消息
- 聊天记录导出功能

### 🔔 系统托盘和通知
- 使用 Avalonia 自带的 `TrayIcon` 控件，在 `App.axaml` 中通过 `<TrayIcon.Icons>` 定义
- 最小化到系统托盘，支持双击恢复窗口
- 托盘右键菜单：显示主窗口、设置选项、退出程序
- 支持嵌套菜单结构（设置子菜单包含个人资料、消息设置、主题切换等）
- 新消息时托盘图标闪烁和通知气泡
- Windows 原生通知推送
- 可自定义提示音效
- 支持勿扰模式设置

### 👤 个人信息管理
- 头像上传和预览
- 昵称、个性签名编辑
- 在线状态设置（在线、离开、隐身）
- 个人资料卡片展示

## 服务器端功能 (AvaChat.Server)

### 🖥️ 服务管理控制台
- Avalonia UI 管理界面
- 实时显示服务器运行状态
- 在线用户列表和连接统计
- 服务器日志查看和导出
- 向所有在线用户发送系统公告

###  用户管理
- **用户注册:** 自动生成 8 位唯一用户号码，保存用户信息
- **登录验证:** 检查登录信息，返回验证结果
- **在线状态管理:**
  - 实时更新用户在线状态
  - 向其他用户广播状态变更
  - 优雅的用户下线处理

### � 连接和消息管理
- **客户端连接处理:**
  - TCP/WebSocket 多协议支持
  - 连接池管理和异常连接清理
  - 心跳检测和重连机制
- **消息转发:**
  - 高效的消息路由机制
  - 支持单发和群发
  - 消息持久化存储

### 📨 核心业务流程
1. **用户上线:** 验证登录信息 → 发送在线好友列表 → 广播用户上线状态
2. **消息转发:** 接收客户端消息 → 转发给指定用户 → 确认送达状态
3. **用户下线:** 检测用户断开 → 广播下线消息 → 更新服务器状态显示

## 数据库设计规范

### 设计原则
- **规范化设计:** 所有表结构达到第三范式 (3NF)，消除数据冗余
- **性能优化:** 合理设计索引策略，支持高并发访问
- **数据完整性:** 完善的约束规则确保数据一致性
- **安全性控制:** 用户权限管理和数据访问控制
- **文件位置:** SQLite 数据库文件存放在可执行文件同目录下，便于部署和管理

### 数据库文件管理
- **服务器数据库:** `AvaChat.Server.db` - 存储所有用户账户、聊天记录、好友关系、在线状态、系统日志
- **客户端缓存:** `AvaChat.Client.db` - 仅存储本地缓存数据（聊天记录缓存、用户设置、离线消息等）
- **连接字符串:**
  - 服务器：`Data Source=./AvaChat.Server.db`
  - 客户端：`Data Source=./AvaChat.Client.db`
- **数据同步策略:**
  - 客户端本地缓存近期聊天记录以提升性能
  - 历史消息从服务器按需拉取
  - 用户上线时同步最新数据
  - 客户端离线时暂存消息，上线后同步到服务器
- **自动创建:** 首次运行时自动创建数据库文件和表结构



## UI组件设计规范

### 主题色彩
- **主色调:** SystemAccentColor (系统主题色)
- **浅色主题:**
  - 背景: SystemControlBackgroundAltHighBrush
  - 文字: SystemControlForegroundBaseHighBrush
  - 卡片: SystemControlBackgroundChromeMediumLowBrush
- **深色主题:**
  - 背景: SystemControlBackgroundAltMediumBrush
  - 文字: SystemControlForegroundAltHighBrush
  - 卡片: SystemControlBackgroundChromeMediumBrush
- **状态颜色:**
  - 在线: SystemControlHighlightAccentBrush
  - 离线: SystemControlDisabledBaseMediumLowBrush
  - 错误: SystemControlErrorTextForegroundBrush

### 组件规范
- **头像:** 圆形，统一大小 40px
- **状态指示:** 在线(绿色)、忙碌(红色)、离开(黄色)、离线(灰色)
- **消息气泡:** 发送(蓝色右对齐)、接收(白色左对齐)
- **圆角:** 统一 4px，卡片 8px

## 开发规范

### MVVM 架构
- 使用 `CommunityToolkit.Mvvm` 的 `ObservableObject` 和 `RelayCommand`
- 严格的视图-视图模型分离
- 通过依赖注入管理服务生命周期

### TrayIcon 实现
- 在 `App.axaml` 文件中使用 `<TrayIcon.Icons>` 定义托盘图标
- 通过 `TrayIcon.ToolTipText` 设置托盘提示文本
- 使用 `TrayIcon.Menu` 和 `NativeMenu` 定义右键菜单项
- 支持嵌套菜单和分隔符
- 通过事件处理器响应菜单点击
- 示例代码：
```xml
<!-- 在 App.axaml 中定义 -->
<TrayIcon.Icons>
  <TrayIcons>
    <TrayIcon Icon="/Assets/avalonia-logo.ico"
              ToolTipText="AvaChat - 实时聊天系统">
      <TrayIcon.Menu>
        <NativeMenu>
          <NativeMenuItem Header="显示主窗口" Click="ShowWindow_Click"/>
          <NativeMenuItem Header="设置">
            <NativeMenu>
              <NativeMenuItem Header="个人资料" Click="Profile_Click"/>
              <NativeMenuItem Header="消息设置" Click="MessageSettings_Click"/>
              <NativeMenuItemSeparator />
              <NativeMenuItem Header="主题切换" Click="ThemeSwitch_Click"/>
            </NativeMenu>
          </NativeMenuItem>
          <NativeMenuItemSeparator/>
          <NativeMenuItem Header="退出" Click="Exit_Click"/>
        </NativeMenu>
      </TrayIcon.Menu>
    </TrayIcon>
  </TrayIcons>
</TrayIcon.Icons>
```

### 异步编程
- 所有 I/O 操作使用 `async/await`
- UI 操作在主线程执行
- 网络请求和数据库操作在后台线程

### 错误处理
- 全局异常处理机制
- 用户友好的错误提示
- 网络异常自动重连

## 开发里程碑

1. **基础架构:** 项目结构、MVVM框架、数据模型
2. **UI组件:** 登录界面、主窗口、好友列表、聊天窗口
3. **网络通信:** 登录验证、消息收发
4. **增强功能:** 系统托盘、通知、聊天记录
5. **优化完善:** 性能优化、错误处理、UI细节

---

**目标:** 构建现代化、稳定、用户体验优秀的跨平台实时聊天系统，展示 Avalonia UI 和现代 .NET 技术栈的最佳实践。
