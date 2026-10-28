using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace STranslate.Controls;

/// <summary>内容出现时的小幅上浮淡入；不改变布局尺寸，遵循系统减少动画选项。</summary>
public static class SurfaceMotion
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(SurfaceMotion), new PropertyMetadata(false, OnEnabledChanged));

    private static readonly DependencyProperty TranslationProperty = DependencyProperty.RegisterAttached(
        "Translation", typeof(TranslateTransform), typeof(SurfaceMotion));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element) return;
        element.Loaded -= OnLoaded;
        element.Unloaded -= OnUnloaded;
        element.IsVisibleChanged -= OnVisibilityChanged;
        if ((bool)e.NewValue)
        {
            element.Loaded += OnLoaded;
            element.Unloaded += OnUnloaded;
            element.IsVisibleChanged += OnVisibilityChanged;
        }
        else Stop(element);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e) => Reveal((FrameworkElement)sender);
    private static void OnUnloaded(object sender, RoutedEventArgs e) => Stop((FrameworkElement)sender);
    private static void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        if (element.IsLoaded && (bool)e.NewValue) Reveal(element);
        else Stop(element);
    }

    private static void Reveal(FrameworkElement element)
    {
        Stop(element);
        if (!element.IsVisible || !SystemParameters.ClientAreaAnimation) return;

        // 只接管没有其它变换的内容容器，避免与缩放/拖动等业务变换冲突。
        var translation = element.GetValue(TranslationProperty) as TranslateTransform;
        if (translation == null)
        {
            if (element.RenderTransform != Transform.Identity && !element.RenderTransform.Value.IsIdentity) return;
            translation = new TranslateTransform();
            element.SetValue(TranslationProperty, translation);
            element.RenderTransform = translation;
        }
        var duration = TimeSpan.FromMilliseconds(180);
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        translation.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(6, 0, duration) { EasingFunction = easing, FillBehavior = FillBehavior.Stop });
        element.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, duration) { EasingFunction = easing, FillBehavior = FillBehavior.Stop });
    }

    private static void Stop(FrameworkElement element)
    {
        element.BeginAnimation(UIElement.OpacityProperty, null);
        if (element.GetValue(TranslationProperty) is TranslateTransform translation)
            translation.BeginAnimation(TranslateTransform.YProperty, null);
    }
}
