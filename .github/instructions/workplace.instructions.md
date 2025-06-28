---
applyTo: '**'
---

# AvaChat 工作区开发指南

## 项目架构

### 总体架构
- **解决方案类型:** .NET 9 跨平台桌面应用解决方案
- **架构模式:** 客户端/服务器分离架构
- **UI 框架:** Avalonia UI (支持 Windows、macOS、Linux)
- **MVVM 框架:** CommunityToolkit.Mvvm

### 项目组件
- **`AvaChat.Client`:** Avalonia UI 客户端桌面应用程序
  - 用户界面和交互逻辑
  - 本地数据存储和缓存
  - 网络通信客户端
- **`AvaChat.Server`:** Avalonia UI 服务器管理应用程序
  - 服务器管理控制台
  - 用户连接管理
  - 消息转发和广播
- **`AvaChat.Shared`:** 共享类库 (推荐添加)
  - 通用数据模型
  - 网络通信协议
  - 工具类和扩展方法

## Core Commands

### New Project
- To create a new project, use the following command:
  ```
  dotnet new sln -n AvaChat --format slnx
  ```

- To create a new Avalonia MVVM project, run:
  ```
  dotnet new avalonia.mvvm -n AvaChat.Client
  ```

### Add Project to Solution
- To add an existing project to the solution, use:
  ```
  dotnet sln AvaChat.slnx add <path-to-project-file>
  ```

### Build

- To build the entire solution:
  ```
  dotnet build AvaChat.slnx
  ```
- To build a specific project (e.g., the client):
  ```
  dotnet build AvaChat.Client/AvaChat.Client.csproj
  ```

### Run

- To run the server:
  ```
  dotnet run --project AvaChat.Server/AvaChat.Server.csproj
  ```
- To run the client:
  ```
  dotnet run --project AvaChat.Client/AvaChat.Client.csproj
  ```

### Testing

- There are currently no test projects in the solution. If you add tests, please document the test running commands here.

## Package Management

- This solution uses Central Package Management (CPM).
- Use `dotnet new packagesprops` to create a `Directory.Packages.props` file in the solution root.
- All package versions are managed in the `Directory.Packages.props` file.
- When adding or updating a package, modify `Directory.Packages.props`.
- In individual `.csproj` files, `PackageReference` items should not include a `Version` attribute.

### Adding a package via CLI

To add a package from the command line, use the standard `dotnet add package` command. For example, to add `System.Text` to the client project:

```
dotnet add AvaChat.Client/AvaChat.Client.csproj package System.Text
```

This will automatically update `Directory.Packages.props` with the package version and add the version-less `PackageReference` to the project file.

Notice:
- Use Microsoft's Official packages first, such as `Microsoft.Extensions.DependencyInjection`, `Microsoft.EntityFrameworkCore`, etc.
- Use Microsoft.Extensions.DependencyInjection for dependency injection in both client and server projects.