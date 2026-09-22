[CmdletBinding()]
param(
    [Parameter()]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$launcherRoot = $PSScriptRoot
$repositoryRoot = Split-Path -Parent $launcherRoot
$catalogRoot = Join-Path $repositoryRoot 'games'
$stagingRoot = Join-Path $launcherRoot 'publish\installer-staging'
$installerOutput = Join-Path $launcherRoot 'Installer'
$publishScript = Join-Path $launcherRoot 'Publish-App.ps1'
$installerProject = Join-Path $launcherRoot 'build\installer\TGLauncher.aip'
$wpfProject = Join-Path $launcherRoot 'src\GameTranslationLauncher.Wpf\GameTranslationLauncher.Wpf.csproj'

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$projectXml = Get-Content -LiteralPath $wpfProject
    $Version = $projectXml.Project.PropertyGroup |
        ForEach-Object { $_.Version } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 1
}

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Version Launcher phải có dạng major.minor.patch, ví dụ 2.0.0.'
}

if (-not (Test-Path -LiteralPath $catalogRoot -PathType Container)) {
    throw "Không tìm thấy catalog package: $catalogRoot"
}

if (Test-Path -LiteralPath $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null
New-Item -ItemType Directory -Path $installerOutput -Force | Out-Null

& $publishScript -OutputPath $stagingRoot
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$packagedGamesRoot = Join-Path $stagingRoot 'games'
$packagedGameCount = 0
foreach ($gameDirectory in Get-ChildItem -LiteralPath $catalogRoot -Directory) {
    $packageDirectory = Join-Path $gameDirectory.FullName 'dist\launcher'
    if (-not (Test-Path -LiteralPath $packageDirectory -PathType Container)) {
        continue
    }

    $destinationDirectory = Join-Path $packagedGamesRoot "$($gameDirectory.Name)\dist"
    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    Copy-Item -LiteralPath $packageDirectory -Destination $destinationDirectory -Recurse -Force
    $packagedGameCount++
}

if ($packagedGameCount -eq 0) {
    throw 'Không có launcher package hợp lệ để đóng gói.'
}

$advancedInstallerCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Caphyon\Advanced Installer 21.2\bin\x86\AdvancedInstaller.com'),
    (Get-Command AdvancedInstaller.com -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue)
) | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) }
$advancedInstaller = $advancedInstallerCandidates | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($advancedInstaller)) {
    throw 'Không tìm thấy Advanced Installer 21.2. Hãy cài Advanced Installer rồi chạy lại Build-Installer.ps1.'
}

if (-not (Test-Path -LiteralPath $installerProject -PathType Leaf)) {
    throw "Không tìm thấy project Advanced Installer: $installerProject"
}

& $advancedInstaller /edit $installerProject /SetVersion $Version
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $advancedInstaller /edit $installerProject /SetOutputLocation -buildname DefaultBuild -path $installerOutput
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$setupPath = Join-Path $installerOutput "TGLauncherSetup-$Version.exe"
& $advancedInstaller /edit $installerProject /SetPackageName $setupPath -buildname DefaultBuild
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# NOTE: Reset và đồng bộ lại để mọi package trong catalog hiện tại được đóng gói,
# kể cả khi project được mở ở một đường dẫn workspace khác.
& $advancedInstaller /edit $installerProject /ResetSync APPDIR -clearcontent
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $advancedInstaller /edit $installerProject /NewSync APPDIR $stagingRoot -existingfiles delete
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# NOTE(installer-shortcuts): ResetSync loại bỏ shortcut phụ thuộc vào executable
# cũ, nên tạo lại sau khi TGLauncher.exe đã được đồng bộ vào APPDIR.
& $advancedInstaller /edit $installerProject /NewShortcut `
    -name 'TGLauncher.exe' `
    -dir DesktopFolder `
    -target 'APPDIR\TGLauncher.exe' `
    -wkdir APPDIR `
    -desc 'Mở TG Launcher'
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $advancedInstaller /edit $installerProject /NewShortcut `
    -name TGLauncher `
    -dir SHORTCUTDIR `
    -target 'APPDIR\TGLauncher.exe' `
    -wkdir APPDIR `
    -desc 'Mở TG Launcher'
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $advancedInstaller /build $installerProject
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $setupPath -PathType Leaf)) {
    throw "Advanced Installer đã build xong nhưng không tìm thấy setup: $setupPath"
}

Write-Host "Đã đóng gói $packagedGameCount game: $setupPath"
