param(
    [string]$OutBat = ""
)
$ErrorActionPreference = "Stop"

# Auto-detect existing Android SDK first
$candidates = @(
    $env:ANDROID_HOME,
    $env:ANDROID_SDK_ROOT,
    (Join-Path $env:LOCALAPPDATA "Android\Sdk"),
    "C:\Android\Sdk"
) | Where-Object { $_ -and $_.Trim() -ne "" }

$sdkRoot = $null
foreach ($c in $candidates) {
    if ((Test-Path (Join-Path $c "platforms\android-34\android.jar")) -and (Test-Path (Join-Path $c "build-tools\34.0.0\apksigner.bat"))) {
        $sdkRoot = $c
        break
    }
}

if (-not $sdkRoot) {
    $sdkRoot = Join-Path $env:LOCALAPPDATA "Android\Sdk"
    New-Item -ItemType Directory -Force -Path $sdkRoot | Out-Null

    $androidCli = Get-ChildItem -Path (Join-Path $sdkRoot "cmdline-tools") -Filter "android.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    $sdkMan = Get-ChildItem -Path (Join-Path $sdkRoot "cmdline-tools") -Filter "sdkmanager.bat" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $androidCli -and -not $sdkMan) {
        Write-Host "[SDK] Resolving latest Google Android Command-Line Tools URL..."
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $repoXml = (New-Object System.Net.WebClient).DownloadString("https://dl.google.com/android/repository/repository2-1.xml")
        $matches = [regex]::Matches($repoXml, 'commandlinetools-win-(\d+)_latest\.zip')
        if ($matches.Count -eq 0) {
            throw "Could not find commandlinetools-win URL in Google repository XML"
        }
        $best = $matches | Sort-Object { [long]$_.Groups[1].Value } -Descending | Select-Object -First 1
        $dlUrl = "https://dl.google.com/android/repository/" + $best.Value
        $zipPath = Join-Path $env:TEMP $best.Value
        Write-Host "[SDK] Downloading $dlUrl ..."
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($dlUrl, $zipPath)
        $wc.Dispose()

        $tmpExtract = Join-Path $env:TEMP ("aim_cmdline_" + [guid]::NewGuid().ToString("N"))
        Expand-Archive -Path $zipPath -DestinationPath $tmpExtract -Force
        $destLatest = Join-Path $sdkRoot "cmdline-tools\latest"
        New-Item -ItemType Directory -Force -Path (Split-Path $destLatest) | Out-Null
        if (Test-Path $destLatest) { Remove-Item $destLatest -Recurse -Force }
        Move-Item (Join-Path $tmpExtract "cmdline-tools") $destLatest
        Remove-Item $tmpExtract -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
        $androidCli = Get-ChildItem -Path $destLatest -Filter "android.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        $sdkMan = Get-Item (Join-Path $destLatest "bin\sdkmanager.bat") -ErrorAction SilentlyContinue
    }

    Write-Host "[SDK] Installing Android platform 34 & build-tools 34.0.0..."
    $yes = (@("y") * 25) -join "`r`n"
    if ($androidCli) {
        $yes | & $androidCli.FullName "--sdk=$sdkRoot" sdk install "platforms/android-34" "build-tools/34.0.0"
        if ($LASTEXITCODE -ne 0) {
            throw "android sdk install failed with exit code $LASTEXITCODE"
        }
    } else {
        $yes | cmd /c "`"$($sdkMan.FullName)`" --sdk_root=`"$sdkRoot`" --licenses" | Out-Null
        cmd /c "`"$($sdkMan.FullName)`" --sdk_root=`"$sdkRoot`" `"platforms;android-34`" `"build-tools;34.0.0`""
        if ($LASTEXITCODE -ne 0) {
            throw "sdkmanager failed with exit code $LASTEXITCODE"
        }
    }
}

$androidJar = Join-Path $sdkRoot "platforms\android-34\android.jar"
$buildTools = Join-Path $sdkRoot "build-tools\34.0.0"
if (-not (Test-Path $androidJar)) { throw "Missing android.jar at $androidJar" }
if (-not (Test-Path (Join-Path $buildTools "aapt2.exe"))) { throw "Missing aapt2.exe in $buildTools" }

if ($OutBat) {
    @(
        "@echo off",
        "set ""ANDROID_SDK=$sdkRoot""",
        "set ""ANDROID_JAR=$androidJar""",
        "set ""BUILD_TOOLS=$buildTools"""
    ) | Set-Content -Path $OutBat -Encoding ASCII
}
Write-Host "[SDK] Ready: $sdkRoot"
