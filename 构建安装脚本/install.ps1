# ==============================================================================
# 校园活动管理系统 - 安装脚本
# Campus Activity Management System - Installation Script
# ==============================================================================
# 作者: 开发团队
# 版本: 1.0.0
# 描述: 自动化安装和配置校园活动管理系统
# 使用方法: .\install.ps1 [-Environment] [-Force] [-Help]
# ==============================================================================

param(
    [Parameter()]
    [ValidateSet("Development", "Production", "Testing")]
    [string]$Environment = "Development",
    
    [Parameter()]
    [switch]$Force,
    
    [Parameter()]
    [switch]$SkipDatabase,
    
    [Parameter()]
    [switch]$Help
)

# 显示帮助信息
if ($Help) {
    Write-Host "校园活动管理系统 - 安装脚本" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "用法:" -ForegroundColor Yellow
    Write-Host "  .\install.ps1 [Options]" -ForegroundColor White
    Write-Host ""
    Write-Host "选项:" -ForegroundColor Yellow
    Write-Host "  -Environment    目标环境 (Development, Production, Testing)" -ForegroundColor White
    Write-Host "  -Force          强制重新安装，覆盖现有配置" -ForegroundColor White
    Write-Host "  -SkipDatabase   跳过数据库初始化" -ForegroundColor White
    Write-Host "  -Help           显示此帮助信息" -ForegroundColor White
    Write-Host ""
    Write-Host "示例:" -ForegroundColor Yellow
    Write-Host "  .\install.ps1                           # 开发环境安装" -ForegroundColor White
    Write-Host "  .\install.ps1 -Environment Production   # 生产环境安装" -ForegroundColor White
    Write-Host "  .\install.ps1 -Force                    # 强制重新安装" -ForegroundColor White
    exit 0
}

# 脚本配置
$ErrorActionPreference = "Stop"
$InstallStartTime = Get-Date

# 颜色输出函数
function Write-Info($message) { Write-Host "ℹ️  $message" -ForegroundColor Cyan }
function Write-Success($message) { Write-Host "✅ $message" -ForegroundColor Green }
function Write-Warning($message) { Write-Host "⚠️  $message" -ForegroundColor Yellow }
function Write-Error($message) { Write-Host "❌ $message" -ForegroundColor Red }
function Write-Step($message) { Write-Host "🔄 $message" -ForegroundColor Blue }

# 横幅
function Show-Banner {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                   校园活动管理系统 - 安装脚本                     ║" -ForegroundColor Cyan
    Write-Host "║                Campus Activity Management System                 ║" -ForegroundColor Cyan
    Write-Host "║                        Installation Script                      ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Info "目标环境: $Environment"
    Write-Info "开始时间: $($InstallStartTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    Write-Host ""
}

# 检查是否以管理员身份运行
function Test-AdminPrivileges {
    $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object System.Security.Principal.WindowsPrincipal($currentUser)
    $isAdmin = $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-not $isAdmin) {
        Write-Warning "建议以管理员身份运行此脚本以避免权限问题"
        $response = Read-Host "是否继续? (y/N)"
        if ($response -ne "y" -and $response -ne "Y") {
            Write-Info "安装已取消"
            exit 0
        }
    } else {
        Write-Success "已以管理员身份运行"
    }
}

# 检查系统环境
function Test-SystemRequirements {
    Write-Step "检查系统要求..."
    
    # 检查操作系统
    $osInfo = Get-WmiObject -Class Win32_OperatingSystem
    Write-Success "操作系统: $($osInfo.Caption) $($osInfo.Version)"
    
    # 检查.NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Success ".NET SDK: $dotnetVersion"
        
        # 检查是否为.NET 8
        if ($dotnetVersion -match "^8\.") {
            Write-Success ".NET 8 SDK 已安装"
        } else {
            Write-Warning ".NET 8 SDK 未安装，当前版本: $dotnetVersion"
            Write-Info "请从 https://dotnet.microsoft.com/download/dotnet/8.0 下载安装"
        }
    }
    catch {
        Write-Error ".NET SDK 未安装"
        Write-Info "请从 https://dotnet.microsoft.com/download 下载安装 .NET 8 SDK"
        exit 1
    }
    
    # 检查内存
    $memory = Get-WmiObject -Class Win32_ComputerSystem
    $memoryGB = [math]::Round($memory.TotalPhysicalMemory / 1GB, 2)
    Write-Success "系统内存: $memoryGB GB"
    
    if ($memoryGB -lt 4) {
        Write-Warning "建议至少4GB内存以获得最佳性能"
    }
    
    # 检查磁盘空间
    $disk = Get-WmiObject -Class Win32_LogicalDisk | Where-Object { $_.DeviceID -eq "C:" }
    $freeSpaceGB = [math]::Round($disk.FreeSpace / 1GB, 2)
    Write-Success "C盘可用空间: $freeSpaceGB GB"
    
    if ($freeSpaceGB -lt 5) {
        Write-Warning "建议至少5GB可用磁盘空间"
    }
    
    Write-Host ""
}

# 安装必需软件
function Install-Prerequisites {
    Write-Step "检查并安装必需软件..."
    
    # 检查Chocolatey
    if (!(Get-Command choco -ErrorAction SilentlyContinue)) {
        Write-Info "安装 Chocolatey 包管理器..."
        try {
            Set-ExecutionPolicy Bypass -Scope Process -Force
            [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
            Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
            Write-Success "Chocolatey 安装完成"
        }
        catch {
            Write-Warning "Chocolatey 自动安装失败，请手动安装"
        }
    } else {
        Write-Success "Chocolatey 已安装"
    }
    
    # MySQL相关说明
    Write-Info "数据库要求:"
    Write-Host "  - MySQL 8.0+ 或 MariaDB 10.6+" -ForegroundColor Yellow
    Write-Host "  - 建议使用云数据库服务 (如阿里云RDS、腾讯云CDB)" -ForegroundColor Yellow
    Write-Host "  - 本地开发可使用 Docker: docker run -d --name mysql -p 3306:3306 -e MYSQL_ROOT_PASSWORD=password mysql:8.0" -ForegroundColor Yellow
    
    # Redis相关说明
    Write-Info "缓存要求:"
    Write-Host "  - Redis 6.0+ (可选，系统会降级到内存缓存)" -ForegroundColor Yellow
    Write-Host "  - 建议使用云Redis服务 (如阿里云Redis、腾讯云Redis)" -ForegroundColor Yellow
    Write-Host "  - 本地开发可使用 Docker: docker run -d --name redis -p 6379:6379 redis:7-alpine" -ForegroundColor Yellow
    
    Write-Host ""
}

# 创建项目目录结构
function Initialize-ProjectStructure {
    Write-Step "初始化项目目录结构..."
    
    $directories = @(
        "logs",
        "config",
        "data",
        "backups",
        "temp"
    )
    
    foreach ($dir in $directories) {
        if (!(Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Success "创建目录: $dir"
        } else {
            Write-Info "目录已存在: $dir"
        }
    }
    
    Write-Host ""
}

# 配置应用程序设置
function Configure-Application {
    Write-Step "配置应用程序设置..."
    
    # WebAPI配置
    $webApiConfigPath = "src/CampusActivity.WebAPI/appsettings.$Environment.json"
    $webApiConfig = @{
        "Logging" = @{
            "LogLevel" = @{
                "Default" = if ($Environment -eq "Production") { "Warning" } else { "Information" }
                "Microsoft.AspNetCore" = "Warning"
                "Microsoft.EntityFrameworkCore" = if ($Environment -eq "Development") { "Information" } else { "Warning" }
            }
        }
        "AllowedHosts" = "*"
        "ConnectionStrings" = @{
            "DefaultConnection" = "Server=localhost;Port=3306;Database=CampusActivityDB;User=root;Password=your_password_here;"
            "Redis" = "localhost:6379"
        }
        "JwtSettings" = @{
            "SecretKey" = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((New-Guid).ToString() + (New-Guid).ToString()))
            "Issuer" = "CampusActivitySystem"
            "Audience" = "CampusActivityUsers"
            "ExpirationHours" = if ($Environment -eq "Production") { 8 } else { 24 }
        }
        "AiSettings" = @{
            "ApiUrl" = "https://api.openai.com/v1/chat/completions"
            "ApiKey" = "your_openai_api_key_here"
            "Model" = "gpt-3.5-turbo"
            "TimeoutSeconds" = 30
            "MaxRetries" = 3
            "CircuitBreakerThreshold" = 5
        }
    }
    
    # BlazorWeb配置
    $blazorConfigPath = "src/CampusActivity.BlazorWeb/appsettings.$Environment.json"
    $blazorConfig = @{
        "Logging" = @{
            "LogLevel" = @{
                "Default" = if ($Environment -eq "Production") { "Warning" } else { "Information" }
                "Microsoft.AspNetCore" = "Warning"
            }
        }
        "AllowedHosts" = "*"
        "ApiSettings" = @{
            "BaseUrl" = if ($Environment -eq "Production") { "https://your-api-domain.com/" } else { "http://localhost:7186/" }
        }
    }
    
    # 保存配置文件
    try {
        $webApiConfig | ConvertTo-Json -Depth 10 | Set-Content -Path $webApiConfigPath -Encoding UTF8
        Write-Success "WebAPI 配置已保存: $webApiConfigPath"
        
        $blazorConfig | ConvertTo-Json -Depth 10 | Set-Content -Path $blazorConfigPath -Encoding UTF8
        Write-Success "BlazorWeb 配置已保存: $blazorConfigPath"
    }
    catch {
        Write-Error "配置文件保存失败: $($_.Exception.Message)"
    }
    
    Write-Host ""
}

# 数据库初始化
function Initialize-Database {
    if ($SkipDatabase) {
        Write-Info "跳过数据库初始化 (-SkipDatabase)"
        return
    }
    
    Write-Step "数据库初始化..."
    
    # 检查数据库连接
    Write-Info "请确保数据库服务已启动并可访问"
    
    $connectionString = Read-Host "请输入数据库连接字符串 (直接回车使用默认配置)"
    
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        $connectionString = "Server=localhost;Port=3306;Database=CampusActivityDB;User=root;Password=password;"
        Write-Info "使用默认连接字符串: $connectionString"
    }
    
    # 更新配置文件中的连接字符串
    try {
        $configFiles = @(
            "src/CampusActivity.WebAPI/appsettings.$Environment.json",
            "src/CampusActivity.WebAPI/appsettings.json"
        )
        
        foreach ($configFile in $configFiles) {
            if (Test-Path $configFile) {
                $config = Get-Content $configFile | ConvertFrom-Json
                $config.ConnectionStrings.DefaultConnection = $connectionString
                $config | ConvertTo-Json -Depth 10 | Set-Content $configFile -Encoding UTF8
                Write-Success "已更新配置文件: $configFile"
            }
        }
    }
    catch {
        Write-Warning "更新配置文件失败: $($_.Exception.Message)"
    }
    
    # 注意：由于项目使用 EnsureCreatedAsync，数据库会自动创建
    Write-Info "数据库将在首次启动时自动创建和初始化"
    
    Write-Host ""
}

# 构建项目
function Build-Project {
    Write-Step "构建项目..."
    
    try {
        if (Test-Path "build.ps1") {
            Write-Info "使用构建脚本进行构建..."
            & ".\build.ps1" -Configuration Release
        } else {
            Write-Info "直接构建解决方案..."
            dotnet build CampusActivitySystem.sln --configuration Release
        }
        Write-Success "项目构建完成"
    }
    catch {
        Write-Error "项目构建失败: $($_.Exception.Message)"
        exit 1
    }
    
    Write-Host ""
}

# 创建服务脚本
function Create-ServiceScripts {
    Write-Step "创建服务管理脚本..."
    
    # 启动脚本
    $startScript = @"
# 启动校园活动管理系统
Write-Host "启动校园活动管理系统..." -ForegroundColor Green

# 启动WebAPI
Write-Host "启动WebAPI服务..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'src/CampusActivity.WebAPI'; dotnet run"

# 等待API启动
Start-Sleep -Seconds 5

# 启动BlazorWeb
Write-Host "启动Web界面..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'src/CampusActivity.BlazorWeb'; dotnet run"

Write-Host "系统启动完成!" -ForegroundColor Green
Write-Host "WebAPI: http://localhost:7186" -ForegroundColor Cyan
Write-Host "Web界面: http://localhost:7150" -ForegroundColor Cyan
"@

    # 停止脚本
    $stopScript = @"
# 停止校园活动管理系统
Write-Host "停止校园活动管理系统..." -ForegroundColor Red

# 查找并停止相关进程
Get-Process | Where-Object { `$_.ProcessName -like "*CampusActivity*" -or (`$_.CommandLine -like "*CampusActivity*") } | Stop-Process -Force

Write-Host "系统已停止" -ForegroundColor Green
"@

    # 保存脚本
    $startScript | Set-Content -Path "start.ps1" -Encoding UTF8
    $stopScript | Set-Content -Path "stop.ps1" -Encoding UTF8
    
    Write-Success "服务脚本已创建: start.ps1, stop.ps1"
    Write-Host ""
}

# 创建部署文档
function Create-DeploymentGuide {
    Write-Step "创建部署文档..."
    
    $deploymentGuide = @"
# 校园活动管理系统 - 部署指南

## 系统要求
- Windows 10/11 或 Windows Server 2016+
- .NET 8.0 SDK
- MySQL 8.0+ 或 MariaDB 10.6+
- Redis 6.0+ (可选)
- 至少 4GB 内存
- 至少 5GB 可用磁盘空间

## 快速开始
1. 运行 ``.\install.ps1`` 进行初始安装
2. 配置数据库连接字符串
3. 运行 ``.\start.ps1`` 启动系统

## 配置文件
- WebAPI配置: ``src/CampusActivity.WebAPI/appsettings.$Environment.json``
- Web配置: ``src/CampusActivity.BlazorWeb/appsettings.$Environment.json``

## 服务地址
- WebAPI: http://localhost:7186
- Web界面: http://localhost:7150
- API文档: http://localhost:7186/swagger

## 默认账户
- 管理员: admin / admin123
- 学生: student1 / 123456
- 教师: teacher1 / 123456

## 日志文件
- 位置: logs/ 目录
- 级别: $Environment 环境为 $(if ($Environment -eq "Production") { "Warning" } else { "Information" })

## 数据库备份
建议定期备份 CampusActivityDB 数据库

## 故障排除
1. 检查端口是否被占用: ``netstat -ano | findstr ":7186"``
2. 检查数据库连接
3. 查看日志文件
4. 重启服务: ``.\stop.ps1`` 然后 ``.\start.ps1``

## 技术支持
如需技术支持，请联系开发团队。
"@

    $deploymentGuide | Set-Content -Path "部署指南.md" -Encoding UTF8
    Write-Success "部署文档已创建: 部署指南.md"
    Write-Host ""
}

# 显示安装摘要
function Show-InstallationSummary {
    $installEndTime = Get-Date
    $installDuration = $installEndTime - $InstallStartTime
    
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║                         安装完成                                 ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    Write-Success "目标环境: $Environment"
    Write-Success "完成时间: $($installEndTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    Write-Success "安装耗时: $($installDuration.ToString('mm\:ss'))"
    Write-Host ""
    
    Write-Info "接下来的步骤:"
    Write-Host "  1. 配置数据库连接字符串" -ForegroundColor Yellow
    Write-Host "     编辑: src/CampusActivity.WebAPI/appsettings.$Environment.json" -ForegroundColor White
    Write-Host ""
    Write-Host "  2. 启动系统" -ForegroundColor Yellow
    Write-Host "     运行: .\start.ps1" -ForegroundColor White
    Write-Host ""
    Write-Host "  3. 访问系统" -ForegroundColor Yellow
    Write-Host "     Web界面: http://localhost:7150" -ForegroundColor White
    Write-Host "     API文档: http://localhost:7186/swagger" -ForegroundColor White
    Write-Host ""
    Write-Host "  4. 默认管理员账户" -ForegroundColor Yellow
    Write-Host "     用户名: admin" -ForegroundColor White
    Write-Host "     密码: admin123" -ForegroundColor White
    Write-Host ""
    
    Write-Info "管理脚本:"
    Write-Host "  启动系统: .\start.ps1" -ForegroundColor Cyan
    Write-Host "  停止系统: .\stop.ps1" -ForegroundColor Cyan
    Write-Host "  重新构建: .\build.ps1" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Info "文档:"
    Write-Host "  部署指南: 部署指南.md" -ForegroundColor Cyan
    Write-Host "  API文档: docs/ 目录" -ForegroundColor Cyan
    Write-Host ""
}

# 错误处理
trap {
    Write-Error "安装过程中发生错误: $($_.Exception.Message)"
    Write-Host "错误位置: $($_.InvocationInfo.ScriptLineNumber) 行" -ForegroundColor Red
    Write-Warning "如需帮助，请查看 部署指南.md 或联系技术支持"
    exit 1
}

# 主执行流程
try {
    Show-Banner
    Test-AdminPrivileges
    Test-SystemRequirements
    Install-Prerequisites
    Initialize-ProjectStructure
    Configure-Application
    Initialize-Database
    Build-Project
    Create-ServiceScripts
    Create-DeploymentGuide
    Show-InstallationSummary
}
catch {
    Write-Error "安装失败: $($_.Exception.Message)"
    exit 1
}

Write-Success "校园活动管理系统安装完成! 🎉"
exit 0 