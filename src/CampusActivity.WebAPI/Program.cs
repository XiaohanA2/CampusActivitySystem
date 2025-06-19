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

// 配置Redis缓存
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

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

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins("https://localhost:7000", "http://localhost:5150", "https://localhost:44310", "http://localhost:50771")
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

app.UseHttpsRedirection();

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
}
