using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using CampusActivity.Infrastructure.Data;
using CampusActivity.Infrastructure.UnitOfWork;
using CampusActivity.Application.Services;
using CampusActivity.Application.Mappings;
using CampusActivity.Shared.Constants;
using CampusActivity.Shared.DTOs;
using CampusActivity.Infrastructure.Repositories;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// 配置服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 配置Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "校园活动管理系统 API", 
        Version = "v1",
        Description = "校园活动管理系统的RESTful API"
    });
    
    // JWT认证配置
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 配置数据库
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// 配置缓存（使用内存缓存）
builder.Services.AddDistributedMemoryCache();

// 配置JWT认证
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "DefaultSecretKeyForDevelopment";
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 配置授权策略
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppConstants.Policies.RequireStudentRole, 
        policy => policy.RequireRole(AppConstants.Roles.Student));
    options.AddPolicy(AppConstants.Policies.RequireTeacherRole, 
        policy => policy.RequireRole(AppConstants.Roles.Teacher));
    options.AddPolicy(AppConstants.Policies.RequireAdminRole, 
        policy => policy.RequireRole(AppConstants.Roles.Admin));
    options.AddPolicy(AppConstants.Policies.RequireStaffRole, 
        policy => policy.RequireRole(AppConstants.Roles.Teacher, AppConstants.Roles.Admin));
});

// 配置AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// 注册应用服务
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();

// 注册Repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins("http://localhost:7150", "http://localhost:5150", "http://localhost:50771")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 配置日志
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// 配置HTTP客户端
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // 设置30秒超时
    client.DefaultRequestHeaders.Add("User-Agent", "CampusActivitySystem/1.0");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// 配置HTTP客户端工厂
builder.Services.AddHttpClient();

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "校园活动管理系统 API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowBlazorApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 确保数据库已创建
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
        
        // 可以在这里添加种子数据
        await SeedData(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "数据库初始化失败");
    }
}

app.Run();

// 种子数据方法
static async Task SeedData(ApplicationDbContext context)
{
    // 添加默认活动分类
    if (!context.ActivityCategories.Any())
    {
        var categories = new[]
        {
            new CampusActivity.Domain.Entities.ActivityCategory { Name = "学术讲座", Description = "学术研究、专业知识分享", IsActive = true, SortOrder = 1 },
            new CampusActivity.Domain.Entities.ActivityCategory { Name = "文艺演出", Description = "音乐、舞蹈、戏剧等文艺活动", IsActive = true, SortOrder = 2 },
            new CampusActivity.Domain.Entities.ActivityCategory { Name = "体育竞技", Description = "各类体育比赛和健身活动", IsActive = true, SortOrder = 3 },
            new CampusActivity.Domain.Entities.ActivityCategory { Name = "社会实践", Description = "志愿服务、社会调研等实践活动", IsActive = true, SortOrder = 4 },
            new CampusActivity.Domain.Entities.ActivityCategory { Name = "创新创业", Description = "创业大赛、创新项目等", IsActive = true, SortOrder = 5 },
            new CampusActivity.Domain.Entities.ActivityCategory { Name = "交流参观", Description = "企业参观、学术交流等", IsActive = true, SortOrder = 6 }
        };
        
        context.ActivityCategories.AddRange(categories);
        await context.SaveChangesAsync();
    }
    
    // 添加默认管理员用户
    if (!context.Users.Any(u => u.Role == CampusActivity.Shared.DTOs.UserRole.Admin))
    {
        var adminUser = new CampusActivity.Domain.Entities.User
        {
            Username = "admin",
            Email = "admin@campus.edu",
            FullName = "系统管理员",
            Role = CampusActivity.Shared.DTOs.UserRole.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
    }

    // 添加示例活动数据
    if (!context.Activities.Any())
    {
        var categories = context.ActivityCategories.ToList();
        var adminUser = context.Users.FirstOrDefault(u => u.Role == CampusActivity.Shared.DTOs.UserRole.Admin);
        
        if (categories.Any() && adminUser != null)
        {
            var activities = new[]
            {
                new CampusActivity.Domain.Entities.Activity
                {
                    Title = "人工智能技术讲座",
                    Description = "探讨AI技术的最新发展和应用前景，邀请业内专家分享经验。",
                    Location = "学术报告厅A101",
                    StartTime = DateTime.UtcNow.AddDays(7),
                    EndTime = DateTime.UtcNow.AddDays(7).AddHours(2),
                    RegistrationDeadline = DateTime.UtcNow.AddDays(5),
                    MaxParticipants = 100,
                    CurrentParticipants = 35,
                    Status = CampusActivity.Shared.DTOs.ActivityStatus.Published,
                    CategoryId = categories.First(c => c.Name == "学术讲座").Id,
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CampusActivity.Domain.Entities.Activity
                {
                    Title = "校园歌手大赛",
                    Description = "一年一度的校园歌手大赛，展示你的音乐才华！",
                    Location = "学生活动中心",
                    StartTime = DateTime.UtcNow.AddDays(14),
                    EndTime = DateTime.UtcNow.AddDays(14).AddHours(3),
                    RegistrationDeadline = DateTime.UtcNow.AddDays(10),
                    MaxParticipants = 50,
                    CurrentParticipants = 28,
                    Status = CampusActivity.Shared.DTOs.ActivityStatus.Published,
                    CategoryId = categories.First(c => c.Name == "文艺演出").Id,
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CampusActivity.Domain.Entities.Activity
                {
                    Title = "篮球友谊赛",
                    Description = "院系间篮球友谊赛，增进同学间的友谊。",
                    Location = "体育馆",
                    StartTime = DateTime.UtcNow.AddDays(3),
                    EndTime = DateTime.UtcNow.AddDays(3).AddHours(2),
                    RegistrationDeadline = DateTime.UtcNow.AddDays(1),
                    MaxParticipants = 20,
                    CurrentParticipants = 12,
                    Status = CampusActivity.Shared.DTOs.ActivityStatus.Published,
                    CategoryId = categories.First(c => c.Name == "体育竞技").Id,
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CampusActivity.Domain.Entities.Activity
                {
                    Title = "社区志愿服务",
                    Description = "走进社区，为老人提供志愿服务，传递爱心。",
                    Location = "幸福社区",
                    StartTime = DateTime.UtcNow.AddDays(5),
                    EndTime = DateTime.UtcNow.AddDays(5).AddHours(4),
                    RegistrationDeadline = DateTime.UtcNow.AddDays(3),
                    MaxParticipants = 30,
                    CurrentParticipants = 15,
                    Status = CampusActivity.Shared.DTOs.ActivityStatus.Published,
                    CategoryId = categories.First(c => c.Name == "社会实践").Id,
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CampusActivity.Domain.Entities.Activity
                {
                    Title = "创新创业大赛",
                    Description = "展示你的创新项目，获得投资机会和创业指导。",
                    Location = "创业孵化中心",
                    StartTime = DateTime.UtcNow.AddDays(21),
                    EndTime = DateTime.UtcNow.AddDays(21).AddHours(6),
                    RegistrationDeadline = DateTime.UtcNow.AddDays(15),
                    MaxParticipants = 40,
                    CurrentParticipants = 22,
                    Status = CampusActivity.Shared.DTOs.ActivityStatus.Published,
                    CategoryId = categories.First(c => c.Name == "创新创业").Id,
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CampusActivity.Domain.Entities.Activity
                {
                    Title = "企业参观活动",
                    Description = "参观知名企业，了解企业文化和工作环境。",
                    Location = "科技园区",
                    StartTime = DateTime.UtcNow.AddDays(10),
                    EndTime = DateTime.UtcNow.AddDays(10).AddHours(3),
                    RegistrationDeadline = DateTime.UtcNow.AddDays(7),
                    MaxParticipants = 25,
                    CurrentParticipants = 18,
                    Status = CampusActivity.Shared.DTOs.ActivityStatus.Published,
                    CategoryId = categories.First(c => c.Name == "交流参观").Id,
                    CreatedBy = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
            
            context.Activities.AddRange(activities);
            await context.SaveChangesAsync();
        }
    }

    // 添加一些测试用户
    if (!context.Users.Any(u => u.Role != CampusActivity.Shared.DTOs.UserRole.Admin))
    {
        var testUsers = new[]
        {
            new CampusActivity.Domain.Entities.User
            {
                Username = "student1",
                Email = "student1@campus.edu",
                FullName = "张三",
                Role = CampusActivity.Shared.DTOs.UserRole.Student,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                IsActive = true,
                StudentId = "2021001",
                Major = "计算机科学与技术",
                CreatedAt = DateTime.UtcNow
            },
            new CampusActivity.Domain.Entities.User
            {
                Username = "student2",
                Email = "student2@campus.edu",
                FullName = "李四",
                Role = CampusActivity.Shared.DTOs.UserRole.Student,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                IsActive = true,
                StudentId = "2021002",
                Major = "软件工程",
                CreatedAt = DateTime.UtcNow
            },
            new CampusActivity.Domain.Entities.User
            {
                Username = "teacher1",
                Email = "teacher1@campus.edu",
                FullName = "王老师",
                Role = CampusActivity.Shared.DTOs.UserRole.Teacher,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                IsActive = true,
                EmployeeId = "T001",
                Department = "计算机学院",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(testUsers);
        await context.SaveChangesAsync();

        // 添加一些用户偏好设置
        var categories = context.ActivityCategories.ToList();
        var student1 = testUsers[0];
        var student2 = testUsers[1];

        var preferences = new[]
        {
            // 学生1偏好学术讲座和创新创业
            new CampusActivity.Domain.Entities.UserActivityPreference
            {
                UserId = student1.Id,
                CategoryId = categories.First(c => c.Name == "学术讲座").Id,
                Weight = 0.8,
                LastUpdated = DateTime.UtcNow
            },
            new CampusActivity.Domain.Entities.UserActivityPreference
            {
                UserId = student1.Id,
                CategoryId = categories.First(c => c.Name == "创新创业").Id,
                Weight = 0.7,
                LastUpdated = DateTime.UtcNow
            },
            // 学生2偏好文艺演出和体育竞技
            new CampusActivity.Domain.Entities.UserActivityPreference
            {
                UserId = student2.Id,
                CategoryId = categories.First(c => c.Name == "文艺演出").Id,
                Weight = 0.9,
                LastUpdated = DateTime.UtcNow
            },
            new CampusActivity.Domain.Entities.UserActivityPreference
            {
                UserId = student2.Id,
                CategoryId = categories.First(c => c.Name == "体育竞技").Id,
                Weight = 0.6,
                LastUpdated = DateTime.UtcNow
            }
        };

        context.UserActivityPreferences.AddRange(preferences);
        await context.SaveChangesAsync();
    }
}

// 重试策略
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                // 记录重试信息
                Console.WriteLine($"OpenAI API调用失败，正在进行第{retryAttempt}次重试，等待{timespan.TotalMilliseconds}ms");
            });
}

// 熔断器策略
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
            onBreak: (outcome, timespan) =>
            {
                // 记录熔断器开启信息
                Console.WriteLine($"OpenAI API熔断器开启，将在{timespan.TotalMilliseconds}ms后重试");
            },
            onReset: () =>
            {
                // 记录熔断器重置信息
                Console.WriteLine("OpenAI API熔断器重置");
            });
}
