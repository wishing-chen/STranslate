using STranslate.Plugin;
using STranslate.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace STranslate.Controls;

/// <summary>紧凑供应商下拉，服务列表只在展开时显示。</summary>
public partial class TranslationProviderBar : UserControl
{
    private ObservableCollection<Service>? _services;
    private readonly List<Service> _observed = [];
    public static readonly DependencyProperty SelectedProviderProperty = DependencyProperty.Register(
        nameof(SelectedProvider), typeof(Service), typeof(TranslationProviderBar));
    public Service? SelectedProvider
    {
        get => (Service?)GetValue(SelectedProviderProperty);
        private set => SetValue(SelectedProviderProperty, value);
    }
    public static readonly DependencyProperty SelectionLabelProperty = DependencyProperty.Register(
        nameof(SelectionLabel), typeof(string), typeof(TranslationProviderBar), new PropertyMetadata(""));
    public string SelectionLabel
    {
        get => (string)GetValue(SelectionLabelProperty);
        private set => SetValue(SelectionLabelProperty, value);
    }

    public TranslationProviderBar()
    {
        InitializeComponent();
        Menu.Indicator = Arrow;
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
        DataContextChanged += (_, _) => { if (IsLoaded) Attach(); };
    }

    private void Attach()
    {
        Detach();
        if (DataContext is MainWindowViewModel vm)
        {
            _services = vm.TranslateService.Services;
            _services.CollectionChanged += OnCollectionChanged;
        }
        ObserveServices();
    }

    private void Detach()
    {
        if (_services != null) _services.CollectionChanged -= OnCollectionChanged;
        foreach (var service in _observed) service.PropertyChanged -= OnServiceChanged;
        _observed.Clear();
        _services = null;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => ObserveServices();
    private void ObserveServices()
    {
        foreach (var service in _observed) service.PropertyChanged -= OnServiceChanged;
        _observed.Clear();
        if (_services != null)
            foreach (var service in _services)
            {
                _observed.Add(service);
                service.PropertyChanged += OnServiceChanged;
            }
        RefreshSelection();
    }

    private void OnServiceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Service.IsEnabled) or nameof(Service.DisplayName)) RefreshSelection();
    }

    private void RefreshSelection()
    {
        var enabled = _observed.Where(service => service.IsEnabled).ToArray();
        SelectedProvider = enabled.FirstOrDefault();
        SelectionLabel = SelectedProvider == null ? (TryFindResource("TranslationProviders") as string ?? "Providers")
            : SelectedProvider.DisplayName + (enabled.Length > 1 ? $" +{enabled.Length - 1}" : "");
    }
}