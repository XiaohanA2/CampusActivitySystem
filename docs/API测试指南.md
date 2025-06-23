# API测试指南

## 概述

本项目的API测试页面提供了完整的API端点测试功能，支持一键测试所有API接口，也可以单独测试每个API端点。

## 访问方式

1. 启动项目后，访问 `/api-test` 页面
2. 或者通过导航菜单进入"API测试"页面

## 界面功能

### 1. 配置信息区域
- 显示API基础URL
- 显示当前时间和认证状态
- 提供用户登录/登出功能
- 默认测试账号：用户名 `admin`，密码 `admin123`

### 2. 批量测试功能
- **测试所有API**：一键测试所有API端点
- **测试公开API**：仅测试无需认证的公开API
- **清空日志**：清除测试结果记录

### 3. 分类API测试按钮
所有API按功能分类，以按钮形式展示，支持单独测试：

#### 🟢 公开API测试
无需登录即可测试的API：
- 基础连接测试
- 健康检查
- 数据库连接测试
- 数据库状态检查
- 获取活动列表
- 获取活动分类
- 获取热门活动

#### 🔐 认证API测试
需要登录后测试的API：
- 获取用户资料
- 用户注册测试
- 获取我的报名活动
- 创建活动测试

#### 📅 日程表API测试
**基础操作：**
- 创建日程项
- 获取日程列表
- 更新日程项
- 删除日程项

**状态管理：**
- 切换完成状态
- 获取即将到来的日程
- 获取逾期日程

**视图和统计：**
- 获取日历视图
- 获取日程统计
- 添加活动到日程
- 从日程移除活动

#### 🎯 推荐系统API测试
- 个性化推荐
- 协同过滤推荐
- 基于内容的推荐
- 获取用户偏好
- 更新用户偏好
- 重新计算推荐

## 测试功能详解

### 1. 公开API测试
无需登录即可测试的API：
- `GET /api/test` - 基础连接测试
- `GET /api/test/health` - 健康检查
- `GET /api/test/database` - 数据库连接测试
- `GET /api/test/status` - 数据库状态检查
- `GET /api/activities` - 获取活动列表
- `GET /api/activities/categories` - 获取活动分类
- `GET /api/activities/popular` - 获取热门活动

### 2. 认证API测试
需要登录后测试的API：
- `GET /api/auth/profile` - 获取用户资料
- `POST /api/auth/register` - 用户注册
- `GET /api/activities/my-registrations` - 获取我的报名活动
- `POST /api/activities` - 创建活动（需要教师/管理员权限）

### 3. 日程表API测试
**基础操作：**
- `POST /api/schedule` - 创建日程项
- `GET /api/schedule` - 获取日程列表
- `PUT /api/schedule/{id}` - 更新日程项
- `DELETE /api/schedule/{id}` - 删除日程项

**状态管理：**
- `POST /api/schedule/{id}/toggle-completion` - 切换完成状态
- `GET /api/schedule/upcoming` - 获取即将到来的日程
- `GET /api/schedule/overdue` - 获取逾期日程

**视图和统计：**
- `GET /api/schedule/calendar` - 获取日历视图
- `GET /api/schedule/statistics` - 获取日程统计
- `POST /api/schedule/activities/{activityId}` - 添加活动到日程
- `DELETE /api/schedule/activities/{activityId}` - 从日程移除活动

### 4. 推荐系统API测试
- `GET /api/recommendations` - 获取个性化推荐
- `GET /api/recommendations/collaborative` - 获取协同过滤推荐
- `GET /api/recommendations/content-based` - 获取基于内容的推荐
- `GET /api/recommendations/preferences` - 获取用户偏好
- `POST /api/recommendations/preferences` - 更新用户偏好
- `POST /api/recommendations/recalculate` - 重新计算推荐

## 测试账号

默认测试账号：
- 用户名：`admin`
- 密码：`admin123`

## 测试结果说明

- 🟢 **绿色**：API调用成功
- 🔴 **红色**：API调用失败
- 🟡 **黄色**：警告信息
- ⚪ **白色**：普通信息

## 使用流程

### 1. 测试公开API
1. 直接点击"🟢 公开API测试"区域中的任意按钮
2. 查看测试结果日志

### 2. 测试认证API
1. 在配置信息区域使用默认账号登录
2. 登录成功后，点击"🔐 认证API测试"区域中的按钮
3. 查看测试结果日志

### 3. 测试日程表API
1. 确保已登录
2. 点击"📅 日程表API测试"区域中的按钮
3. 建议按顺序测试：先创建日程项，再测试其他操作
4. 查看测试结果日志

### 4. 测试推荐系统API
1. 确保已登录
2. 点击"🎯 推荐系统API测试"区域中的按钮
3. 查看测试结果日志

## 常见问题

### 1. 连接失败
- 检查API服务器是否启动
- 检查API基础URL配置是否正确
- 检查网络连接

### 2. 认证失败
- 确保使用正确的用户名和密码
- 检查JWT配置是否正确
- 检查token是否过期

### 3. 权限不足
- 某些API需要特定角色权限（如教师、管理员）
- 确保测试账号具有相应权限

### 4. 日程表API测试注意事项
- 创建日程项后，系统会自动记录创建的ID
- 更新、删除、切换状态等操作需要先创建日程项
- 添加活动到日程需要先创建活动

## 技术实现

### 前端技术栈
- Blazor Server
- Bootstrap 5
- HttpClient

### 认证机制
- JWT Token
- LocalStorage存储
- Bearer Token认证

### 错误处理
- 统一的异常处理
- 详细的错误日志
- 用户友好的错误提示

### 状态管理
- 自动记录创建的实体ID
- 智能的API依赖关系处理
- 实时的测试状态反馈

## 扩展功能

如需添加新的API测试，可以：

1. 在对应的测试方法中添加新的API调用
2. 使用 `TestSingleApi` 方法进行单个API测试
3. 根据需要添加认证头设置
4. 在界面中添加对应的测试按钮

示例：
```csharp
// 添加单个API测试按钮
<button class="btn btn-outline-info" @onclick="() => TestSingleApi('GET', 'api/new-endpoint', '新API测试')" disabled="@(!isAuthenticated || isTesting)">
    新API测试
</button>

// 添加对应的测试方法
private async Task TestNewApi()
{
    await TestSingleApi("GET", "api/new-endpoint", "新API测试");
}
``` 