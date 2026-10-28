using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace STranslate.Controls;

/// <summary>翻译页常驻供应商栏，固定圆形图标错峰浮入，跟随系统动画设置。</summary>
public partial class TranslationProviderBar : UserControl
{
    public TranslationProviderBar() => InitializeComponent();

    private void OnProviderLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !SystemParameters.ClientAreaAnimation) return;
        var delay = TimeSpan.FromMilliseconds(Math.Min(Math.Max(0, Providers.Items.IndexOf(button.DataContext)), 8) * 35);
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var movement = new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(420))
        { BeginTime = delay, EasingFunction = easing, FillBehavior = FillBehavior.Stop };
        var opacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
        { BeginTime = delay, FillBehavior = FillBehavior.Stop };
        var transform = new TranslateTransform();
        button.RenderTransform = transform;
        transform.BeginAnimation(TranslateTransform.YProperty, movement);
        button.BeginAnimation(OpacityProperty, opacity);
    }
}
