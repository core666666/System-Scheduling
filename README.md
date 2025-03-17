# 值日排班系统

## 项目概述

值日排班系统是一个基于.NET 7 Blazor Server的Web应用程序，用于管理员工值班安排和通知。系统支持用户认证、值班人员管理、排班安排以及自动通知功能，旨在提高组织值班安排的效率和可靠性。

## 技术栈

* .NET 7
* Blazor Server
* Entity Framework Core
* SQLite 数据库
* 基于Cookie的身份验证
* Bootstrap 5 (UI框架)

## 系统架构

本系统采用传统的三层架构:

1. **表示层**: Blazor Server页面和组件，提供交互式用户界面
2. **业务逻辑层**: 服务类处理核心业务逻辑，如排班算法和通知管理
3. **数据访问层**: 通过EF Core访问SQLite数据库，实现数据持久化

## 主要功能

### 用户认证和授权
系统实现了基于Cookie的身份认证机制，支持用户登录、注销和会话管理，确保系统安全性。

![用户登录界面](https://image.baidu.com/search/down?url=http://tvax3.sinaimg.cn/large/0061Cjilly1hzjsd2wl1uj31hc0pbnmb.jpg)

### 仪表盘
仪表盘提供系统的整体概览，包括以下关键信息:

- 今日值班人员信息展示
- 通知发送状态统计
- 快捷导航栏

![仪表盘界面](https://image.baidu.com/search/down?url=http://tvax3.sinaimg.cn/large/0061Cjilly1hzjsfd735hj31ha0p8ags.jpg)

### 值班人员管理
支持添加、编辑、删除值班人员信息，包括姓名、部门、联系方式等基本信息管理。

![人员管理界面](https://image.baidu.com/search/down?url=http://tvax2.sinaimg.cn/large/0061Cjilly1hzjsfsof4qj31hb0p3gss.jpg)

### 排班表生成和管理
提供灵活的排班规则设置，支持自动生成排班表、手动调整排班、排班冲突检测等功能。

![排班管理界面](https://image.baidu.com/search/down?url=http://tvax2.sinaimg.cn/large/0061Cjilly1hzjsgxysvgg31hb0p4478.gif)

### 通知配置
可配置通知时间、通知方式和通知内容模板，支持定时通知和即时通知。

![通知配置界面](https://image.baidu.com/search/down?url=http://tvax4.sinaimg.cn/large/0061Cjilly1hzjshrpibcj31hc13s4bf.jpg)

### 钉钉消息通知集成
集成钉钉开放平台API，实现值班提醒自动推送到钉钉工作群或个人。

![钉钉通知效果](https://image.baidu.com/search/down?url=http://tvax4.sinaimg.cn/large/0061Cjilly1hzjsig3o5wj30gg0bvdj1.jpg)

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
* 现代浏览器（Chrome、Firefox、Edge等）

### 安装步骤

1. 克隆或下载代码库到本地
2. 在项目根目录打开命令行
3. 执行以下命令:

```shell
cd SchedulingApplication
dotnet restore
dotnet run
```

4. 打开浏览器访问 `https://localhost:5001` 或控制台输出的URL

### 配置说明

主要配置在`appsettings.json`文件中:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=scheduling.db"  // SQLite数据库文件路径
  },
  "DingTalk": {
    "AppKey": "your-app-key",
    "AppSecret": "your-app-secret",
    "AgentId": "your-agent-id"
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
   - 建议启用HTTPS，确保数据传输安全

2. **数据库**:
   - 系统首次运行会自动创建SQLite数据库文件
   - 定期备份数据库文件以防止数据丢失
   - 考虑在生产环境中使用更强大的数据库系统（如SQL Server、MySQL等）

3. **钉钉通知**:
   - 当前DingTalkService是模拟实现，实际使用时需要集成钉钉开放平台API
   - 钉钉集成需要配置企业ID和应用密钥
   - 确保网络环境可正常访问钉钉API

4. **认证与授权**:
   - 系统使用Cookie认证，会话默认7天过期
   - CustomAuthStateProvider负责处理认证状态
   - 可根据需要调整Cookie过期时间和安全策略

5. **环境兼容性**:
   - 确保服务器环境支持.NET 7
   - 如需部署到IIS，请确保安装ASP.NET Core托管包
   - 系统UI设计响应式，支持PC和移动设备访问

## 系统扩展建议

1. 添加更丰富的角色和权限控制，实现基于角色的访问控制（RBAC）
2. 实现排班冲突检测和提醒，确保排班合理性
3. 增加历史数据统计和报表功能，提供数据分析支持
4. 提供移动端兼容视图或开发配套APP，方便移动使用
5. 集成其他通知渠道(如邮件、短信、企业微信等)
6. 引入人工智能算法优化排班，考虑公平性和效率
7. 提供多语言支持，扩大适用范围

## 问题排查

如遇系统问题，可尝试以下步骤:

1. 检查日志信息(`logs/`目录)
2. 确认数据库连接正常
3. 验证认证状态提供程序是否正确注册
4. 清除浏览器缓存并重试
5. 确认网络连接状态，特别是与钉钉API的连接

