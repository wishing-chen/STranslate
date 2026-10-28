param([Parameter(Mandatory = $true)][string]$InputPath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, WindowsBase
$repoRoot = Split-Path $PSScriptRoot -Parent
$imagePath = (Resolve-Path -LiteralPath $InputPath).Path
$image = [System.Windows.Media.Imaging.BitmapImage]::new()
$image.BeginInit()
$image.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
$image.UriSource = [Uri]::new($imagePath)
$image.EndInit()
$image.Freeze()

function Get-LogoFrame([int]$Size) {
    # 图标导出应用固定圆角蒙版，避免生成母版外缘的杂点进入托盘和通知。
    $visual = [System.Windows.Media.DrawingVisual]::new()
    [System.Windows.Media.RenderOptions]::SetBitmapScalingMode($visual, [System.Windows.Media.BitmapScalingMode]::HighQuality)
    $context = $visual.RenderOpen()
    $iconBounds = [System.Windows.Rect]::new($Size * 0.07, $Size * 0.07, $Size * 0.86, $Size * 0.86)
    $context.PushClip([System.Windows.Media.RectangleGeometry]::new($iconBounds, $Size * 0.20, $Size * 0.20))
    $scale = [Math]::Min($Size / $image.PixelWidth, $Size / $image.PixelHeight)
    $width = $image.PixelWidth * $scale
    $height = $image.PixelHeight * $scale
    $context.DrawImage($image, [System.Windows.Rect]::new(($Size - $width) / 2, ($Size - $height) / 2, $width, $height))
    $context.Pop()
    $context.Close()
    $bitmap = [System.Windows.Media.Imaging.RenderTargetBitmap]::new($Size, $Size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $bitmap.Freeze()
    return $bitmap
}

function Get-PngBytes([int]$Size) {
    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create((Get-LogoFrame $Size)))
    $stream = [IO.MemoryStream]::new()
    try { $encoder.Save($stream); return ,$stream.ToArray() }
    finally { $stream.Dispose() }
}

$resources = Join-Path $repoRoot 'src/STranslate/Resources'
Copy-Item -LiteralPath $imagePath -Destination (Join-Path $repoRoot 'images/logo-master.png') -Force
[IO.File]::WriteAllBytes((Join-Path $resources 'app-logo.png'), (Get-PngBytes 1024))
[IO.File]::WriteAllBytes((Join-Path $repoRoot 'src/STranslate/Images/app.png'), (Get-PngBytes 256))
[IO.File]::WriteAllBytes((Join-Path $repoRoot 'images/favicon.png'), (Get-PngBytes 512))

$sizes = @(16, 20, 24, 32, 40, 48, 64, 96, 128, 256)
$frames = @($sizes | ForEach-Object { ,(Get-PngBytes $_) })
$iconPath = Join-Path $resources 'app.ico'
$file = [IO.File]::Create($iconPath)
$writer = [IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($index = 0; $index -lt $sizes.Count; $index++) {
        $sizeByte = if ($sizes[$index] -eq 256) { 0 } else { $sizes[$index] }
        $writer.Write([byte]$sizeByte)
        $writer.Write([byte]$sizeByte)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $frames[$index].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
}
finally { $writer.Dispose(); $file.Dispose() }
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $resources 'updater.ico') -Force

$decoder = [System.Windows.Media.Imaging.IconBitmapDecoder]::new([Uri]::new($iconPath), [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
if ($decoder.Frames.Count -ne $sizes.Count) { throw 'ICO 帧数校验失败。' }
Write-Output ('Logo 格式转换完成；ICO 尺寸：' + (($decoder.Frames | ForEach-Object PixelWidth) -join ', '))
