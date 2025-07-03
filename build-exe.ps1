# 快速打包脚本 - 只生成exe文件
param(
    [switch]$SkipServer,
    [switch]$SkipClient
)

Write-Host "🚀 AvaChat 快速打包" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Gray

# 创建发布目录
$outputDir = ".\Dist"
if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outputDir | Out-Null

if (-not $SkipServer) {
    Write-Host "📦 打包服务器端..." -ForegroundColor Cyan

    dotnet publish .\AvaChat.Server\AvaChat.Server.csproj `
        -c Release `
        -r win-x64 `
        --self-contained `
        -o "$outputDir" `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:PublishTrimmed=false

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ 服务器端打包完成" -ForegroundColor Green
    } else {
        Write-Host "❌ 服务器端打包失败" -ForegroundColor Red
        exit 1
    }
}

if (-not $SkipClient) {
    Write-Host "📦 打包客户端..." -ForegroundColor Cyan

    # 临时重命名服务器exe以避免冲突
    if (Test-Path "$outputDir\AvaChat.Server.exe") {
        Rename-Item "$outputDir\AvaChat.Server.exe" "AvaChat.Server.exe.temp"
    }

    dotnet publish .\AvaChat.Client\AvaChat.Client.csproj `
        -c Release `
        -r win-x64 `
        --self-contained `
        -o "$outputDir" `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:PublishTrimmed=false

    # 恢复服务器exe名称
    if (Test-Path "$outputDir\AvaChat.Server.exe.temp") {
        Rename-Item "$outputDir\AvaChat.Server.exe.temp" "AvaChat.Server.exe"
    }

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ 客户端打包完成" -ForegroundColor Green
    } else {
        Write-Host "❌ 客户端打包失败" -ForegroundColor Red
        exit 1
    }
}

# 清理不需要的文件，只保留exe
Get-ChildItem $outputDir | Where-Object { $_.Extension -ne ".exe" } | Remove-Item -Recurse -Force

Write-Host ""
Write-Host "🎉 打包完成!" -ForegroundColor Green
Write-Host "📂 输出目录: $outputDir" -ForegroundColor Yellow

Get-ChildItem "$outputDir\*.exe" | ForEach-Object {
    $size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "   $($_.Name) - $size MB" -ForegroundColor Gray
}

Write-Host ""
Write-Host "💡 使用方法:" -ForegroundColor Yellow
Write-Host "   1. 先运行 AvaChat.Server.exe" -ForegroundColor Gray
Write-Host "   2. 再运行 AvaChat.Client.exe" -ForegroundColor Gray
