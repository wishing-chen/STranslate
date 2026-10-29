using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace STranslate.Controls;

/// <summary>可打断的上浮浮层。服务仍由原命令独立启停，动画不修改服务模型。</summary>
public class ServiceSwitcherPopup : Popup
{
    private Window? _owner;
    private readonly Stopwatch _clock = new();
    private readonly List<FrameworkElement> _items = [];
    private double _progress, _from, _target, _duration;
    private bool _rendering, _restoreFocus, _finishing;

    public ServiceSwitcherPopup()
    {
        StaysOpen = true;
        PopupAnimation = PopupAnimation.None;
        Unloaded += (_, _) => FinishClose(false);
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded), typeof(bool), typeof(ServiceSwitcherPopup),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, e) => ((ServiceSwitcherPopup)d).Animate((bool)e.NewValue)));

    public bool CloseOnSelection { get; set; }
    public bool DropDown { get; set; }
    public RotateTransform? Indicator { get; set; }

    private void Animate(bool open)
    {
        if (_finishing) return;
        if (_rendering) AdvanceProgress();
        _from = _progress;
        _target = open ? 1 : 0;
        _duration = open ? 420 : 230;
        _clock.Restart();
        // 再次打开时先恢复命中状态，OnOpened 的焦点遍历才能看到可交互内容。
        if (Child != null) Child.IsHitTestVisible = open;
        if (open)
        {
            _restoreFocus = true;
            if (!IsOpen) SetCurrentValue(IsOpenProperty, true);
        }
        if (!SystemParameters.ClientAreaAnimation)
        {
            _progress = _target;
            ApplyFrame();
            StopRendering();
            if (!open) FinishClose(_restoreFocus);
            return;
        }
        ApplyFrame();
        if (!_rendering)
        {
            CompositionTarget.Rendering += OnRendering;
            _rendering = true;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _owner = Window.GetWindow(PlacementTarget);
        if (_owner != null)
        {
            _owner.PreviewMouseDown += OnOwnerMouseDown;
            _owner.PreviewKeyDown += OnOwnerKeyDown;
            _owner.Deactivated += OnOwnerDeactivated;
            _owner.IsVisibleChanged += OnOwnerVisibilityChanged;
        }
        if (Child != null)
        {
            Child.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnSelection));
            Child.UpdateLayout();
            _items.Clear();
            CollectItems(Child);
            ApplyFrame();
            Child.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }
    }

    private void CollectItems(DependencyObject node)
    {
        if (node is ContentPresenter presenter && presenter.DataContext is ViewModels.ServiceQuickAccessItem or STranslate.Plugin.Service)
        {
            presenter.RenderTransform = new TranslateTransform();
            _items.Add(presenter);
            return;
        }
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            CollectItems(VisualTreeHelper.GetChild(node, i));
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        AdvanceProgress();
        ApplyFrame();
        if (_clock.Elapsed.TotalMilliseconds < _duration && SystemParameters.ClientAreaAnimation) return;
        _progress = _target;
        ApplyFrame();
        StopRendering();
        if (!IsExpanded) FinishClose(_restoreFocus);
    }

    private void AdvanceProgress()
    {
        var t = Math.Clamp(_clock.Elapsed.TotalMilliseconds / _duration, 0, 1);
        var remaining = Math.Pow(1 - t, 3);
        var eased = _target > _from ? 1 - remaining * Math.Cos(t * Math.PI * .35) : 1 - remaining;
        _progress = _from + (_target - _from) * eased;
    }

    private void ApplyFrame()
    {
        if (Indicator != null) Indicator.Angle = _progress * 180;
        if (Child == null) return;
        Child.Opacity = Math.Min(1, _progress * 3);
        if (Child.RenderTransform is not TranslateTransform)
            Child.RenderTransform = new TranslateTransform();
        ((TranslateTransform)Child.RenderTransform).Y = (1 - _progress) * (DropDown ? -12 : 12);
        for (var i = 0; i < _items.Count; i++)
        {
            // 延迟作用于已缓动的进度，保持参考动画的固定圆钮尺寸。
            var rank = DropDown ? i : _items.Count - 1 - i;
            var delay = Math.Min(rank * .035, .21);
            var t = Math.Clamp((_progress - delay) / (1 - delay), 0, 1);
            _items[i].Opacity = Math.Min(1, t * 1.5);
            ((TranslateTransform)_items[i].RenderTransform).Y = (1 - t) * (DropDown ? -12 : 24 + Math.Min(rank, 4) * 12);
        }
    }

    private void RequestClose(bool restoreFocus)
    {
        _restoreFocus = restoreFocus;
        SetCurrentValue(IsExpandedProperty, false);
        GetBindingExpression(IsExpandedProperty)?.UpdateSource();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RequestClose(true);
            e.Handled = true;
        }
        else base.OnPreviewKeyDown(e);
    }

    private void OnOwnerKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        RequestClose(true);
        e.Handled = true;
    }

    private void OnOwnerMouseDown(object sender, MouseButtonEventArgs e)
    {
        // 开关本身负责反向播放；其它点击照常传递，不抢回用户刚选择的焦点。
        if (PlacementTarget?.IsMouseOver != true) RequestClose(false);
    }

    private void OnOwnerDeactivated(object? sender, EventArgs e) => RequestClose(false);
    private void OnOwnerVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is false) FinishClose(false);
    }

    private void OnSelection(object sender, RoutedEventArgs e)
    {
        if (CloseOnSelection && e.OriginalSource is ButtonBase)
            Dispatcher.InvokeAsync(() => RequestClose(true), DispatcherPriority.Input);
    }

    private void StopRendering()
    {
        CompositionTarget.Rendering -= OnRendering;
        _rendering = false;
        _clock.Stop();
    }

    private void FinishClose(bool restoreFocus)
    {
        if (_finishing) return;
        _finishing = true;
        var owner = _owner;
        StopRendering();
        _progress = 0;
        if (Indicator != null) Indicator.Angle = 0;
        SetCurrentValue(IsOpenProperty, false);
        if (IsExpanded) SetCurrentValue(IsExpandedProperty, false);
        _finishing = false;
        if (restoreFocus && owner?.IsActive == true)
        {
            if (owner.FindName("PART_Input") is InputControl { IsVisible: true } input && input.Focus())
                return;

            // 输入框被隐藏时，回到真正接收键盘输入的内部按钮。
            if (PlacementTarget is IconButton button)
            {
                button.ApplyTemplate();
                var part = button.Type == IconButton.IconButtonType.Toggle ? "PART_ToggleButton" : "PART_Button";
                if (button.Template?.FindName(part, button) is UIElement target && target.Focus()) return;
            }
            PlacementTarget?.Focus();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        StopRendering();
        _progress = 0;
        if (Indicator != null) Indicator.Angle = 0;
        if (IsExpanded && !_finishing)
        {
            _finishing = true;
            SetCurrentValue(IsExpandedProperty, false);
            GetBindingExpression(IsExpandedProperty)?.UpdateSource();
            _finishing = false;
        }
        if (_owner != null)
        {
            _owner.PreviewMouseDown -= OnOwnerMouseDown;
            _owner.PreviewKeyDown -= OnOwnerKeyDown;
            _owner.Deactivated -= OnOwnerDeactivated;
            _owner.IsVisibleChanged -= OnOwnerVisibilityChanged;
            _owner = null;
        }
        Child?.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnSelection));
        _items.Clear();
        base.OnClosed(e);
    }
}
