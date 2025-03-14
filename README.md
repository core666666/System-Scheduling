# 值日排班系统

## 项目概述

值日排班系统是一个基于.NET 7 Blazor Server的Web应用程序，用于管理员工值班安排和通知。系统支持用户认证、值班人员管理、排班安排以及自动通知功能。

## 技术栈

* .NET 7
* Blazor Server
* Entity Framework Core
* SQLite 数据库
* 基于Cookie的身份验证
* Bootstrap 5 (UI框架)

## 系统架构

本系统采用传统的三层架构:

1. **表示层**: Blazor Server页面和组件
2. **业务逻辑层**: 服务类处理业务逻辑
3. **数据访问层**: 通过EF Core访问SQLite数据库

## 主要功能

* 用户认证和授权
* 值班人员管理(添加、编辑、删除)
* 排班表生成和管理
* 通知配置
* 钉钉消息通知集成

## 项目结构

```
SchedulingApplication/
├── Data/                  # 数据访问相关
│   └── ApplicationDbContext.cs  # EF Core数据库上下文
├── Models/                # 数据模型
│   ├── User.cs            # 用户模型
│   ├── Staff.cs           # 值班人员模型
│   ├── DutySchedule.cs    # 值班安排模型
│   ├── NotificationConfig.cs  # 通知配置模型
│   └── LoginModel.cs      # 登录表单模型
├── Pages/                 # Blazor页面
│   ├── _Host.cshtml       # 主机页面
│   ├── _Layout.cshtml     # 布局页面
│   └── Login.razor        # 登录页面
├── Services/              # 业务逻辑服务
│   ├── AuthService.cs     # 身份验证服务
│   ├── CustomAuthStateProvider.cs  # 认证状态提供器
│   ├── DingTalkService.cs # 钉钉通知服务
│   └── IDingTalkService.cs  # 钉钉服务接口
├── Shared/                # 共享组件
│   ├── MainLayout.razor   # 主布局组件
│   └── NavMenu.razor      # 导航菜单组件
├── wwwroot/               # 静态资源
├── App.razor              # 应用主组件
├── _Imports.razor         # 全局导入
├── Program.cs             # 应用入口点
└── appsettings.json       # 应用配置
```

## 安装与配置

### 前置条件

* .NET 7 SDK
* 支持SQLite的开发环境

### 安装步骤

1. 克隆或下载代码库到本地
2. 在项目根目录打开命令行
3. 执行以下命令:

```shell
cd SchedulingApplication
dotnet restore
dotnet run
```

### 配置说明

主要配置在`appsettings.json`文件中:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=scheduling.db"  // SQLite数据库文件路径
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 认证系统

系统使用基于Cookie的身份验证，初始管理员账户:

* 用户名: admin
* 密码: admin123 (请在生产环境中更改)

## 系统使用说明

1. 访问应用程序URL后，系统会自动重定向到登录页面
2. 使用管理员账户登录系统
3. 在导航菜单中选择功能:
   - 人员管理: 添加和管理值班人员
   - 排班管理: 创建和查看值班安排
   - 通知配置: 设置通知时间和启用/禁用状态

## 重要注意事项

1. **安全考虑**:
   - 默认用户名和密码仅用于开发环境，生产环境应当更改
   - 生产环境中应实现密码加密存储

2. **数据库**:
   - 系统首次运行会自动创建SQLite数据库文件
   - 定期备份数据库文件以防止数据丢失

3. **钉钉通知**:
   - 当前DingTalkService是模拟实现，实际使用时需要集成钉钉开放平台API
   - 钉钉集成需要配置企业ID和应用密钥

4. **认证与授权**:
   - 系统使用Cookie认证，会话默认7天过期
   - CustomAuthStateProvider负责处理认证状态

5. **环境兼容性**:
   - 确保服务器环境支持.NET 7
   - 如需部署到IIS，请确保安装ASP.NET Core托管包

## 系统扩展建议

1. 添加更丰富的角色和权限控制
2. 实现排班冲突检测和提醒
3. 增加历史数据统计和报表功能
4. 提供移动端兼容视图或APP
5. 集成其他通知渠道(如邮件、短信等)

## 问题排查

如遇系统问题，可尝试以下步骤:

1. 检查日志信息(`logs/`目录)
2. 确认数据库连接正常
3. 验证认证状态提供程序是否正确注册
4. 清除浏览器缓存并重试

## 开发者信息

本系统由[开发者姓名/团队]开发。
如有问题请联系[联系方式]。 