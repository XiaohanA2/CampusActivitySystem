# ==============================================================================
# 校园活动管理系统 - 构建脚本
# Campus Activity Management System - Build Script
# ==============================================================================
# 作者: 开发团队
# 版本: 1.0.0
# 描述: 自动化构建整个校园活动管理系统
# 使用方法: .\build.ps1 [Release|Debug] [-Clean] [-NoBuild] [-Help]
# ==============================================================================

param(
    [Parameter(Position=0)]
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    
    [Parameter()]
    [switch]$Clean,
    
    [Parameter()]
    [switch]$NoBuild,
    
    [Parameter()]
    [switch]$Help
)

# 显示帮助信息
if ($Help) {
    Write-Host "校园活动管理系统 - 构建脚本" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "用法:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1 [Configuration] [Options]" -ForegroundColor White
    Write-Host ""
    Write-Host "配置:" -ForegroundColor Yellow
    Write-Host "  Release    发布版本构建 (默认)" -ForegroundColor White
    Write-Host "  Debug      调试版本构建" -ForegroundColor White
    Write-Host ""
    Write-Host "选项:" -ForegroundColor Yellow
    Write-Host "  -Clean     清理之前的构建输出" -ForegroundColor White
    Write-Host "  -NoBuild   只恢复包，不进行构建" -ForegroundColor White
    Write-Host "  -Help      显示此帮助信息" -ForegroundColor White
    Write-Host ""
    Write-Host "示例:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1                    # 默认Release构建" -ForegroundColor White
    Write-Host "  .\build.ps1 Debug              # Debug构建" -ForegroundColor White
    Write-Host "  .\build.ps1 Release -Clean     # 清理后构建" -ForegroundColor White
    exit 0
}

# 脚本配置
$SolutionFile = "CampusActivitySystem.sln"
$ErrorActionPreference = "Stop"
$BuildStartTime = Get-Date

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
    Write-Host "║                   校园活动管理系统 - 构建脚本                     ║" -ForegroundColor Cyan
    Write-Host "║                Campus Activity Management System                 ║" -ForegroundColor Cyan
    Write-Host "║                          Build Script                           ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Info "构建配置: $Configuration"
    Write-Info "开始时间: $($BuildStartTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    Write-Host ""
}

# 检查必需工具
function Test-Prerequisites {
    Write-Step "检查构建环境..."
    
    # 检查.NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Success ".NET SDK: $dotnetVersion"
    }
    catch {
        Write-Error ".NET SDK 未安装或不在PATH中"
        Write-Host "请访问 https://dotnet.microsoft.com/download 下载安装" -ForegroundColor Yellow
        exit 1
    }
    
    # 检查解决方案文件
    if (!(Test-Path $SolutionFile)) {
        Write-Error "解决方案文件不存在: $SolutionFile"
        exit 1
    }
    Write-Success "解决方案文件: $SolutionFile"
    
    # 检查项目结构
    $projects = @(
        "src/CampusActivity.Domain/CampusActivity.Domain.csproj",
        "src/CampusActivity.Shared/CampusActivity.Shared.csproj",
        "src/CampusActivity.Infrastructure/CampusActivity.Infrastructure.csproj",
        "src/CampusActivity.Application/CampusActivity.Application.csproj",
        "src/CampusActivity.WebAPI/CampusActivity.WebAPI.csproj",
        "src/CampusActivity.BlazorWeb/CampusActivity.BlazorWeb.csproj"
    )
    
    foreach ($project in $projects) {
        if (Test-Path $project) {
            Write-Success "项目: $(Split-Path $project -Leaf)"
        } else {
            Write-Warning "项目缺失: $project"
        }
    }
    
    Write-Host ""
}

# 清理构建输出
function Invoke-Clean {
    if ($Clean) {
        Write-Step "清理构建输出..."
        
        try {
            # 清理解决方案
            dotnet clean $SolutionFile --configuration $Configuration --verbosity minimal
            
            # 清理bin和obj目录
            Get-ChildItem -Path . -Recurse -Directory -Name "bin", "obj" | ForEach-Object {
                $path = Join-Path $_.Directory.FullName $_.Name
                if (Test-Path $path) {
                    Remove-Item $path -Recurse -Force
                    Write-Success "已清理: $path"
                }
            }
            
            # 清理publish目录
            if (Test-Path "publish") {
                Remove-Item "publish" -Recurse -Force
                Write-Success "已清理: publish"
            }
            
            Write-Success "清理完成"
        }
        catch {
            Write-Error "清理失败: $($_.Exception.Message)"
            exit 1
        }
        
        Write-Host ""
    }
}

# 恢复NuGet包
function Restore-Packages {
    Write-Step "恢复NuGet包..."
    
    try {
        dotnet restore $SolutionFile --verbosity minimal
        Write-Success "包恢复完成"
    }
    catch {
        Write-Error "包恢复失败: $($_.Exception.Message)"
        exit 1
    }
    
    Write-Host ""
}

# 构建解决方案
function Build-Solution {
    if ($NoBuild) {
        Write-Info "跳过构建 (-NoBuild)"
        return
    }
    
    Write-Step "构建解决方案 ($Configuration)..."
    
    try {
        dotnet build $SolutionFile --configuration $Configuration --no-restore --verbosity minimal
        Write-Success "构建完成"
    }
    catch {
        Write-Error "构建失败: $($_.Exception.Message)"
        exit 1
    }
    
    Write-Host ""
}

# 运行测试
function Run-Tests {
    Write-Step "查找测试项目..."
    
    $testProjects = Get-ChildItem -Path . -Recurse -Name "*.Tests.csproj"
    
    if ($testProjects.Count -eq 0) {
        Write-Info "未找到测试项目，跳过测试"
    } else {
        Write-Info "找到 $($testProjects.Count) 个测试项目"
        foreach ($testProject in $testProjects) {
            Write-Step "运行测试: $testProject"
            try {
                dotnet test $testProject --configuration $Configuration --no-build --verbosity minimal
                Write-Success "测试通过: $testProject"
            }
            catch {
                Write-Warning "测试失败: $testProject"
            }
        }
    }
    
    Write-Host ""
}

# 创建发布包
function Create-PublishPackage {
    if ($Configuration -eq "Release") {
        Write-Step "创建发布包..."
        
        $publishDir = "publish"
        if (!(Test-Path $publishDir)) {
            New-Item -ItemType Directory -Path $publishDir | Out-Null
        }
        
        try {
            # 发布WebAPI
            Write-Info "发布 WebAPI..."
            dotnet publish "src/CampusActivity.WebAPI/CampusActivity.WebAPI.csproj" `
                --configuration Release `
                --output "$publishDir/WebAPI" `
                --no-restore `
                --verbosity minimal
            
            # 发布BlazorWeb
            Write-Info "发布 BlazorWeb..."
            dotnet publish "src/CampusActivity.BlazorWeb/CampusActivity.BlazorWeb.csproj" `
                --configuration Release `
                --output "$publishDir/BlazorWeb" `
                --no-restore `
                --verbosity minimal
            
            # 复制部署脚本
            Copy-Item "deploy.ps1" "$publishDir/" -ErrorAction SilentlyContinue
            Copy-Item "install.ps1" "$publishDir/" -ErrorAction SilentlyContinue
            Copy-Item "docs" "$publishDir/" -Recurse -ErrorAction SilentlyContinue
            
            Write-Success "发布包创建完成: $publishDir"
        }
        catch {
            Write-Error "发布失败: $($_.Exception.Message)"
            exit 1
        }
        
        Write-Host ""
    }
}

# 显示构建摘要
function Show-BuildSummary {
    $buildEndTime = Get-Date
    $buildDuration = $buildEndTime - $BuildStartTime
    
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║                         构建完成                                 ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    Write-Success "构建配置: $Configuration"
    Write-Success "完成时间: $($buildEndTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    Write-Success "构建耗时: $($buildDuration.ToString('mm\:ss'))"
    Write-Host ""
    
    Write-Info "项目组成:"
    Write-Host "  📦 CampusActivity.Domain       - 领域模型层" -ForegroundColor White
    Write-Host "  📦 CampusActivity.Shared       - 共享库" -ForegroundColor White
    Write-Host "  📦 CampusActivity.Infrastructure - 基础设施层" -ForegroundColor White
    Write-Host "  📦 CampusActivity.Application   - 应用服务层" -ForegroundColor White
    Write-Host "  🌐 CampusActivity.WebAPI        - RESTful API" -ForegroundColor White
    Write-Host "  🎨 CampusActivity.BlazorWeb     - Web界面" -ForegroundColor White
    Write-Host ""
    
    if ($Configuration -eq "Release") {
        Write-Info "下一步 - 部署:"
        Write-Host "  1. 检查 publish/ 目录下的输出文件" -ForegroundColor Yellow
        Write-Host "  2. 运行 .\install.ps1 进行环境安装" -ForegroundColor Yellow
        Write-Host "  3. 配置数据库连接字符串" -ForegroundColor Yellow
        Write-Host "  4. 启动服务" -ForegroundColor Yellow
    } else {
        Write-Info "下一步 - 开发:"
        Write-Host "  1. 启动API: cd src/CampusActivity.WebAPI && dotnet run" -ForegroundColor Yellow
        Write-Host "  2. 启动Web: cd src/CampusActivity.BlazorWeb && dotnet run" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

# 错误处理
trap {
    Write-Error "构建过程中发生错误: $($_.Exception.Message)"
    Write-Host "错误位置: $($_.InvocationInfo.ScriptLineNumber) 行" -ForegroundColor Red
    exit 1
}

# 主执行流程
try {
    Show-Banner
    Test-Prerequisites
    Invoke-Clean
    Restore-Packages
    Build-Solution
    Run-Tests
    Create-PublishPackage
    Show-BuildSummary
}
catch {
    Write-Error "构建失败: $($_.Exception.Message)"
    exit 1
}

exit 0 