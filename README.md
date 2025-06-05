# FastService

[![NuGet Version](https://img.shields.io/nuget/v/FastService)](https://www.nuget.org/packages/FastService)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.0%20%7C%20.NET%209.0-blue)](https://dotnet.microsoft.com/)

> 🚀 **简单将Service映射MiniApis，快速简单应用！**

FastService 是一个轻量级的 .NET 库，通过源代码生成器自动将继承自 `FastApi` 的服务类映射为 ASP.NET Core Minimal APIs，大大简化了 API 开发流程。

## ✨ 特性

- 🔥 **零配置映射** - 继承 `FastApi` 基类即可自动生成 API 端点
- ⚡ **源代码生成** - 编译时生成代码，运行时零反射，性能卓越
- 🎯 **智能路由** - 根据方法名自动推断 HTTP 方法和路由
- 🛡️ **过滤器支持** - 内置过滤器机制，支持认证、授权等
- 📝 **OpenAPI 集成** - 完美支持 Swagger/OpenAPI 文档生成
- 🔧 **高度可配置** - 灵活的配置选项满足各种需求
- 📦 **轻量级** - 基于 .NET Standard 2.0，兼容性强

## 📦 安装

### Package Manager
```bash
Install-Package FastService
```

### .NET CLI
```bash
dotnet add package FastService
```

### PackageReference
```xml
<PackageReference Include="FastService" Version="0.1.4" />
```

## 🚀 快速开始

### 1. 创建服务类

```csharp
using FastService;

[Route("/api/users")]
[Tags("用户管理")]
public class UserService : FastApi
{
    [EndpointSummary("获取用户列表")]
    public async Task<List<User>> GetUsersAsync()
    {
        // 实现获取用户逻辑
        return await GetAllUsersAsync();
    }

    [EndpointSummary("创建用户")]
    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        // 实现创建用户逻辑
        return await CreateNewUserAsync(request);
    }

    [EndpointSummary("更新用户")]
    public async Task<User> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        // 实现更新用户逻辑
        return await UpdateExistingUserAsync(id, request);
    }

    [EndpointSummary("删除用户")]
    public async Task DeleteUserAsync(int id)
    {
        // 实现删除用户逻辑
        await DeleteExistingUserAsync(id);
    }
}
```

### 2. 注册服务

```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加必要的服务
builder.Services.AddOpenApi();

var app = builder.Build();

// 配置管道
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // 或使用 Swagger UI
}

// 🎯 关键步骤：映射 FastService APIs
app.MapFastApis();

app.Run();
```

### 3. 运行应用

启动应用后，FastService 会自动生成以下端点：

- `GET /api/users` - 获取用户列表
- `POST /api/users` - 创建用户
- `PUT /api/users/{id}` - 更新用户
- `DELETE /api/users/{id}` - 删除用户

## 🔧 高级配置

### 全局配置

```csharp
app.MapFastApis(options =>
{
    options.Prefix = "v1";                    // API 前缀
    options.Version = "1.0";                  // API 版本
    options.DisableAutoMapRoute = false;      // 禁用自动路由映射
    options.AutoAppendId = true;              // 自动追加 ID 参数
    options.PluralizeServiceName = true;      // 服务名复数化
    options.EnableProperty = false;           // 启用属性访问
    options.DisableTrimMethodPrefix = false;  // 禁用方法前缀修剪
});
```

### 自定义路由

```csharp
[Route("/api/v1/products")]
public class ProductService : FastApi
{
    [Route("search")]  // 自定义路由：GET /api/v1/products/search
    public async Task<List<Product>> SearchProductsAsync(string keyword)
    {
        // 搜索逻辑
    }
}
```

### 过滤器支持

```csharp
// 创建自定义过滤器
public class AuthenticationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, 
        EndpointFilterDelegate next)
    {
        // 认证逻辑
        if (!IsAuthenticated(context.HttpContext))
        {
            return Results.Unauthorized();
        }
        
        return await next(context);
    }
}

// 应用过滤器
[Filter(typeof(AuthenticationFilter))]
public class SecureService : FastApi
{
    public async Task<string> GetSecureDataAsync()
    {
        return "机密数据";
    }
}
```

### 忽略特定方法

```csharp
public class MyService : FastApi
{
    public async Task<string> PublicMethodAsync()
    {
        return "这个方法会被映射为 API";
    }

    [IgnoreRoute]  // 忽略此方法
    public async Task<string> InternalMethodAsync()
    {
        return "这个方法不会被映射";
    }
}
```

## 📋 HTTP 方法映射规则

FastService 根据方法名自动推断 HTTP 方法：

| 方法名前缀 | HTTP 方法 | 示例 |
|-----------|----------|------|
| `Get*` | GET | `GetUsersAsync()` → `GET /api/users` |
| `Create*`, `Add*`, `Post*` | POST | `CreateUserAsync()` → `POST /api/users` |
| `Update*`, `Edit*`, `Put*` | PUT | `UpdateUserAsync()` → `PUT /api/users` |
| `Delete*`, `Remove*` | DELETE | `DeleteUserAsync()` → `DELETE /api/users` |
| 其他 | POST | `ProcessDataAsync()` → `POST /api/process-data` |

## 🏗️ 项目结构

```
FastService/
├── src/
│   ├── FastService/                 # 核心库
│   │   ├── FastApi.cs              # 基类
│   │   ├── RouteAttribute.cs       # 路由特性
│   │   ├── FilterAttribute.cs      # 过滤器特性
│   │   └── FastOption.cs           # 配置选项
│   ├── FastService.Analyzers/       # 源代码生成器
│   │   └── FastGenerator.cs        # 主要生成器
│   ├── Fast.Application/            # 示例应用层
│   └── Fast.Test/                   # 测试项目
├── Directory.Build.props            # 构建属性
└── FastService.sln                  # 解决方案文件
```

## 🤝 贡献

欢迎贡献代码！请遵循以下步骤：

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🙏 致谢

- 感谢 ASP.NET Core 团队提供的优秀框架
- 感谢所有贡献者的支持

## 📞 联系方式

- **作者**: Token
- **组织**: AIDotNet
- **仓库**: [https://github.com/AIDotNet/FastService](https://github.com/AIDotNet/FastService)
- **问题反馈**: [GitHub Issues](https://github.com/AIDotNet/FastService/issues)

---

⭐ 如果这个项目对你有帮助，请给个 Star 支持一下！