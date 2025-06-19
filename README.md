# 校园活动管理系统

一个基于 .NET 9 的现代化校园活动管理系统，集成了 C++/CLI 混合编程和原生 C++ 模块，展示了完整的企业级应用架构。

## 🎯 项目特色

### 技术架构亮点
- **8个程序集**：超越要求的5个程序集，展示完整分层架构
- **C++/CLI混合编程**：智能推荐引擎，展示托管与非托管代码互操作
- **C++原生模块**：高性能数据分析库，通过P/Invoke调用
- **Blazor Server**：现代化Web UI，提供响应式用户体验
- **分层架构**：Domain、Infrastructure、Application、WebAPI、BlazorWeb分离
- **现代.NET技术栈**：EF Core、AutoMapper、JWT、Redis缓存

### 业务功能完整
- **多角色系统**：学生、教师、管理员权限管理
- **活动全生命周期**：创建、发布、报名、管理
- **智能推荐**：协同过滤 + 基于内容的混合推荐算法
- **数据分析**：活动趋势分析、参与度统计
- **实时功能**：活动状态更新、报名状态跟踪

## 🏗️ 系统架构

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

## 🚀 快速开始

### 环境要求
- **.NET 9 SDK**
- **MySQL 8.0+**
- **Redis 6.0+**
- **Visual Studio 2022** (用于C++编译)

### 1. 克隆项目
```bash
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

### 4. 运行系统
```bash
# 启动 Web API
cd src/CampusActivity.WebAPI
dotnet run --urls "https://localhost:7000"

# 启动 Blazor 前端
cd src/CampusActivity.BlazorWeb
dotnet run --urls "https://localhost:7001"
```

### 5. 访问系统
- **前端界面**: https://localhost:7001
- **API文档**: https://localhost:7000/swagger
- **默认管理员**: admin / admin123

## 📋 功能模块

### 🔐 用户管理
- **多角色认证**：学生、教师、管理员
- **JWT令牌**：安全的身份验证机制
- **权限控制**：基于角色的访问控制

### 📅 活动管理
- **活动CRUD**：完整的活动生命周期管理
- **分类管理**：学术讲座、文艺演出、体育竞技等
- **状态跟踪**：草稿、已发布、已结束等状态
- **报名管理**：在线报名、取消报名、名额控制

### 🤖 智能推荐
- **协同过滤**：基于用户行为的推荐
- **内容推荐**：基于用户偏好的推荐
- **混合算法**：多种算法结合的智能推荐
- **C++加速**：原生代码提升计算性能

### 📊 数据分析
- **参与统计**：活动参与度分析
- **趋势分析**：活动热度趋势
- **用户画像**：用户兴趣偏好分析
- **实时监控**：系统运行状态监控

## 🔧 技术实现

### 核心技术栈
- **后端框架**: ASP.NET Core 9.0
- **前端框架**: Blazor Server
- **数据库**: MySQL 8.0 + Entity Framework Core
- **缓存**: Redis
- **认证**: JWT Bearer Token
- **映射**: AutoMapper
- **日志**: Serilog
- **API文档**: Swagger/OpenAPI

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

## 📁 项目结构

```
CampusActivitySystem/
├── src/
│   ├── CampusActivity.Shared/           # 共享库
│   │   ├── DTOs/                        # 数据传输对象
│   │   ├── Constants/                   # 应用常量
│   │   └── Enums/                       # 枚举定义
│   ├── CampusActivity.Domain/           # 领域层
│   │   ├── Entities/                    # 实体模型
│   │   └── Common/                      # 基础实体
│   ├── CampusActivity.Infrastructure/   # 基础设施层
│   │   ├── Data/                        # 数据上下文
│   │   ├── Repositories/                # 仓储实现
│   │   └── UnitOfWork/                  # 工作单元
│   ├── CampusActivity.Application/      # 应用层
│   │   ├── Services/                    # 业务服务
│   │   ├── Interfaces/                  # 服务接口
│   │   └── Mappings/                    # 对象映射
│   ├── CampusActivity.WebAPI/           # Web API
│   │   ├── Controllers/                 # API控制器
│   │   ├── Middleware/                  # 中间件
│   │   └── Extensions/                  # 扩展方法
│   ├── CampusActivity.BlazorWeb/        # Blazor前端
│   │   ├── Pages/                       # 页面组件
│   │   ├── Shared/                      # 共享组件
│   │   └── Services/                    # 前端服务
│   ├── CampusActivity.NativeLib/        # C++/CLI库
│   │   ├── RecommendationEngine.h       # 推荐引擎头文件
│   │   └── RecommendationEngine.cpp     # 推荐引擎实现
│   └── CampusActivity.Core/             # C++原生库
│       ├── CMakeLists.txt               # CMake配置
│       ├── DataAnalysis.h               # 数据分析头文件
│       └── DataAnalysis.cpp             # 数据分析实现
├── tests/                               # 测试项目
├── docs/                                # 文档
│   ├── 数据库配置指南.md
│   ├── 部署运行指南.md
│   └── 项目结构说明.md
├── build.ps1                            # 构建脚本
└── README.md                            # 项目说明
```

## 🧪 测试

### 运行测试
```bash
# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test tests/CampusActivity.Application.Tests
```

### API测试
```bash
# 登录获取Token
curl -X POST "https://localhost:7000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin123"}'

# 获取活动列表
curl -X GET "https://localhost:7000/api/activities" \
  -H "Authorization: Bearer <your-token>"
```

## 🐳 Docker部署

### 使用Docker Compose
```bash
# 构建并启动所有服务
docker-compose up -d

# 查看服务状态
docker-compose ps

# 查看日志
docker-compose logs -f
```

### 服务端口
- **Web API**: http://localhost:5000
- **Blazor Web**: http://localhost:5001
- **MySQL**: localhost:3306
- **Redis**: localhost:6379

## 📈 性能优化

### 数据库优化
- **索引策略**: 为常用查询字段添加索引
- **连接池**: 配置数据库连接池参数
- **查询优化**: 使用EF Core查询优化技巧

### 缓存策略
- **Redis缓存**: 推荐结果、用户会话缓存
- **内存缓存**: 静态数据、配置信息缓存
- **分布式缓存**: 支持多实例部署

### C++性能提升
- **原生计算**: 复杂算法使用C++实现
- **并行处理**: 利用多核CPU并行计算
- **内存优化**: 减少托管/非托管转换开销

## 🔒 安全特性

### 认证授权
- **JWT令牌**: 无状态身份验证
- **角色权限**: 细粒度权限控制
- **HTTPS**: 全站HTTPS加密传输

### 数据安全
- **密码加密**: BCrypt密码哈希
- **SQL注入防护**: 参数化查询
- **XSS防护**: 输入验证和输出编码

## 📚 文档

- [数据库配置指南](docs/数据库配置指南.md)
- [部署运行指南](docs/部署运行指南.md)
- [项目结构说明](docs/项目结构说明.md)
- [API文档](https://localhost:7000/swagger)

## 🤝 贡献指南

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开 Pull Request
