using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WinCal.Core.Helpers;
using WinCal.Core.Services;

namespace WinCal.Views;

/// <summary>
/// ICS URL 列表项
/// </summary>
public class IcsUrlItem
{
    public int Index { get; set; }
    public string FullUrl { get; set; } = "";
    public string DisplayUrl { get; set; } = "";
    public string Color { get; set; } = "#FF6D00";
    public string Alias { get; set; } = "";
}

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private bool _initialized = false;
    private readonly List<IcsUrlItem> _icsUrls = new();

    private static readonly string[] FontSizeLabels = { "最小", "较小", "标准", "较大", "最大" };
    private static readonly int[] IcsRefreshValues = { 10, 30, 60, 120 };
    private static readonly string[] SubscriptionColors = {
        "#FF6D00", "#0078D4", "#E91E63", "#00897B", "#7B1FA2", "#C62828", "#2E7D32", "#F57F17"
    };

    public SettingsWindow()
    {
        _settings = AppSettings.Load();

        InitializeComponent();
        LoadSettings();
        _initialized = true;
    }

    /// <summary>
    /// 将保存的设置值加载到 UI 控件
    /// </summary>
    private void LoadSettings()
    {
        // #1 颜色主题
        ThemeComboBox.SelectedIndex = (int)_settings.ThemeMode;

        // #2 字体大小
        FontSizeSlider.Value = _settings.FontSizeOffset;
        UpdateFontSizeLabel();

        // #3 开机自启动
        AutoStartupCheckBox.IsChecked = _settings.AutoStartup;

        // 替换系统任务栏日历
        InterceptCalendarCheckBox.IsChecked = _settings.InterceptTaskbarCalendar;

        // #4 数据源
        DataSourceComboBox.SelectedIndex = (int)_settings.DataSource;
        UpdateIcsPanelVisibility();

        // 加载 ICS URL 列表（含别名）
        _icsUrls.Clear();
        if (_settings.IcsUrls != null)
        {
            for (int i = 0; i < _settings.IcsUrls.Count; i++)
            {
                var alias = (_settings.IcsAliases != null && i < _settings.IcsAliases.Count)
                    ? _settings.IcsAliases[i] : "";
                _icsUrls.Add(new IcsUrlItem
                {
                    Index = i,
                    FullUrl = _settings.IcsUrls[i],
                    DisplayUrl = ShortenUrl(_settings.IcsUrls[i]),
                    Color = SubscriptionColors[i % SubscriptionColors.Length],
                    Alias = alias
                });
            }
        }
        RefreshIcsUrlList();

        var refreshIdx = Array.IndexOf(IcsRefreshValues, _settings.IcsRefreshMinutes);
        IcsRefreshCombo.SelectedIndex = refreshIdx >= 0 ? refreshIdx : 1;

        // #5 近期事件天数
        UpcomingDaysCombo.SelectedIndex = _settings.UpcomingDays switch
        {
            1 => 0,
            7 => 2,
            _ => 1  // 3 天默认
        };

        // #6 周起始日
        WeekStartSunday.IsChecked = _settings.WeekStartDay == WeekStartDay.Sunday;
        WeekStartMonday.IsChecked = _settings.WeekStartDay == WeekStartDay.Monday;

        // 字体大小滑块事件
        FontSizeSlider.ValueChanged += (_, _) => UpdateFontSizeLabel();
    }

    /// <summary>
    /// 截短 URL 显示（保留域名和路径首尾）
    /// </summary>
    private static string ShortenUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        if (url.Length <= 60) return url;

        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            if (path.Length > 30)
                path = path.Substring(0, 15) + "..." + path.Substring(path.Length - 10);
            return $"{uri.Host}{path}";
        }
        catch
        {
            return url.Substring(0, 30) + "...";
        }
    }

    /// <summary>
    /// 刷新 ICS URL 列表控件
    /// </summary>
    private void RefreshIcsUrlList()
    {
        if (IcsUrlList == null) return;
        IcsUrlList.ItemsSource = null;
        IcsUrlList.ItemsSource = _icsUrls;
    }

    /// <summary>
    /// 添加 ICS URL
    /// </summary>
    private void OnAddIcsUrl(object sender, RoutedEventArgs e)
    {
        var url = NewIcsUrlTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(url)) return;

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            MessageBox.Show("请输入有效的 HTTP/HTTPS 链接", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var idx = _icsUrls.Count;
        _icsUrls.Add(new IcsUrlItem
        {
            Index = idx,
            FullUrl = url,
            DisplayUrl = ShortenUrl(url),
            Color = SubscriptionColors[idx % SubscriptionColors.Length]
        });

        NewIcsUrlTextBox.Text = "";
        RefreshIcsUrlList();
    }

    /// <summary>
    /// 删除 ICS URL
    /// </summary>
    private void OnRemoveIcsUrl(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int idx)
        {
            var item = _icsUrls.FirstOrDefault(u => u.Index == idx);
            if (item != null)
            {
                _icsUrls.Remove(item);
                // 重新编号和分配颜色
                for (int i = 0; i < _icsUrls.Count; i++)
                {
                    _icsUrls[i].Index = i;
                    _icsUrls[i].Color = SubscriptionColors[i % SubscriptionColors.Length];
                }
                RefreshIcsUrlList();
            }
        }
    }

    private void UpdateFontSizeLabel()
    {
        var idx = (int)FontSizeSlider.Value + 2; // -2..2 → 0..4
        FontSizeLabel.Text = FontSizeLabels[idx];
    }

    private void OnDataSourceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        UpdateIcsPanelVisibility();
    }

    private void UpdateIcsPanelVisibility()
    {
        if (IcsPanel == null || DataSourceComboBox == null) return;
        var source = (DataSourceType)DataSourceComboBox.SelectedIndex;
        IcsPanel.Visibility = source == DataSourceType.IcsUrl || source == DataSourceType.Both
            ? Visibility.Visible
            : Visibility.Collapsed;
        if (SystemCalendarTip != null)
        {
            SystemCalendarTip.Visibility = source == DataSourceType.SystemCalendar || source == DataSourceType.Both
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// 从 UI 控件收集值并保存
    /// </summary>
    private void OnSave(object sender, RoutedEventArgs e)
    {
        // #1 颜色主题
        _settings.ThemeMode = (ThemeMode)ThemeComboBox.SelectedIndex;

        // 立即应用主题切换
        ThemeHelper.ApplyTheme(_settings.ThemeMode);

        // #2 字体大小
        _settings.FontSizeOffset = (int)FontSizeSlider.Value;

        // #3 开机自启动
        _settings.AutoStartup = AutoStartupCheckBox.IsChecked == true;
        ApplyAutoStartup(_settings.AutoStartup);

        // 替换系统任务栏日历
        _settings.InterceptTaskbarCalendar = InterceptCalendarCheckBox.IsChecked == true;

        // #4 数据源
        _settings.DataSource = (DataSourceType)DataSourceComboBox.SelectedIndex;
        _settings.IcsUrls = _icsUrls.Select(u => u.FullUrl).ToList();
        _settings.IcsAliases = _icsUrls.Select(u => u.Alias ?? "").ToList();
        _settings.IcsRefreshMinutes = IcsRefreshValues[IcsRefreshCombo.SelectedIndex];

        // #5 近期事件天数
        _settings.UpcomingDays = UpcomingDaysCombo.SelectedIndex switch
        {
            0 => 1,
            2 => 7,
            _ => 3
        };

        // #6 周起始日
        _settings.WeekStartDay = WeekStartMonday.IsChecked == true
            ? WeekStartDay.Monday
            : WeekStartDay.Sunday;

        // 持久化并更新拦截器状态
        _settings.Save();
        App.UpdateInterceptorState();

        Close();
    }

    /// <summary>
    /// 恢复默认值
    /// </summary>
    private void OnResetDefaults(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定恢复所有设置为默认值？", "恢复默认",
            MessageBoxButton.OKCancel, MessageBoxImage.Question);

        if (result != MessageBoxResult.OK) return;

        ThemeComboBox.SelectedIndex = (int)ThemeMode.FollowSystem;
        FontSizeSlider.Value = 0;
        AutoStartupCheckBox.IsChecked = false;
        InterceptCalendarCheckBox.IsChecked = true;
        DataSourceComboBox.SelectedIndex = (int)DataSourceType.SystemCalendar;
        _icsUrls.Clear();
        RefreshIcsUrlList();
        NewIcsUrlTextBox.Text = "";
        IcsRefreshCombo.SelectedIndex = 1;
        UpcomingDaysCombo.SelectedIndex = 1;
        WeekStartSunday.IsChecked = true;
        WeekStartMonday.IsChecked = false;
    }

    private void ApplyAutoStartup(bool enable)
    {
        try
        {
            if (enable)
                StartupHelper.Enable();
            else
                StartupHelper.Disable();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"设置开机自启动失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnFooterLinkClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://www.huangao.net") { UseShellExecute = true });
        }
        catch { }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnExitApp(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定退出 miniCal？", "退出程序",
            MessageBoxButton.OKCancel, MessageBoxImage.Question);

        if (result == MessageBoxResult.OK)
        {
            Application.Current.Shutdown();
        }
    }

    /// <summary>
    /// 允许拖动无边框窗口
    /// </summary>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        DragMove();
    }

    private void OnDeactivated(object sender, EventArgs e)
    {
        // 设置窗口不自动关闭，避免误操作丢失数据
    }
}
