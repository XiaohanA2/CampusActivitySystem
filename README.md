# 校园活动管理系统 (Campus Activity Management System)

## 📋 项目简介

校园活动管理系统是一个基于 .NET 8 的现代化企业级应用，专为高校校园活动管理而设计。系统采用分层架构设计，集成了 C++/CLI 混合编程和原生 C++ 模块，提供了完整的活动生命周期管理、智能推荐、用户权限控制等功能。

### 🎯 项目目标
- 为高校提供一站式的校园活动管理平台
- 展示现代企业级应用开发的最佳实践
- 演示多种编程语言和技术的集成应用
- 提供智能化的活动推荐和数据分析功能

## 🏗️ 技术架构

### 核心技术栈
- **后端框架**: ASP.NET Core 8.0
- **前端框架**: Blazor Server
- **数据库**: MySQL 8.0 + Entity Framework Core
- **缓存**: Redis 6.0+
- **认证**: JWT Bearer Token
- **对象映射**: AutoMapper
- **API文档**: Swagger/OpenAPI
- **日志**: Serilog

### 混合编程技术
- **C++/CLI**: 智能推荐引擎
- **原生C++**: 高性能数据分析库
- **P/Invoke**: 托管与非托管代码互操作

### 系统架构图
```
┌─────────────────────────────────────────────────────────────┐
│                    校园活动管理系统                          │
├─────────────────────────────────────────────────────────────┤
│  表示层 (Presentation Layer)                                │
│  ├── CampusActivity.BlazorWeb (Blazor Server UI)           │
│  └── CampusActivity.WebAPI (REST API)                      │
├─────────────────────────────────────────────────────────────┤
│  应用层 (Application Layer)                                 │
│  └── CampusActivity.Application (业务逻辑服务)              │
├─────────────────────────────────────────────────────────────┤
│  基础设施层 (Infrastructure Layer)                          │
│  └── CampusActivity.Infrastructure (数据访问、仓储)         │
├─────────────────────────────────────────────────────────────┤
│  领域层 (Domain Layer)                                      │
│  └── CampusActivity.Domain (实体模型、业务规则)             │
├─────────────────────────────────────────────────────────────┤
│  共享层 (Shared Layer)                                      │
│  └── CampusActivity.Shared (DTOs、常量、枚举)               │
├─────────────────────────────────────────────────────────────┤
│  原生模块 (Native Modules)                                  │
│  ├── CampusActivity.NativeLib (C++/CLI 推荐引擎)           │
│  └── CampusActivity.Core (C++ 数据分析库)                   │
└─────────────────────────────────────────────────────────────┘
```

## 🚀 项目亮点

### 1. 超越要求的架构设计
- **8个程序集**: 远超要求的5个程序集，展示完整的分层架构
- **清晰的分层**: Domain、Infrastructure、Application、WebAPI、BlazorWeb分离
- **依赖注入**: 完整的IoC容器配置和服务注册

### 2. 混合编程技术集成
- **C++/CLI推荐引擎**: 高性能的协同过滤算法实现
- **原生C++数据分析**: 通过P/Invoke调用的高性能计算模块
- **托管与非托管代码互操作**: 展示.NET与C++的深度集成

### 3. 现代化Web技术
- **Blazor Server**: 响应式Web UI，实时数据更新
- **Bootstrap 5**: 现代化UI组件和响应式设计
- **SignalR**: 实时通信和状态同步

### 4. 企业级功能特性
- **JWT认证**: 安全的身份验证和授权机制
- **角色权限**: 学生、教师、管理员多角色权限控制
- **数据缓存**: Redis缓存提升系统性能
- **API文档**: 完整的Swagger API文档

## 📋 功能模块

### 🔐 用户管理
- **多角色认证**: 学生、教师、管理员三种角色
- **JWT令牌**: 安全的身份验证机制
- **权限控制**: 基于角色的访问控制(RBAC)
- **用户画像**: 用户兴趣偏好分析

### 📅 活动管理
- **活动CRUD**: 完整的活动生命周期管理
- **分类管理**: 学术讲座、文艺演出、体育竞技等分类
- **状态跟踪**: 草稿、已发布、进行中、已结束等状态
- **报名管理**: 在线报名、取消报名、名额控制
- **图片上传**: 活动封面图片管理

### 🤖 智能推荐
- **协同过滤**: 基于用户行为的推荐算法
- **内容推荐**: 基于用户偏好的推荐算法
- **混合算法**: 多种算法结合的智能推荐
- **C++加速**: 原生代码提升计算性能
- **实时更新**: 用户行为驱动的推荐更新

### 📊 数据分析
- **参与统计**: 活动参与度分析
- **趋势分析**: 活动热度趋势分析
- **用户画像**: 用户兴趣偏好分析
- **实时监控**: 系统运行状态监控

### 💬 实时通信
- **聊天功能**: 活动参与者实时交流
- **状态同步**: 活动状态实时更新
- **通知系统**: 重要事件实时通知

## 🛠️ 开发环境要求

### 必需软件
- **.NET 8 SDK**
- **Visual Studio 2022** (用于C++编译)
- **MySQL 8.0+**
- **Redis 6.0+**
- **PowerShell** (Windows环境)

### 可选软件
- **CMake** (用于C++原生库编译)
- **Git** (版本控制)

## 🚀 快速开始

### 自动化安装（推荐）
```powershell
# 克隆项目
git clone <repository-url>
cd CampusActivitySystem

# 运行安装脚本（自动配置环境）
.\install.ps1

# 启动系统
.\start.ps1
```

### 手动安装
### 1. 克隆项目
```powershell
git clone <repository-url>
cd CampusActivitySystem
```

### 2. 配置数据库
```sql
CREATE DATABASE CampusActivityDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 3. 更新配置文件
编辑 `src/CampusActivity.WebAPI/appsettings.json`：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CampusActivityDB;User=root;Password=your_password;CharSet=utf8mb4;",
    "Redis": "localhost:6379"
  }
}
```

### 4. 构建项目
```powershell
# 运行构建脚本
.\build.ps1
```

### 5. 运行系统
```powershell
# 启动 Web API
cd src/CampusActivity.WebAPI
dotnet run

# 启动 Blazor 前端
cd src/CampusActivity.BlazorWeb
dotnet run
```

或者使用启动脚本：
```powershell
# 使用自动化启动脚本
.\start.ps1
```

### 6. 访问系统
- **前端界面**: http://localhost:7150
- **API文档**: http://localhost:7186/swagger
- **默认管理员**: admin / admin123

## 📁 项目结构

```
CampusActivitySystem/
├── src/
│   ├── CampusActivity.Shared/           # 共享库
│   │   ├── DTOs/                        # 数据传输对象
│   │   ├── Constants/                   # 应用常量
│   │   └── Enums/                       # 枚举定义
│   ├── CampusActivity.Domain/           # 领域层
│   │   └── Entities/                    # 实体模型
│   ├── CampusActivity.Infrastructure/   # 基础设施层
│   │   ├── Data/                        # 数据上下文
│   │   ├── Repositories/                # 仓储实现
│   │   └── UnitOfWork/                  # 工作单元
│   ├── CampusActivity.Application/      # 应用层
│   │   ├── Services/                    # 业务服务
│   │   └── Mappings/                    # 对象映射
│   ├── CampusActivity.WebAPI/           # Web API
│   │   └── Controllers/                 # API控制器
│   ├── CampusActivity.BlazorWeb/        # Blazor前端
│   │   ├── Pages/                       # 页面组件
│   │   ├── Shared/                      # 共享组件
│   │   └── Services/                    # 前端服务
│   ├── CampusActivity.NativeLib/        # C++/CLI库
│   │   ├── RecommendationEngine.h       # 推荐引擎头文件
│   │   └── RecommendationEngine.cpp     # 推荐引擎实现
│   └── CampusActivity.Core/             # C++原生库
│       ├── CMakeLists.txt               # CMake配置
│       ├── include/                     # 头文件
│       └── src/                         # 源文件
├── docs/                                # 文档
│   ├── 数据库配置指南.md
│   ├── 部署运行指南.md
│   └── 项目结构说明.md
├── build.ps1                            # 构建脚本
└── README.md                            # 项目说明
```

## 🔧 技术实现细节

### C++模块集成
```csharp
// P/Invoke 调用 C++ 原生库
[DllImport("CampusActivity.Core.dll")]
private static extern double CalculateSimilarity(
    int[] userActivities1, int[] userActivities2, 
    int count1, int count2);

// C++/CLI 混合编程示例
// using CampusActivity.NativeLib;
// var recommendations = RecommendationEngine.GetRecommendations(userId);
```

### 数据库设计
- **用户表**: 存储用户基本信息和角色
- **活动表**: 活动详细信息和状态
- **报名表**: 用户活动报名记录
- **偏好表**: 用户活动偏好权重
- **分类表**: 活动分类信息

### 安全机制
- **JWT认证**: 无状态的用户认证
- **角色权限**: 细粒度的权限控制
- **数据验证**: 输入数据的安全验证
- **SQL注入防护**: 参数化查询

## 🎛️ 管理脚本

系统提供了完整的自动化管理脚本，简化部署和运维工作：

### 📦 构建脚本 (build.ps1)
```powershell
# 基本构建
.\build.ps1

# 调试版本构建
.\build.ps1 Debug

# 清理后构建
.\build.ps1 Release -Clean

# 查看帮助
.\build.ps1 -Help
```

### ⚙️ 安装脚本 (install.ps1)
```powershell
# 开发环境安装
.\install.ps1

# 生产环境安装
.\install.ps1 -Environment Production

# 跳过数据库初始化
.\install.ps1 -SkipDatabase

# 查看帮助
.\install.ps1 -Help
```

### 🚀 启动脚本 (start.ps1)
```powershell
# 启动开发环境
.\start.ps1

# 启动生产环境
.\start.ps1 -Environment Production

# 启动后等待用户输入
.\start.ps1 -WaitForExit

# 查看帮助
.\start.ps1 -Help
```

### 🛑 停止脚本 (stop.ps1)
```powershell
# 正常停止
.\stop.ps1

# 强制停止
.\stop.ps1 -Force

# 查看帮助
.\stop.ps1 -Help
```

## 🧪 测试

### 运行测试
```powershell
# 运行所有测试
dotnet test

# 运行特定项目测试
dotnet test src/CampusActivity.Tests/
```

### API测试
- 访问 http://localhost:7186/swagger 查看API文档
- 使用Swagger UI进行API测试
- 支持JWT认证的API调用

## 📚 文档

- [数据库配置指南](docs/数据库配置指南.md)
- [部署运行指南](docs/部署运行指南.md)
- [项目结构说明](docs/项目结构说明.md)

## 📋 项目提交清单

### 文件清单

- [x] 完整的源代码目录(src/)
- [x] 完整的文档目录(docs/)
- [x] 解决方案文件(CampusActivitySystem.sln)
- [x] 构建脚本(build.ps1) - 自动化构建系统
- [x] 安装脚本(install.ps1) - 自动化环境配置
- [x] 启动脚本(start.ps1) - 一键启动服务
- [x] 停止脚本(stop.ps1) - 一键停止服务
- [x] 项目说明(README.md)
- [x] Git忽略文件(.gitignore)

### 文档清单
- [ ] README.md - 项目说明
- [ ] docs/部署运行指南.md - 部署指南
- [ ] docs/数据库配置指南.md - 数据库配置
- [ ] docs/项目结构说明.md - 项目结构
