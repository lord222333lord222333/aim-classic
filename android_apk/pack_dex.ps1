param(
    [Parameter(Mandatory=$true)][string]$ApkPath,
    [Parameter(Mandatory=$true)][string]$DexPath,
    [Parameter(Mandatory=$true)][string]$AssetsRoot
)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::Open($ApkPath, [System.IO.Compression.ZipArchiveMode]::Update)
try {
    # Remove any old dex or backslash-polluted assets entries
    $toDelete = @($zip.Entries | Where-Object { $_.FullName -eq "classes.dex" -or $_.FullName.StartsWith("assets/") -or $_.FullName.StartsWith("assets\") })
    foreach ($e in $toDelete) { $e.Delete() }

    # Add classes.dex uncompressed (for ART zero-copy mmap + alignment)
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $zip,
        $DexPath,
        "classes.dex",
        [System.IO.Compression.CompressionLevel]::NoCompression
    ) | Out-Null

    # Add assets/ with strictly POSIX forward slashes '/' so Android AssetManager opens them on all devices
    $rootLen = $AssetsRoot.TrimEnd('\', '/').Length + 1
    Get-ChildItem -Path $AssetsRoot -File -Recurse | ForEach-Object {
        $rel = $_.FullName.Substring($rootLen).Replace('\', '/')
        $entryName = "assets/" + $rel
        $ext = $_.Extension.ToLowerInvariant()
        $level = if ($ext -in @(".png", ".jpg", ".jpeg", ".gif", ".webp")) {
            [System.IO.Compression.CompressionLevel]::NoCompression
        } else {
            [System.IO.Compression.CompressionLevel]::Optimal
        }
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $entryName, $level) | Out-Null
    }
} finally {
    $zip.Dispose()
}
