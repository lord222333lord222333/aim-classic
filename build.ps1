$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    throw "Could not find csc.exe (.NET Framework 4.0)."
}

Add-Type -AssemblyName System.Drawing
$icoPath = Join-Path $root "assets\aim.ico"
New-Item -ItemType Directory -Force -Path (Split-Path $icoPath) | Out-Null

function New-AimLogoBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $sc = $size / 32.0
    $g.ScaleTransform($sc, $sc)

    # Rounded AIM Royal Blue badge
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $r = 7.0
    $path.AddArc(1, 1, $r, $r, 180, 90)
    $path.AddArc(30 - $r, 1, $r, $r, 270, 90)
    $path.AddArc(30 - $r, 30 - $r, $r, $r, 0, 90)
    $path.AddArc(1, 30 - $r, $r, $r, 90, 90)
    $path.CloseFigure()

    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Rectangle(1, 1, 30, 30)),
        [System.Drawing.Color]::FromArgb(46, 138, 248),
        [System.Drawing.Color]::FromArgb(8, 52, 146),
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
    )
    $g.FillPath($bg, $path)
    $penWhite = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 1.5)
    $g.DrawPath($penWhite, $path)

    # Classic Yellow Running Man silhouette
    $brGold = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 208, 0))
    $penEdge = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(72, 46, 0), 1.1)
    $g.FillEllipse($brGold, 14.5, 4.2, 6.2, 6.2)
    $g.DrawEllipse($penEdge, 14.5, 4.2, 6.2, 6.2)
    $pts = [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF(9.5, 13.8)),
        (New-Object System.Drawing.PointF(16.2, 10.8)),
        (New-Object System.Drawing.PointF(23.5, 12.8)),
        (New-Object System.Drawing.PointF(21.6, 15.8)),
        (New-Object System.Drawing.PointF(17.4, 14.8)),
        (New-Object System.Drawing.PointF(16.0, 19.4)),
        (New-Object System.Drawing.PointF(21.5, 24.8)),
        (New-Object System.Drawing.PointF(19.0, 27.4)),
        (New-Object System.Drawing.PointF(13.8, 22.0)),
        (New-Object System.Drawing.PointF(9.8, 27.6)),
        (New-Object System.Drawing.PointF(7.0, 25.5)),
        (New-Object System.Drawing.PointF(11.6, 18.2)),
        (New-Object System.Drawing.PointF(12.8, 14.2)),
        (New-Object System.Drawing.PointF(8.2, 16.0))
    )
    $g.FillPolygon($brGold, $pts)
    $g.DrawPolygon($penEdge, $pts)

    $bg.Dispose(); $penWhite.Dispose(); $brGold.Dispose(); $penEdge.Dispose(); $path.Dispose(); $g.Dispose()
    return $bmp
}

function Convert-BitmapToDibIcoBytes([System.Drawing.Bitmap]$bmp) {
    $w = $bmp.Width
    $h = $bmp.Height
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    # BITMAPINFOHEADER (40 bytes)
    $bw.Write([UInt32]40)
    $bw.Write([Int32]$w)
    $bw.Write([Int32]($h * 2))
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]0)
    $bw.Write([UInt32]($w * $h * 4))
    $bw.Write([Int32]0); $bw.Write([Int32]0); $bw.Write([UInt32]0); $bw.Write([UInt32]0)

    # Bottom-up BGRA pixels
    for ($y = $h - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $w; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $bw.Write([byte]$c.B); $bw.Write([byte]$c.G); $bw.Write([byte]$c.R); $bw.Write([byte]$c.A)
        }
    }
    # Bottom-up 1bpp AND mask padded to 4-byte stride
    $stride = [int]([Math]::Ceiling($w / 32.0) * 4)
    for ($y = $h - 1; $y -ge 0; $y--) {
        $row = New-Object byte[] $stride
        for ($x = 0; $x -lt $w; $x++) {
            if ($bmp.GetPixel($x, $y).A -lt 20) {
                $byteIdx = [int][Math]::Floor($x / 8)
                $bit = 7 - ($x % 8)
                $row[$byteIdx] = $row[$byteIdx] -bor (1 -shl $bit)
            }
        }
        $bw.Write($row)
    }
    $bw.Flush()
    $bytes = $ms.ToArray()
    $bw.Dispose(); $ms.Dispose()
    return ,$bytes
}

# Build multi-resolution ICO: 16x16, 32x32, 48x48, 64x64 pure 32bpp DIBs (compatible with csc.exe /win32icon and all Windows taskbars)
$sizes = @(16, 32, 48, 64)
$entries = @()
foreach ($sz in $sizes) {
    $b = New-AimLogoBitmap $sz
    $entries += ,@{ Size = $sz; Bytes = (Convert-BitmapToDibIcoBytes $b) }
    $b.Dispose()
}

$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$entries.Count)
$offset = 6 + (16 * $entries.Count)
foreach ($e in $entries) {
    $szByte = if ($e.Size -ge 256) { [byte]0 } else { [byte]$e.Size }
    $bw.Write($szByte); $bw.Write($szByte); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$e.Bytes.Length)
    $bw.Write([UInt32]$offset)
    $offset += $e.Bytes.Length
}
foreach ($e in $entries) { $bw.Write($e.Bytes) }
$bw.Dispose(); $fs.Dispose()

# Gather C# sources, avatars, sounds, and app icon resource
$csFiles = Get-ChildItem (Join-Path $root "src") -Recurse -Filter *.cs | ForEach-Object { $_.FullName }
$resArgs = @("/resource:$icoPath,AIMClassic.AppIcon.ico")

$avDir = Join-Path $root "assets\avatars"
if (Test-Path $avDir) {
    Get-ChildItem $avDir -Filter *.gif | ForEach-Object {
        $resArgs += ("/resource:" + $_.FullName + ",AIMClassic.Avatars." + $_.Name)
    }
}

$sndDir = Join-Path $root "assets\sounds"
if (Test-Path $sndDir) {
    Get-ChildItem $sndDir -Filter *.wav | ForEach-Object {
        $resArgs += ("/resource:" + $_.FullName + ",AIMClassic.Sounds." + $_.Name)
    }
}

$xpDir = Join-Path $root "assets\xp"
if (Test-Path $xpDir) {
    Get-ChildItem $xpDir -Filter *.png | ForEach-Object {
        $resArgs += ("/resource:" + $_.FullName + ",AIMClassic.XpSprites." + $_.Name)
    }
}

$banDir = Join-Path $root "assets\banners"
if (Test-Path $banDir) {
    Get-ChildItem $banDir -Filter *.jpg | ForEach-Object {
        $resArgs += ("/resource:" + $_.FullName + ",AIMClassic.Banners." + $_.Name)
    }
}

$pwaDir = Join-Path $root "mobile_pwa"
if (Test-Path $pwaDir) {
    Get-ChildItem $pwaDir -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($pwaDir.Length + 1).Replace('\', '.')
        $resArgs += ("/resource:" + $_.FullName + ",AIMClassic.MobilePwa." + $rel)
    }
}

$outExe = Join-Path $root "AIMClassic.exe"
$compileExe = $outExe
$lockedMain = $false
if (Test-Path $outExe) {
    try {
        $fsLockCheck = [System.IO.File]::Open($outExe, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        $fsLockCheck.Close()
    }
    catch {
        $lockedMain = $true
        $compileExe = Join-Path ([System.IO.Path]::GetTempPath()) "AIMClassic_stage.exe"
        Write-Host "WARNING: AIMClassic.exe is currently running/locked. Compiling fresh build to temporary staging exe to update AIM_Setup.exe..."
    }
}

$rspPath = Join-Path $root "build_args.rsp"

$argsList = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    "/utf8output",
    "/out:$compileExe",
    "/win32icon:$icoPath",
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/r:System.Web.Extensions.dll"
) + $resArgs + $csFiles

$quoted = $argsList | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }
[System.IO.File]::WriteAllLines($rspPath, $quoted, [System.Text.Encoding]::UTF8)

& $csc "@$rspPath"
$exitCode = $LASTEXITCODE
Remove-Item $rspPath -Force -ErrorAction SilentlyContinue

if ($exitCode -ne 0) {
    throw "csc.exe failed with exit code $exitCode"
}

$fi = Get-Item $compileExe
Write-Host ("SUCCESS: Built " + $fi.FullName + " (" + [Math]::Round($fi.Length / 1024, 1) + " KB) with " + $resArgs.Count + " embedded resources (icon, avatars, sounds)!")

# Build classic 2005-2008 Installer AIM_Setup.exe if setup_src exists
$setupSrcDir = Join-Path $root "setup_src"
if (Test-Path $setupSrcDir) {
    $oldSetupIco = Join-Path $root "assets\aim_setup.ico"
    if (Test-Path $oldSetupIco) { Remove-Item $oldSetupIco -Force -ErrorAction SilentlyContinue }

    # Compress freshly built client exe for embedding inside AIM_Setup.exe
    $gzPayload = Join-Path $root "assets\AIMClassic.exe.gz"
    $rawFs = [System.IO.File]::OpenRead($compileExe)
    $outGzFs = [System.IO.File]::Create($gzPayload)
    $gzStream = New-Object System.IO.Compression.GZipStream($outGzFs, [System.IO.Compression.CompressionMode]::Compress)
    $rawFs.CopyTo($gzStream)
    $gzStream.Dispose(); $outGzFs.Dispose(); $rawFs.Dispose()

    $setupCs = Get-ChildItem $setupSrcDir -Recurse -Filter *.cs | ForEach-Object { $_.FullName }
    $setupRes = @(
        "/resource:$icoPath,AIMSetup.AppIcon.ico",
        "/resource:$icoPath,AIMSetup.ClientIcon.ico",
        "/resource:$gzPayload,AIMSetup.Payload.exe.gz"
    )
    $chimePath = Join-Path $root "assets\sounds\connected_chime.wav"
    if (Test-Path $chimePath) {
        $setupRes += "/resource:$chimePath,AIMSetup.Chime.wav"
    }
    if (Test-Path $xpDir) {
        Get-ChildItem $xpDir -Filter *.png | ForEach-Object {
            $setupRes += ("/resource:" + $_.FullName + ",AIMSetup.XpSprites." + $_.Name)
        }
    }

    $setupExe = Join-Path $root "AIM_Setup.exe"
    $setupRsp = Join-Path $root "build_setup_args.rsp"
    $setupArgs = @(
        "/nologo",
        "/target:winexe",
        "/optimize+",
        "/utf8output",
        "/out:$setupExe",
        "/win32icon:$icoPath",
        "/r:System.dll",
        "/r:System.Core.dll",
        "/r:System.Drawing.dll",
        "/r:System.Windows.Forms.dll"
    ) + $setupRes + $setupCs

    $setupQuoted = $setupArgs | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }
    [System.IO.File]::WriteAllLines($setupRsp, $setupQuoted, [System.Text.Encoding]::UTF8)

    & $csc "@$setupRsp"
    $sExit = $LASTEXITCODE
    Remove-Item $setupRsp -Force -ErrorAction SilentlyContinue
    Remove-Item $gzPayload -Force -ErrorAction SilentlyContinue

    if ($sExit -ne 0) {
        throw "csc.exe failed building AIM_Setup.exe with exit code $sExit"
    }
    $sfi = Get-Item $setupExe
    Write-Host ("SUCCESS: Built " + $sfi.FullName + " (" + [Math]::Round($sfi.Length / 1024, 1) + " KB)!")
}
if ($lockedMain) {
    Remove-Item $compileExe -Force -ErrorAction SilentlyContinue
    Write-Host "NOTE: AIMClassic.exe in project folder could not be overwritten because it is running. Close AIMClassic.exe and re-run build.ps1, or install using AIM_Setup.exe."
}
