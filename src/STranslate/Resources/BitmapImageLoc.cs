using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace STranslate.Resources;

internal class BitmapImageLoc
{
    private static readonly BitmapImage StatusLogo = LoadFrozen("app-logo.png", 128);

    public static readonly BitmapImage AppIcon = LoadFrozen("app-logo.png", 64);
    public static readonly BitmapImage DevIcon = CreateStatusIcon(TrayBadge.Development);
    public static readonly BitmapImage NoHotkeyIcon = CreateStatusIcon(TrayBadge.Paused);
    public static readonly BitmapImage IgnoreOnFullScreenIcon = CreateStatusIcon(TrayBadge.FullScreen);

    private enum TrayBadge { Development, Paused, FullScreen }

    private static BitmapImage LoadFrozen(string resourceName, int decodePixelWidth = 0)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri($"pack://application:,,,/Resources/{resourceName}");
        if (decodePixelWidth > 0) image.DecodePixelWidth = decodePixelWidth;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static BitmapImage CreateStatusIcon(TrayBadge badge)
    {
        // 64px 画布使 16/32px 托盘中的角标仍保留清楚的符号与边界。
        const int size = 64;
        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        using (var drawing = visual.RenderOpen())
        {
            var scale = Math.Min(size / (double)StatusLogo.PixelWidth, size / (double)StatusLogo.PixelHeight);
            var width = StatusLogo.PixelWidth * scale;
            var height = StatusLogo.PixelHeight * scale;
            drawing.DrawImage(StatusLogo, new Rect((size - width) / 2, (size - height) / 2, width, height));

            var white = FrozenBrush(Colors.White);
            var outline = new Pen(white, 3);
            outline.Freeze();

            if (badge == TrayBadge.Development)
            {
                // 开发状态用小圆点；与暂停及全屏的符号角标明确区分。
                drawing.DrawEllipse(FrozenBrush(Color.FromRgb(0x00, 0x75, 0xE8)), outline, new Point(53, 53), 7, 7);
            }
            else
            {
                var fill = badge == TrayBadge.Paused
                    ? Color.FromRgb(0xB8, 0x5A, 0x00)
                    : Color.FromRgb(0x44, 0x46, 0xB8);
                drawing.DrawEllipse(FrozenBrush(fill), outline, new Point(48, 48), 14, 14);

                var symbol = new Pen(white, 4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                symbol.Freeze();
                if (badge == TrayBadge.Paused)
                {
                    drawing.DrawLine(symbol, new Point(41, 48), new Point(55, 48));
                }
                else
                {
                    // 四角框表达全屏边界，避免与禁用热键的减号混淆。
                    DrawCorner(drawing, symbol, 40, 41, 1, 1);
                    DrawCorner(drawing, symbol, 56, 41, -1, 1);
                    DrawCorner(drawing, symbol, 40, 55, 1, -1);
                    DrawCorner(drawing, symbol, 56, 55, -1, -1);
                }
            }
        }

        var rendered = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rendered.Render(visual);
        rendered.Freeze();

        // 显式转回 BitmapImage，保留托盘组件既有的输入类型；OnLoad 后即可释放流。
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rendered));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;
        var result = new BitmapImage();
        result.BeginInit();
        result.CacheOption = BitmapCacheOption.OnLoad;
        result.StreamSource = stream;
        result.EndInit();
        result.Freeze();
        return result;
    }

    private static SolidColorBrush FrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static void DrawCorner(DrawingContext drawing, Pen pen, double x, double y, int horizontal, int vertical)
    {
        drawing.DrawLine(pen, new Point(x, y), new Point(x + horizontal * 5, y));
        drawing.DrawLine(pen, new Point(x, y), new Point(x, y + vertical * 5));
    }
}
