param(
    [string]$major,
    [string]$minor,
    [string]$patch,
    [string]$tag,
    [string]$refName
)

# Directory.Build.props 是仓库内正式包版本的唯一来源。
# CI 使用其中的 major.minor 作为发布线，并以 patch 作为自动递增的最低值。
$buildPropsPath = Join-Path $PSScriptRoot "..\..\..\Directory.Build.props"
[xml]$buildProps = Get-Content $buildPropsPath -Raw
$defaultVersionNode = $buildProps.Project.PropertyGroup.Version |
    Where-Object { $_ -and $_.Condition -match 'BUILD_BUILDNUMBER' -and $_.Condition -notmatch '!=' } |
    Select-Object -First 1
$defaultVersion = ([string]$defaultVersionNode.InnerText).Trim()

if ($defaultVersion -notmatch '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)$') {
    throw "Directory.Build.props 中未找到有效的默认包版本。"
}

$defaultVersionParts = $defaultVersion.Split('.')
if (!$major) {
    $major = $defaultVersionParts[0]
}
if (!$minor) {
    $minor = $defaultVersionParts[1]
}
if (!$patch) {
    $patch = "*"
}

$remote_refs = git ls-remote --tags
if ($LASTEXITCODE -ne 0) {
    throw "无法读取远端 Git 标签，不能安全计算构建版本。"
}

if ($patch -eq "*") {
    $stableTagPattern = "^refs/tags/v$([regex]::Escape($major))\.$([regex]::Escape($minor))\.(?<patch>\d+)$"
    $publishedPatches = $remote_refs | ForEach-Object {
        $tagRef = ($_ -split '\s+')[-1]
        if ($tagRef -match $stableTagPattern) {
            [int]$Matches.patch
        }
    }
    $minimumPatch = if ($major -eq $defaultVersionParts[0] -and $minor -eq $defaultVersionParts[1]) {
        [int]$defaultVersionParts[2]
    }
    else {
        0
    }
    $patch = $minimumPatch
    if ($publishedPatches) {
        $latestPatch = $publishedPatches | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum
        $patch = [Math]::Max($minimumPatch, $latestPatch + 1)
    }
}

$CustomBuildVersion = "$major.$minor.$patch"

if ($refName) {
    $tag = $refName.Replace("_","-")
    if ($refName -eq "master") {
        $tag = $null
    }
}
if ($tag -eq "dev") {
    $devTagPattern = "^refs/tags/v$([regex]::Escape($CustomBuildVersion))-$tag\.(?<patch>\d+)$"
    $publishedDevPatches = $remote_refs | ForEach-Object {
        $tagRef = ($_ -split '\s+')[-1]
        if ($tagRef -match $devTagPattern) {
            [int]$Matches.patch
        }
    }
    $devPatch = "1"
    if ($publishedDevPatches) {
        $latestDevPatch = $publishedDevPatches | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum
        $devPatch = $latestDevPatch + 1
    }
    $CustomBuildVersion = "$CustomBuildVersion-$tag.$devPatch"
}

Write-Host "Setting the value of current build version :  $CustomBuildVersion" 
echo "BUILD_BUILDNUMBER=$CustomBuildVersion" >> $env:GITHUB_OUTPUT
echo "BUILD_BUILDNUMBER=$CustomBuildVersion" >> $env:GITHUB_ENV
echo "BUILD_VERSION=v$CustomBuildVersion" >> $env:GITHUB_OUTPUT
echo "BUILD_VERSION=v$CustomBuildVersion" >> $env:GITHUB_ENV
