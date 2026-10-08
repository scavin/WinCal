using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinCal.Core.Helpers;
using WinCal.Core.Models;
using WinCal.Core.Services;
using WinCal.ViewModels;
using WinCal.Views.Controls;

namespace WinCal.Views;

public partial class PopupWindow : Window
{
    private readonly CalendarViewModel _calendarViewModel;
    private readonly AppSettings _settings;
    private EventDetailWindow? _detailWindow;

    public PopupWindow()
    {
        // 加载设置
        _settings = AppSettings.Load();

        // 初始化 ViewModel
        _calendarViewModel = new CalendarViewModel();

        InitializeComponent();

        // 设置 DataContext
        DataContext = _calendarViewModel;

        // 绑定事件列表
        EventListControl.ItemsSource = _calendarViewModel.UpcomingEvents;

        // 订阅事件列表变化以更新空状态提示
        _calendarViewModel.UpcomingEvents.CollectionChanged += (_, _) => UpdateNoEventsVisibility();

        Loaded += OnLoaded;

        // 窗口尺寸变化时重新定位（异步加载事件后窗口变高）
        SizeChanged += OnSizeChanged;

        // 在 PopupWindow 级别捕获鼠标事件，显示事件详情
        PreviewMouseMove += OnPreviewMouseMove;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplySettings();
        UpdateNoEventsVisibility();
    }

    /// <summary>
    /// 窗口尺寸变化后重新定位，确保不超出屏幕底部
    /// </summary>
    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        WindowPositionHelper.PositionNearTaskbar(this);
    }

    /// <summary>
    /// 应用用户设置到界面
    /// </summary>
    private void ApplySettings()
    {
        // #1 主题
        ThemeHelper.ApplyTheme(_settings.ThemeMode);

        // #2 字体大小偏移
        ApplyFontSizeOffset();

        // #6 周起始日
        _calendarViewModel.WeekStartDay = _settings.WeekStartDay == Core.Services.WeekStartDay.Monday ? 1 : 0;
        Calendar.UpdateWeekHeaders(_calendarViewModel.WeekStartDay);
        _ = _calendarViewModel.RefreshDataAsync();

    }

    /// <summary>
    /// 应用字体大小偏移（基于基准字号 + offset）
    /// </summary>
    private void ApplyFontSizeOffset()
    {
        var offset = _settings.FontSizeOffset;
        if (offset == 0) return;

        // 用 ScaleTransform 缩放整个面板（因为 XAML 中大量硬编码 FontSize）
        // offset 范围 -2~+2，每级缩放 6%
        var scale = 1.0 + offset * 0.06;
        RootBorder.LayoutTransform = new ScaleTransform(scale, scale);
    }

    // Win32 API 用于焦点检测
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    private const uint GW_OWNER = 4;

    // 焦点丢失检测
    private DispatcherTimer? _focusTimer;
    private IntPtr _initialForegroundWindow; // 弹出时的前台窗口

    /// <summary>
    /// 启动焦点丢失检测
    /// 策略：记录弹出时的前台窗口，只有当前台窗口变为其他窗口时才关闭
    /// </summary>
    public void StartFocusTracking()
    {
        StopFocusTracking();

        // 记录当前前台窗口（通常是桌面/任务栏/ShellExperienceHost）
        _initialForegroundWindow = GetForegroundWindow();

        _focusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _focusTimer.Tick += OnFocusCheckTick;
        _focusTimer.Start();
    }

    private void StopFocusTracking()
    {
        if (_focusTimer != null)
        {
            _focusTimer.Stop();
            _focusTimer.Tick -= OnFocusCheckTick;
            _focusTimer = null;
        }
    }

    private void OnFocusCheckTick(object? sender, EventArgs e)
    {
        var foreground = GetForegroundWindow();
        var myHwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

        // 前台是我们自己 → 正常，不做任何操作
        if (foreground == myHwnd)
            return;

        // 前台是我们的子窗口（EventDetail）→ 正常
        var owner = GetWindow(foreground, GW_OWNER);
        if (owner == myHwnd)
            return;

        // 前台仍然是原来的窗口（桌面/任务栏）→ 用户还没点击其他地方，保持打开
        if (foreground == _initialForegroundWindow)
            return;

        // 前台变成了一个新的窗口 → 用户切换了焦点，关闭弹窗
        CloseDetailWindow();
        Hide();
        StopFocusTracking();
    }

    /// <summary>
    /// 失焦自动关闭（同时关闭详情窗口）
    /// 作为定时器检测的补充
    /// </summary>
    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        CloseDetailWindow();
        Hide();
        StopFocusTracking();
    }

    /// <summary>
    /// 鼠标移动时检测是否悬停在 EventItem 上
    /// </summary>
    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        var hitResult = VisualTreeHelper.HitTest(this, pos);
        if (hitResult == null)
        {
            CloseDetailWindow();
            return;
        }

        // 忽略右上角按钮区域
        if (IsMouseOverActionButtons(hitResult.VisualHit))
        {
            CloseDetailWindow();
            return;
        }

        var eventItem = FindAncestor<EventItem>(hitResult.VisualHit);
        if (eventItem?.EventData is CalendarEvent evt)
        {
            ShowDetailWindow(evt);
        }
        else
        {
            CloseDetailWindow();
        }
    }

    /// <summary>
    /// 判断鼠标是否在右上角操作按钮（设置、刷新、通知中心）上
    /// </summary>
    private bool IsMouseOverActionButtons(DependencyObject visualHit)
    {
        var btn = FindAncestor<Button>(visualHit);
        return btn == SettingsButton || btn == RefreshButton || btn == NotificationCenterButton;
    }

    /// <summary>
    /// 显示事件详情窗口（在主面板左侧）
    /// </summary>
    private void ShowDetailWindow(CalendarEvent evt)
    {
        if (_detailWindow == null)
        {
            _detailWindow = new EventDetailWindow();
            _detailWindow.ShowActivated = false; // 不抢焦点
        }

        _detailWindow.ShowEvent(evt, this);
    }

    /// <summary>
    /// 关闭详情窗口
    /// </summary>
    private void CloseDetailWindow()
    {
        if (_detailWindow != null)
        {
            _detailWindow.Hide();
        }
    }

    /// <summary>
    /// 隐藏时同时关闭详情
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        CloseDetailWindow();
        base.OnClosed(e);
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T ancestor)
                return ancestor;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    /// <summary>
    /// 更新事件列表区域的可见性（有事件显示列表，无事件显示占位文案）
    /// </summary>
    private void UpdateNoEventsVisibility()
    {
        var hasEvents = _calendarViewModel.UpcomingEvents.Count > 0;
        EventScrollViewer.Visibility = hasEvents ? Visibility.Visible : Visibility.Collapsed;
        NoEventsText.Visibility = hasEvents ? Visibility.Collapsed : Visibility.Visible;
        EndMarker.Visibility = hasEvents ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 点击刷新按钮强制刷新日历数据
    /// </summary>
    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        RefreshButton.IsEnabled = false;
        try
        {
            await _calendarViewModel.ForceRefreshAsync();
            UpdateNoEventsVisibility();
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// 点击打开系统通知中心与日历
    /// </summary>
    private void OnNotificationCenterClick(object sender, RoutedEventArgs e)
    {
        CloseDetailWindow();
        Hide();
        App.OpenNotificationCenter();
    }

    /// <summary>
    /// 点击齿轮图标打开设置窗口（模态）
    /// </summary>
    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        CloseDetailWindow();
        Hide(); // 先关闭弹窗，再打开独立设置窗口
        App.ShowSettings();
    }
}
