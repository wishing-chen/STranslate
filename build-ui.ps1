param([string]$Dotnet = 'dotnet', [switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$weaverPath = Join-Path $PSScriptRoot 'src/STranslate/FodyWeavers.xml'
$weaverContent = [IO.File]::ReadAllBytes($weaverPath)
Push-Location $PSScriptRoot
try {
    & $Dotnet build src/STranslate.slnx --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Release 构建失败。' }
    if (-not $SkipTests) {
        & $Dotnet test src/Tests/STranslate.Tests/STranslate.Tests.csproj --configuration Release --no-build
        if ($LASTEXITCODE -ne 0) { throw '自动测试失败。' }
    }
    # 便携模式仅需此目录存在；不复制原安装中的账号、密钥或历史。
    $portable = Join-Path $PSScriptRoot 'src/.artifacts/Release/PortableConfig'
    New-Item -ItemType Directory -Path $portable -Force | Out-Null
    Write-Host '构建完成：src/.artifacts/Release/STranslate.exe'
}
finally {
    # MSBuild 会按配置重写此文件，构建结束恢复调用前的源码内容。
    [IO.File]::WriteAllBytes($weaverPath, $weaverContent)
    Pop-Location
}
