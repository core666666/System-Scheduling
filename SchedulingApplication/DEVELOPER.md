# 值日排班系统 - 开发者指南

本文档为值日排班系统的开发者指南，包含系统架构、关键组件、开发约定和常见问题的解决方案。

## 代码组织

本项目采用标准的Blazor Server应用结构，并遵循以下组织原则：

### 关键目录和文件

- **Data/** - 数据访问层
  - ApplicationDbContext.cs - EF Core数据库上下文

- **Models/** - 领域模型
  - 各实体类定义
  - 视图模型和数据传输对象

- **Services/** - 业务逻辑层
  - 身份验证和授权服务
  - 排班业务逻辑
  - 通知服务

- **Pages/** - Blazor页面组件
  - 各功能页面
  - 局部组件

- **Shared/** - 共享UI组件
  - 布局组件
  - 导航组件
  - 对话框组件

## 身份验证与授权

系统使用基于Cookie的身份验证，关键组件包括：

1. **CustomAuthStateProvider** - 自定义认证状态提供器
   - 负责从HttpContext解析当前用户
   - 提供给Blazor组件使用的身份验证状态

2. **AuthService** - 身份验证服务
   - 处理用户登录和注销
   - 验证用户凭据
   - 创建认证Cookie

### 添加新的受保护页面

要创建需要认证的页面，使用`[Authorize]`特性：

```csharp
@page "/protected-page"
@attribute [Authorize]

<h3>受保护的页面</h3>

@code {
    // 页面逻辑
}
```

## 数据库架构和迁移

系统使用EF Core和SQLite数据库。主要实体包括：

1. **User** - 系统用户
2. **Staff** - 值班人员
3. **DutySchedule** - 值班安排
4. **NotificationConfig** - 通知配置

### 数据库更新流程

当模型发生变化时，按以下步骤更新数据库：

1. 打开命令行并导航到项目目录
2. 添加新迁移：
   ```
   dotnet ef migrations add MigrationName
   ```
3. 应用迁移到数据库：
   ```
   dotnet ef database update
   ```

## 钉钉通知集成

当前系统使用模拟的钉钉通知服务。要实现真实集成，需要：

1. 在钉钉开放平台创建应用
2. 获取企业ID和应用密钥
3. 更新DingTalkService实现，使用钉钉API发送消息

### 实现真实钉钉通知的步骤

1. 添加钉钉SDK包引用：
   ```
   dotnet add package DingTalk.Api
   ```

2. 在appsettings.json中添加钉钉配置：
   ```json
   "DingTalk": {
     "CorpId": "your-corp-id",
     "AppKey": "your-app-key",
     "AppSecret": "your-app-secret"
   }
   ```

3. 更新DingTalkService实现，使用钉钉SDK发送消息

## 排班算法

系统的排班逻辑可以在以下几个方向扩展：

1. **轮换排班** - 确保每个员工的值班负担均衡
2. **偏好考虑** - 考虑员工的时间偏好
3. **冲突检测** - 识别和解决排班冲突

## 常见问题与解决方案

### 认证问题

如果遇到认证相关问题：

1. 确保Program.cs中正确注册了认证服务和中间件：
   ```csharp
   builder.Services.AddAuthentication("Cookies")
       .AddCookie(options => {...});
   builder.Services.AddAuthorization();
   // ...
   app.UseAuthentication();
   app.UseAuthorization();
   ```

2. 确保注册了CustomAuthStateProvider：
   ```csharp
   builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
   ```

3. 检查CascadingAuthenticationState是否正确配置在App.razor中

### 数据库连接问题

如果遇到数据库连接问题：

1. 确认appsettings.json中的连接字符串格式正确
2. 验证应用有权限写入数据库文件所在位置
3. 检查EF Core迁移是否正确应用

### 页面未更新

如果Blazor组件未正确反映数据更新：

1. 确保使用StateHasChanged()通知UI更新
2. 检查是否正确使用@bind指令
3. 验证异步操作是否正确处理

## 性能优化建议

1. **组件优化**
   - 使用@key指令优化列表渲染
   - 实现IDisposable释放资源
   - 避免过度渲染

2. **数据加载优化**
   - 使用分页加载大量数据
   - 实现数据缓存
   - 优化EF Core查询

3. **认证性能**
   - 考虑使用JWT而非Cookie认证以减少服务器状态
   - 实现适当的缓存策略

## 测试策略

1. **单元测试**
   - 测试服务层逻辑
   - 模拟数据库上下文

2. **集成测试**
   - 测试页面导航流程
   - 验证数据更新反映在UI上

3. **身份验证测试**
   - 测试受保护资源访问
   - 验证授权策略

## 部署指南

### IIS部署

1. 安装.NET 7 Hosting Bundle
2. 发布应用：
   ```
   dotnet publish -c Release
   ```
3. 在IIS中创建网站，指向publish目录
4. 配置应用程序池为"无托管代码"

### Docker部署

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SchedulingApplication/SchedulingApplication.csproj", "SchedulingApplication/"]
RUN dotnet restore "SchedulingApplication/SchedulingApplication.csproj"
COPY . .
WORKDIR "/src/SchedulingApplication"
RUN dotnet build "SchedulingApplication.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SchedulingApplication.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SchedulingApplication.dll"]
```

## 版本控制与变更管理

1. 使用语义化版本控制 (SemVer)
2. 为每个重要变更添加CHANGELOG条目
3. 定期标记发布版本

## 联系与支持

如有开发相关问题，请联系技术团队负责人：
- 电子邮件：[email@example.com]
- 内部工单系统：[URL] 