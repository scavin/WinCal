using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinCal.Core.Helpers;
using WinCal.Core.Models;
using WinCal.Core.Services;

namespace WinCal.ViewModels;

/// <summary>
/// 日历主 ViewModel：月份导航、日期网格生成、事件聚合
/// </summary>
public class CalendarViewModel : INotifyPropertyChanged
{
    private readonly ICalendarService _calendarService;

    private int _year;
    private int _month;
    private string _monthDisplay = string.Empty;
    private DateTime _selectedDate = DateTime.Today;
    private bool _isLoading;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 当前显示的年份
    /// </summary>
    public int Year
    {
        get => _year;
        set { _year = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 当前显示的月份（1~12）
    /// </summary>
    public int Month
    {
        get => _month;
        set { _month = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 月份显示文本，如 "2025年5月"
    /// </summary>
    public string MonthDisplay
    {
        get => _monthDisplay;
        set { _monthDisplay = value; OnPropertyChanged(); }
    }

    private int _selectedDayIndex = -1;

    /// <summary>
    /// 当前选中日期
    /// </summary>
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set { _selectedDate = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 选中日期在 CalendarDays 中的索引（-1 表示无选中）
    /// </summary>
    public int SelectedDayIndex
    {
        get => _selectedDayIndex;
        set { _selectedDayIndex = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 日历网格数据（6行7列 = 42天，含上下月补位）
    /// </summary>
    public ObservableCollection<CalendarDay> CalendarDays { get; } = new();

    /// <summary>
    /// 近期事件列表
    /// </summary>
    public ObservableCollection<CalendarEvent> UpcomingEvents { get; } = new();

    /// <summary>
    /// 周起始日（0=周日, 1=周一）
    /// </summary>
    public int WeekStartDay { get; set; } = 1; // 默认周一开始

    /// <summary>
    /// 近期事件天数范围
    /// </summary>
    public int UpcomingDays { get; set; } = 3;

    public CalendarViewModel() : this(CreateDefaultService())
    {
    }

    public CalendarViewModel(ICalendarService calendarService)
    {
        _calendarService = calendarService;

        // 从设置加载近期事件天数（避免使用硬编码默认值 3）
        var settings = AppSettings.Load();
        UpcomingDays = settings.UpcomingDays;

        NavigateToToday();
    }

    /// <summary>
    /// 根据用户设置创建日历服务
    /// </summary>
    private static ICalendarService CreateDefaultService()
    {
        var settings = AppSettings.Load();

        return settings.DataSource switch
        {
            DataSourceType.IcsUrl => CreateIcsService(settings),
            DataSourceType.Both => new AggregateCalendarService(
                CreateSystemService(),
                CreateIcsService(settings)),
            _ => CreateSystemService()
        };
    }

    private static ICalendarService CreateSystemService()
    {
        // 检查 WindowsCalendarService 是否可用（编译时可能被排除）
        try
        {
            var type = Type.GetType("WinCal.Core.Services.WindowsCalendarService, WinCal");
            if (type != null)
            {
                return (ICalendarService)Activator.CreateInstance(type)!;
            }
        }
        catch
        {
            // WinRT not available
        }

        return new EmptyCalendarService();
    }

    private static readonly string[] SubscriptionColors = {
        "#FF6D00", "#0078D4", "#E91E63", "#00897B", "#7B1FA2", "#C62828", "#2E7D32", "#F57F17"
    };

    private static ICalendarService CreateIcsService(AppSettings settings)
    {
        var urls = settings.IcsUrls?.Where(u => !string.IsNullOrWhiteSpace(u)).ToList() ?? new List<string>();

        if (urls.Count == 0)
            return new EmptyCalendarService();

        // 获取别名列表，与 URL 一一对应
        var aliases = settings.IcsAliases ?? new List<string>();

        if (urls.Count == 1)
        {
            var name = GetAlias(aliases, 0, urls[0]);
            return new IcsCalendarService(
                urls[0],
                settings.IcsRefreshMinutes,
                name,
                SubscriptionColors[0]);
        }

        // 多个 URL：创建多个服务并用 AggregateCalendarService 聚合
        var services = new List<ICalendarService>();
        for (int i = 0; i < urls.Count; i++)
        {
            var name = GetAlias(aliases, i, urls[i]);
            services.Add(new IcsCalendarService(
                urls[i],
                settings.IcsRefreshMinutes,
                name,
                SubscriptionColors[i % SubscriptionColors.Length]));
        }

        return new AggregateCalendarService(services.ToArray());
    }

    /// <summary>
    /// 获取订阅别名：优先使用用户设置的别名，否则从 URL 域名推断
    /// </summary>
    private static string GetAlias(List<string> aliases, int index, string url)
    {
        if (index < aliases.Count && !string.IsNullOrWhiteSpace(aliases[index]))
            return aliases[index];

        // 从 URL 推断默认名称
        try
        {
            var host = new Uri(url).Host;
            // 移除常见前缀
            if (host.StartsWith("calendar.")) host = host.Substring("calendar.".Length);
            if (host.StartsWith("www.")) host = host.Substring("www.".Length);
            if (host.StartsWith("cal.")) host = host.Substring("cal.".Length);
            return host;
        }
        catch
        {
            return $"订阅 {index + 1}";
        }
    }

    /// <summary>
    /// 导航到今天
    /// </summary>
    public void NavigateToToday()
    {
        Year = DateTime.Today.Year;
        Month = DateTime.Today.Month;
        SelectedDate = DateTime.Today;
        UpdateMonthDisplay();
        _ = RefreshDataAsync();
    }

    /// <summary>
    /// 上一月（不改变 SelectedDate，事件列表保持不变）
    /// </summary>
    public void NavigatePreviousMonth()
    {
        Month--;
        if (Month < 1)
        {
            Month = 12;
            Year--;
        }
        UpdateMonthDisplay();
        _ = RefreshDataAsync();
    }

    /// <summary>
    /// 下一月（不改变 SelectedDate，事件列表保持不变）
    /// </summary>
    public void NavigateNextMonth()
    {
        Month++;
        if (Month > 12)
        {
            Month = 1;
            Year++;
        }
        UpdateMonthDisplay();
        _ = RefreshDataAsync();
    }

    /// <summary>
    /// 选中某个日期
    /// </summary>
    public void SelectDate(DateTime date)
    {
        SelectedDate = date;

        // 更新选中索引
        SelectedDayIndex = -1;
        for (int i = 0; i < CalendarDays.Count; i++)
        {
            if (CalendarDays[i].Date.Date == date.Date)
            {
                SelectedDayIndex = i;
                break;
            }
        }

        // 刷新事件列表，显示选中日期起的事件
        _ = UpdateUpcomingEventsFromDateAsync(date);
    }

    /// <summary>
    /// 从指定日期开始更新近期事件列表（直接从服务获取，不受 CalendarDays 范围限制）
    /// </summary>
    private async Task UpdateUpcomingEventsFromDateAsync(DateTime fromDate)
    {
        UpcomingEvents.Clear();

        var endDate = fromDate.Date.AddDays(UpcomingDays);

        List<CalendarEvent> events;
        try
        {
            events = await _calendarService.GetEventsAsync(fromDate.Date, endDate);
        }
        catch
        {
            return;
        }

        // 按时间排序，取前 100 条
        var sorted = events
            .OrderBy(e => e.StartTime)
            .Take(100);

        foreach (var evt in sorted)
        {
            UpcomingEvents.Add(evt);
        }
    }

    /// <summary>
    /// 强制刷新（忽略缓存，重新拉取）
    /// </summary>
    public async Task ForceRefreshAsync()
    {
        await _calendarService.ForceRefreshAsync();
        await RefreshDataAsync();
    }

    /// <summary>
    /// 获取日历账户列表
    /// </summary>
    public Task<List<CalendarAccountInfo>> GetCalendarAccountsAsync()
    {
        return _calendarService.GetCalendarAccountsAsync();
    }

    /// <summary>
    /// 检查日历服务是否可用
    /// </summary>
    public Task<bool> IsCalendarAvailableAsync()
    {
        return _calendarService.IsAvailableAsync();
    }

    /// <summary>
    /// 打开系统日历应用
    /// </summary>
    public void OpenSystemCalendar()
    {
        _calendarService.OpenSystemCalendarApp();
    }

    /// <summary>
    /// 刷新所有数据：日历网格立即渲染，事件异步加载
    /// </summary>
    public async Task RefreshDataAsync()
    {
        // 第一阶段：立即生成日历网格（不依赖事件数据，纯日期计算）
        GenerateCalendarGrid(new List<CalendarEvent>());

        // 第二阶段：异步加载事件数据，再更新网格和事件列表
        IsLoading = true;
        try
        {
            var rangeStart = new DateTime(Year, Month, 1).AddDays(-7);
            var rangeEnd = new DateTime(Year, Month, 1).AddMonths(1).AddDays(7);

            List<CalendarEvent> events;
            try
            {
                events = await _calendarService.GetEventsAsync(rangeStart, rangeEnd);
            }
            catch
            {
                events = new List<CalendarEvent>();
            }

            // 用事件数据重新生成网格（更新事件点标记）
            GenerateCalendarGrid(events);

            // 生成近期事件列表
            GenerateUpcomingEvents();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 生成 6×7 日历网格
    /// </summary>
    private void GenerateCalendarGrid(List<CalendarEvent> events)
    {
        CalendarDays.Clear();

        var firstDayOfMonth = new DateTime(Year, Month, 1);
        int daysInMonth = DateTime.DaysInMonth(Year, Month);

        // 计算第一天在网格中的偏移（考虑周起始日）
        int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
        int offset = (firstDayOfWeek - WeekStartDay + 7) % 7;

        // 网格起始日期（上月补位）
        var gridStartDate = firstDayOfMonth.AddDays(-offset);

        // 填充 42 个格子（6行 × 7列）
        for (int i = 0; i < 42; i++)
        {
            var date = gridStartDate.AddDays(i);
            bool isCurrentMonth = date.Month == Month && date.Year == Year;
            bool isToday = date.Date == DateTime.Today;

            // 查找当天的事件
            var dayEvents = events.Where(e =>
                e.IsAllDay
                    ? date.Date >= e.StartTime.Date &&
                      date.Date < (e.EndTime.Date > e.StartTime.Date ? e.EndTime.Date : e.StartTime.Date.AddDays(1))
                    : date.Date == e.StartTime.Date
            ).ToList();

            // 获取农历文本
            string lunarText = LunarCalendarHelper.GetLunarDateText(date);

            CalendarDays.Add(new CalendarDay(
                Date: date,
                IsCurrentMonth: isCurrentMonth,
                IsToday: isToday,
                HasEvents: dayEvents.Count > 0,
                LunarDate: lunarText,
                Events: dayEvents
            ));
        }

        // 网格生成后重新计算选中索引
        UpdateSelectedDayIndex();
    }

    /// <summary>
    /// 根据 SelectedDate 在当前日历网格中的位置更新 SelectedDayIndex。
    /// 如果 SelectedDate 不在当前显示月份中，则不高亮任何日期（-1）。
    /// </summary>
    private void UpdateSelectedDayIndex()
    {
        SelectedDayIndex = -1;
        for (int i = 0; i < CalendarDays.Count; i++)
        {
            if (CalendarDays[i].Date.Date == SelectedDate.Date)
            {
                SelectedDayIndex = i;
                break;
            }
        }
    }

    /// <summary>
    /// 生成近期事件列表（以选中日期为基准，N 天内，最多 100 条）
    /// </summary>
    private async void GenerateUpcomingEvents()
    {
        UpcomingEvents.Clear();

        var fromDate = SelectedDate.Date;
        var endDate = fromDate.AddDays(UpcomingDays);

        List<CalendarEvent> events;
        try
        {
            events = await _calendarService.GetEventsAsync(fromDate, endDate);
        }
        catch
        {
            return;
        }

        // 按时间排序，取前 100 条
        var sorted = events
            .OrderBy(e => e.StartTime)
            .Take(100);

        foreach (var evt in sorted)
        {
            UpcomingEvents.Add(evt);
        }
    }

    private void UpdateMonthDisplay()
    {
        MonthDisplay = $"{Year}年{Month}月";
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
