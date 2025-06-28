# ==============================================================================
# 校园活动管理系统 - 停止脚本
# Campus Activity Management System - Stop Script
# ==============================================================================
# 作者: 开发团队
# 版本: 1.0.0
# 描述: 停止校园活动管理系统的所有服务
# 使用方法: .\stop.ps1 [-Force] [-Help]
# ==============================================================================

param(
    [Parameter()]
    [switch]$Force,
    
    [Parameter()]
    [switch]$Help
)

# 显示帮助信息
if ($Help) {
    Write-Host "校园活动管理系统 - 停止脚本" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "用法:" -ForegroundColor Yellow
    Write-Host "  .\stop.ps1 [Options]" -ForegroundColor White
    Write-Host ""
    Write-Host "选项:" -ForegroundColor Yellow
    Write-Host "  -Force    强制停止所有相关进程" -ForegroundColor White
    Write-Host "  -Help     显示此帮助信息" -ForegroundColor White
    Write-Host ""
    Write-Host "示例:" -ForegroundColor Yellow
    Write-Host "  .\stop.ps1        # 正常停止" -ForegroundColor White
    Write-Host "  .\stop.ps1 -Force # 强制停止" -ForegroundColor White
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
    Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║                   校园活动管理系统 - 停止脚本                     ║" -ForegroundColor Red
    Write-Host "║                Campus Activity Management System                 ║" -ForegroundColor Red
    Write-Host "║                          Stop Script                            ║" -ForegroundColor Red
    Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Red
    Write-Host ""
    Write-Info "停止时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    Write-Host ""
}

# 停止端口上的进程
function Stop-ProcessByPort($port, $serviceName) {
    Write-Step "检查端口 $port ($serviceName)..."
    
    try {
        # 查找占用端口的进程
        $netstatOutput = netstat -ano | findstr ":$port "
        
        if ($netstatOutput) {
            $processIds = @()
            
            foreach ($line in $netstatOutput) {
                if ($line -match "\s+(\d+)$") {
                    $processId = $matches[1]
                    if ($processId -and $processId -ne "0") {
                        $processIds += $processId
                    }
                }
            }
            
            $processIds = $processIds | Select-Object -Unique
            
            foreach ($processId in $processIds) {
                try {
                    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
                    if ($process) {
                        Write-Info "停止进程: $($process.ProcessName) (PID: $processId)"
                        
                        if ($Force) {
                            Stop-Process -Id $processId -Force
                        } else {
                            Stop-Process -Id $processId
                        }
                        
                        Write-Success "已停止 $serviceName 进程 (PID: $processId)"
                    }
                }
                catch {
                    Write-Warning "无法停止进程 $processId : $($_.Exception.Message)"
                }
            }
        } else {
            Write-Info "$serviceName 端口 $port 未被占用"
        }
    }
    catch {
        Write-Warning "检查端口 $port 时出错: $($_.Exception.Message)"
    }
}

# 停止相关进程
function Stop-CampusActivityProcesses {
    Write-Step "查找校园活动系统相关进程..."
    
    $processNames = @(
        "CampusActivity.*",
        "dotnet"
    )
    
    $stoppedProcesses = 0
    
    foreach ($processName in $processNames) {
        try {
            $processes = Get-Process | Where-Object { 
                $_.ProcessName -like $processName -or 
                ($_.MainModule -and $_.MainModule.FileName -like "*CampusActivity*") -or
                ($_.CommandLine -and $_.CommandLine -like "*CampusActivity*")
            }
            
            foreach ($process in $processes) {
                try {
                    # 检查是否为校园活动系统相关进程
                    $isCampusActivity = $false
                    
                    if ($process.ProcessName -like "*CampusActivity*") {
                        $isCampusActivity = $true
                    }
                    elseif ($process.ProcessName -eq "dotnet") {
                        # 检查dotnet进程的命令行参数
                        try {
                            $commandLine = (Get-WmiObject Win32_Process -Filter "ProcessId = $($process.Id)").CommandLine
                            if ($commandLine -and $commandLine -like "*CampusActivity*") {
                                $isCampusActivity = $true
                            }
                        }
                        catch {
                            # 无法获取命令行，跳过
                        }
                    }
                    
                    if ($isCampusActivity) {
                        Write-Info "停止进程: $($process.ProcessName) (PID: $($process.Id))"
                        
                        if ($Force) {
                            Stop-Process -Id $process.Id -Force
                        } else {
                            Stop-Process -Id $process.Id
                        }
                        
                        $stoppedProcesses++
                        Write-Success "已停止进程: $($process.ProcessName) (PID: $($process.Id))"
                    }
                }
                catch {
                    Write-Warning "无法停止进程 $($process.ProcessName) (PID: $($process.Id)): $($_.Exception.Message)"
                }
            }
        }
        catch {
            Write-Warning "查找进程 $processName 时出错: $($_.Exception.Message)"
        }
    }
    
    if ($stoppedProcesses -eq 0) {
        Write-Info "未找到正在运行的校园活动系统进程"
    } else {
        Write-Success "共停止了 $stoppedProcesses 个进程"
    }
}

# 清理临时文件
function Clear-TempFiles {
    Write-Step "清理临时文件..."
    
    $tempDirs = @(
        "temp",
        "logs/temp"
    )
    
    foreach ($dir in $tempDirs) {
        if (Test-Path $dir) {
            try {
                Remove-Item "$dir/*" -Recurse -Force -ErrorAction SilentlyContinue
                Write-Success "已清理临时目录: $dir"
            }
            catch {
                Write-Warning "清理临时目录失败: $dir"
            }
        }
    }
}

# 检查服务状态
function Test-ServiceStatus {
    Write-Step "检查服务状态..."
    
    $ports = @(
        @{ Port = 7186; Name = "WebAPI" },
        @{ Port = 7150; Name = "BlazorWeb" }
    )
    
    foreach ($service in $ports) {
        try {
            $connection = Test-NetConnection -ComputerName localhost -Port $service.Port -InformationLevel Quiet -WarningAction SilentlyContinue
            if ($connection) {
                Write-Warning "$($service.Name) 服务仍在运行 (端口 $($service.Port))"
            } else {
                Write-Success "$($service.Name) 服务已停止"
            }
        }
        catch {
            Write-Success "$($service.Name) 服务已停止"
        }
    }
}

# 显示停止摘要
function Show-StopSummary {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║                         系统停止完成                             ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    
    Write-Info "管理操作:"
    Write-Host "  启动系统: .\start.ps1" -ForegroundColor Cyan
    Write-Host "  重新构建: .\build.ps1" -ForegroundColor Cyan
    Write-Host "  重新安装: .\install.ps1" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Success "🛑 校园活动管理系统已停止"
}

# 主执行流程
try {
    Show-Banner
    
    # 按端口停止服务
    Stop-ProcessByPort 7186 "WebAPI"
    Stop-ProcessByPort 7150 "BlazorWeb"
    
    # 等待一段时间让进程优雅关闭
    Write-Info "等待进程优雅关闭..."
    Start-Sleep -Seconds 3
    
    # 停止相关进程
    Stop-CampusActivityProcesses
    
    # 清理临时文件
    Clear-TempFiles
    
    # 最终状态检查
    Test-ServiceStatus
    
    # 显示摘要
    Show-StopSummary
}
catch {
    Write-Error "停止过程中发生错误: $($_.Exception.Message)"
    Write-Host "错误位置: $($_.InvocationInfo.ScriptLineNumber) 行" -ForegroundColor Red
    
    if (!$Force) {
        Write-Info "尝试使用 -Force 参数强制停止"
    }
    
    exit 1
}

exit 0 