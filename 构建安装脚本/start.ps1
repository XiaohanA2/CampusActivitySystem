# ==============================================================================
# 校园活动管理系统 - 启动脚本
# Campus Activity Management System - Start Script
# ==============================================================================
# 作者: 开发团队
# 版本: 1.0.0
# 描述: 启动校园活动管理系统的所有服务
# 使用方法: .\start.ps1 [-Environment] [-WaitForExit] [-Help]
# ==============================================================================

param(
    [Parameter()]
    [ValidateSet("Development", "Production", "Testing")]
    [string]$Environment = "Development",
    
    [Parameter()]
    [switch]$WaitForExit,
    
    [Parameter()]
    [switch]$Help
)

# 显示帮助信息
if ($Help) {
    Write-Host "校园活动管理系统 - 启动脚本" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "用法:" -ForegroundColor Yellow
    Write-Host "  .\start.ps1 [Options]" -ForegroundColor White
    Write-Host ""
    Write-Host "选项:" -ForegroundColor Yellow
    Write-Host "  -Environment    运行环境 (Development, Production, Testing)" -ForegroundColor White
    Write-Host "  -WaitForExit    等待用户按键后退出" -ForegroundColor White
    Write-Host "  -Help           显示此帮助信息" -ForegroundColor White
    Write-Host ""
    Write-Host "示例:" -ForegroundColor Yellow
    Write-Host "  .\start.ps1                           # 开发环境启动" -ForegroundColor White
    Write-Host "  .\start.ps1 -Environment Production   # 生产环境启动" -ForegroundColor White
    Write-Host "  .\start.ps1 -WaitForExit              # 启动后等待" -ForegroundColor White
    exit 0
}

# 颜色输出函数
function Write-Info($message) { Write-Host "ℹ️  $message" -ForegroundColor Cyan }
function Write-Success($message) { Write-Host "✅ $message" -ForegroundColor Green }
function Write-Warning($message) { Write-Host "⚠️  $message" -ForegroundColor Yellow }
function Write-Error($message) { Write-Host "❌ $message" -ForegroundColor Red }
function Write-Step($message) { Write-Host "🔄 $message" -ForegroundColor Blue }

# 横幅
function Show-Banner {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║                   校园活动管理系统 - 启动脚本                     ║" -ForegroundColor Green
    Write-Host "║                Campus Activity Management System                 ║" -ForegroundColor Green
    Write-Host "║                          Start Script                           ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    Write-Info "运行环境: $Environment"
    Write-Info "启动时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    Write-Host ""
}

# 检查端口占用
function Test-PortAvailability($port, $serviceName) {
    $connection = Test-NetConnection -ComputerName localhost -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($connection) {
        Write-Warning "$serviceName 端口 $port 已被占用"
        return $false
    } else {
        Write-Success "$serviceName 端口 $port 可用"
        return $true
    }
}

# 检查项目文件
function Test-ProjectFiles {
    Write-Step "检查项目文件..."
    
    $requiredFiles = @(
        "src/CampusActivity.WebAPI/CampusActivity.WebAPI.csproj",
        "src/CampusActivity.BlazorWeb/CampusActivity.BlazorWeb.csproj"
    )
    
    foreach ($file in $requiredFiles) {
        if (Test-Path $file) {
            Write-Success "项目文件: $file"
        } else {
            Write-Error "项目文件缺失: $file"
            exit 1
        }
    }
    
    Write-Host ""
}

# 检查配置文件
function Test-Configuration {
    Write-Step "检查配置文件..."
    
    $configFiles = @(
        "src/CampusActivity.WebAPI/appsettings.json",
        "src/CampusActivity.BlazorWeb/appsettings.json"
    )
    
    if ($Environment -ne "Development") {
        $configFiles += @(
            "src/CampusActivity.WebAPI/appsettings.$Environment.json",
            "src/CampusActivity.BlazorWeb/appsettings.$Environment.json"
        )
    }
    
    foreach ($file in $configFiles) {
        if (Test-Path $file) {
            Write-Success "配置文件: $file"
        } else {
            Write-Warning "配置文件缺失: $file"
        }
    }
    
    Write-Host ""
}

# 启动WebAPI服务
function Start-WebApiService {
    Write-Step "启动WebAPI服务..."
    
    # 检查端口
    if (!(Test-PortAvailability 7186 "WebAPI")) {
        Write-Error "WebAPI端口被占用，请先停止占用该端口的进程"
        return $false
    }
    
    try {
        $apiPath = "src/CampusActivity.WebAPI"
        
        # 设置环境变量
        $env:ASPNETCORE_ENVIRONMENT = $Environment
        
        Write-Info "在新窗口中启动WebAPI服务..."
        Write-Host "  路径: $apiPath" -ForegroundColor White
        Write-Host "  环境: $Environment" -ForegroundColor White
        Write-Host "  地址: http://localhost:7186" -ForegroundColor White
        
        # 启动WebAPI
        $apiProcess = Start-Process powershell -ArgumentList @(
            "-NoProfile",
            "-WindowStyle", "Normal",
            "-Command", 
            "& { Write-Host 'WebAPI服务启动中...' -ForegroundColor Green; cd '$apiPath'; `$env:ASPNETCORE_ENVIRONMENT='$Environment'; dotnet run; Read-Host '按任意键关闭窗口' }"
        ) -PassThru
        
        Write-Success "WebAPI服务启动命令已发送 (PID: $($apiProcess.Id))"
        
        # 等待服务启动
        Write-Info "等待WebAPI服务启动..."
        Start-Sleep -Seconds 8
        
        # 测试API是否可访问
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:7186/health" -Method GET -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Success "WebAPI服务启动成功"
            } else {
                Write-Warning "WebAPI服务可能未完全启动"
            }
        }
        catch {
            Write-Warning "无法连接到WebAPI服务，可能仍在启动中"
        }
        
        return $true
    }
    catch {
        Write-Error "启动WebAPI服务失败: $($_.Exception.Message)"
        return $false
    }
}

# 启动BlazorWeb服务
function Start-BlazorWebService {
    Write-Step "启动Blazor Web服务..."
    
    # 检查端口
    if (!(Test-PortAvailability 7150 "BlazorWeb")) {
        Write-Error "BlazorWeb端口被占用，请先停止占用该端口的进程"
        return $false
    }
    
    try {
        $webPath = "src/CampusActivity.BlazorWeb"
        
        # 设置环境变量
        $env:ASPNETCORE_ENVIRONMENT = $Environment
        
        Write-Info "在新窗口中启动Blazor Web服务..."
        Write-Host "  路径: $webPath" -ForegroundColor White
        Write-Host "  环境: $Environment" -ForegroundColor White
        Write-Host "  地址: http://localhost:7150" -ForegroundColor White
        
        # 启动BlazorWeb
        $webProcess = Start-Process powershell -ArgumentList @(
            "-NoProfile",
            "-WindowStyle", "Normal",
            "-Command", 
            "& { Write-Host 'Blazor Web服务启动中...' -ForegroundColor Green; cd '$webPath'; `$env:ASPNETCORE_ENVIRONMENT='$Environment'; dotnet run; Read-Host '按任意键关闭窗口' }"
        ) -PassThru
        
        Write-Success "Blazor Web服务启动命令已发送 (PID: $($webProcess.Id))"
        
        # 等待服务启动
        Write-Info "等待Blazor Web服务启动..."
        Start-Sleep -Seconds 8
        
        # 测试Web是否可访问
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:7150" -Method GET -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Success "Blazor Web服务启动成功"
            } else {
                Write-Warning "Blazor Web服务可能未完全启动"
            }
        }
        catch {
            Write-Warning "无法连接到Blazor Web服务，可能仍在启动中"
        }
        
        return $true
    }
    catch {
        Write-Error "启动Blazor Web服务失败: $($_.Exception.Message)"
        return $false
    }
}

# 显示启动摘要
function Show-StartupSummary {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║                         系统启动完成                             ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    
    Write-Info "服务地址:"
    Write-Host "  🌐 Blazor Web界面: http://localhost:7150" -ForegroundColor Cyan
    Write-Host "  🔌 WebAPI服务:     http://localhost:7186" -ForegroundColor Cyan
    Write-Host "  📚 API文档:        http://localhost:7186/swagger" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Info "默认测试账户:"
    Write-Host "  👑 管理员: admin / admin123" -ForegroundColor Yellow
    Write-Host "  🎓 学生:   student1 / 123456" -ForegroundColor Yellow
    Write-Host "  👨‍🏫 教师:   teacher1 / 123456" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Info "管理操作:"
    Write-Host "  停止系统: .\stop.ps1" -ForegroundColor White
    Write-Host "  重新构建: .\build.ps1" -ForegroundColor White
    Write-Host "  查看日志: logs/ 目录" -ForegroundColor White
    Write-Host ""
    
    Write-Success "🎉 校园活动管理系统已成功启动!"
    
    # 自动打开浏览器
    try {
        Write-Info "正在打开浏览器..."
        Start-Process "http://localhost:7150"
    }
    catch {
        Write-Warning "无法自动打开浏览器，请手动访问 http://localhost:7150"
    }
}

# 主执行流程
try {
    Show-Banner
    Test-ProjectFiles
    Test-Configuration
    
    # 启动服务
    $apiStarted = Start-WebApiService
    $webStarted = Start-BlazorWebService
    
    if ($apiStarted -and $webStarted) {
        Show-StartupSummary
    } else {
        Write-Error "部分服务启动失败，请检查错误信息"
        exit 1
    }
    
    # 等待用户输入
    if ($WaitForExit) {
        Write-Host ""
        Read-Host "按回车键退出脚本 (服务将继续运行)"
    }
}
catch {
    Write-Error "启动失败: $($_.Exception.Message)"
    Write-Host "错误位置: $($_.InvocationInfo.ScriptLineNumber) 行" -ForegroundColor Red
    exit 1
}

exit 0 