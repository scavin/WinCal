using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using WinCal.Core.Models;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using IcalEvent = Ical.Net.CalendarComponents.CalendarEvent;
using WinCalEvent = WinCal.Core.Models.CalendarEvent;

namespace WinCal.Core.Services;

/// <summary>
/// 远程 ICS 日历订阅服务 —— 从指定 URL 下载 .ics 文件并解析事件
/// </summary>
public class IcsCalendarService : ICalendarService
{
    private readonly string _icsUrl;
    private readonly int _refreshMinutes;
    private readonly string _calendarName;
    private readonly string _color;

    // 缓存原始 Calendar 对象（包含 RRULE 定义），不缓存展开后的事件
    private Calendar? _cachedCalendar;
    private DateTime _lastRefreshTime = DateTime.MinValue;
    private readonly HttpClient _httpClient;
    private readonly string _diskCachePath; // 磁盘缓存文件路径
    private bool _isBackgroundRefreshing;   // 防止重复后台刷新

    public IcsCalendarService(string icsUrl, int refreshMinutes = 30, string calendarName = "ICS 订阅", string color = "#0078D4")
    {
        _icsUrl = icsUrl ?? throw new ArgumentNullException(nameof(icsUrl));
        _refreshMinutes = refreshMinutes;
        _calendarName = calendarName;
        _color = color;

        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        // 模拟浏览器 User-Agent，避免某些服务器拒绝请求
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "miniCal/1.0 (Calendar Client)");

        // 磁盘缓存路径：%LOCALAPPDATA%/WinCal/cache/{url_hash}.ics
        var cacheDir = Path.Combine(AppSettings.AppDataDirectory, "cache");
        Directory.CreateDirectory(cacheDir);
        var urlHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_icsUrl)))[..16];
        _diskCachePath = Path.Combine(cacheDir, $"{urlHash}.ics");
    }

    public async Task<List<WinCalEvent>> GetEventsAsync(DateTime start, DateTime end)
    {
        await EnsureCacheAsync();

        if (_cachedCalendar == null)
            return new List<WinCalEvent>();

        // 使用 GetOccurrences 展开重复事件（RRULE）到指定日期范围
        var occurrences = _cachedCalendar.GetOccurrences(start, end);

        var events = new List<WinCalEvent>();
        foreach (var occurrence in occurrences)
        {
            try
            {
                var calEvent = ConvertOccurrence(occurrence);
                if (calEvent != null)
                    events.Add(calEvent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WinCal: Failed to convert ICS occurrence: {ex.Message}");
            }
        }

        // 去重：同日起始的事件，如果标题前缀相同（如"端午节"和"端午节（休）"），只保留跨度最长的版本
        var deduped = new List<WinCalEvent>();
        var groups = events.GroupBy(e => e.StartTime.Date);

        foreach (var group in groups)
        {
            // 按标题前缀分组：取第一个'（'之前的部分作为前缀
            var prefixGroups = group.ToList()
                .GroupBy(e => e.Title.Split('（', '(')[0]);

            foreach (var pg in prefixGroups)
            {
                // 保留跨度最长的（EndTime 最晚的）
                var best = pg.OrderByDescending(e => e.EndTime).First();
                deduped.Add(best);
            }
        }

        return deduped
            .OrderBy(e => e.StartTime)
            .ToList();
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (string.IsNullOrWhiteSpace(_icsUrl))
            return false;

        try
        {
            await EnsureCacheAsync();
            return _cachedCalendar != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<CalendarAccountInfo>> GetCalendarAccountsAsync()
    {
        var available = await IsAvailableAsync();
        return new List<CalendarAccountInfo>
        {
            new("ICS 订阅", _calendarName, "ics-subscription", _color, available)
        };
    }

    public async Task ForceRefreshAsync()
    {
        // 强制刷新：跳过所有缓存，直接从网络下载
        _isBackgroundRefreshing = false; // 取消可能正在进行的后台刷新
        await RefreshFromNetworkAsync();
    }

    public void OpenSystemCalendarApp()
    {
        // ICS 订阅没有对应的系统日历应用，尝试打开 URL
        try
        {
            Process.Start(new ProcessStartInfo(_icsUrl) { UseShellExecute = true });
        }
        catch { }
    }

    /// <summary>
    /// 确保缓存有效。优先级：内存缓存 → 磁盘缓存 → 网络下载。
    /// 磁盘缓存命中后，如已过期则在后台异步刷新（不阻塞调用方）。
    /// </summary>
    private async Task EnsureCacheAsync()
    {
        // 1. 内存缓存仍然有效 → 直接返回（最快路径）
        if (_cachedCalendar != null && (DateTime.Now - _lastRefreshTime).TotalMinutes < _refreshMinutes)
            return;

        // 2. 内存缓存为空 → 尝试从磁盘加载（快速路径）
        if (_cachedCalendar == null)
        {
            await LoadFromDiskCacheAsync();
        }

        // 3. 已有数据（内存或磁盘），检查是否需要后台刷新
        if (_cachedCalendar != null)
        {
            if ((DateTime.Now - _lastRefreshTime).TotalMinutes >= _refreshMinutes && !_isBackgroundRefreshing)
            {
                _ = RefreshFromNetworkAsync(); // 后台刷新，不阻塞
            }
            return;
        }

        // 4. 无任何缓存 → 必须同步网络下载（仅首次启动会发生）
        await RefreshFromNetworkAsync();
    }

    /// <summary>
    /// 从本地磁盘缓存文件加载 ICS 数据（毫秒级）
    /// </summary>
    private async Task LoadFromDiskCacheAsync()
    {
        try
        {
            if (!File.Exists(_diskCachePath))
                return;

            var icsContent = await File.ReadAllTextAsync(_diskCachePath);
            if (string.IsNullOrWhiteSpace(icsContent))
                return;

            _cachedCalendar = ParseIcsContent(icsContent);
            if (_cachedCalendar != null)
            {
                // 使用文件最后写入时间作为缓存时间
                _lastRefreshTime = File.GetLastWriteTime(_diskCachePath);
                Debug.WriteLine($"WinCal: Loaded ICS from disk cache ({_cachedCalendar.Events.Count} events, cached at {_lastRefreshTime:HH:mm:ss})");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WinCal: Failed to load disk cache: {ex.Message}");
        }
    }

    /// <summary>
    /// 从网络下载并更新缓存（内存 + 磁盘）
    /// </summary>
    private async Task RefreshFromNetworkAsync()
    {
        _isBackgroundRefreshing = true;
        try
        {
            var icsContent = await DownloadIcsAsync();
            var calendar = ParseIcsContent(icsContent);

            if (calendar != null)
            {
                _cachedCalendar = calendar;
                _lastRefreshTime = DateTime.Now;

                // 保存到磁盘缓存
                try
                {
                    await File.WriteAllTextAsync(_diskCachePath, icsContent);
                    Debug.WriteLine($"WinCal: ICS disk cache updated ({calendar.Events.Count} events)");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"WinCal: Failed to write disk cache: {ex.Message}");
                }

                Debug.WriteLine($"WinCal: ICS refreshed from network ({calendar.Events.Count} events)");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WinCal: Network refresh failed: {ex.Message}");
            // 保留旧缓存（如果有）
            if (_cachedCalendar == null)
                throw;
        }
        finally
        {
            _isBackgroundRefreshing = false;
        }
    }

    /// <summary>
    /// 下载远程 .ics 文件内容
    /// </summary>
    private async Task<string> DownloadIcsAsync()
    {
        var response = await _httpClient.GetAsync(_icsUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// 解析 ICS 文本为 Calendar 对象（保留 RRULE 等重复规则，不展开）
    /// </summary>
    private Calendar? ParseIcsContent(string icsContent)
    {
        try
        {
            var calendar = Calendar.Load(icsContent);
            Debug.WriteLine($"WinCal: Parsed ICS calendar with {calendar.Events.Count} event definitions");
            return calendar;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WinCal: Failed to parse ICS calendar: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 将一个 Occurrence（可能是重复事件的某一次出现）转换为 WinCal CalendarEvent
    /// </summary>
    private WinCalEvent? ConvertOccurrence(Occurrence occurrence)
    {
        if (occurrence.Source == null)
            return null;

        var icalEvent = occurrence.Source as IcalEvent;
        if (icalEvent == null)
            return null;

        var period = occurrence.Period;
        if (period?.StartTime == null)
            return null;

        var startTime = period.StartTime.AsSystemLocal;
        var isAllDay = icalEvent.IsAllDay;

        DateTime endTime;
        if (period.EndTime != null)
        {
            endTime = period.EndTime.AsSystemLocal;
        }
        else if (period.Duration != default)
        {
            endTime = startTime + period.Duration;
        }
        else
        {
            endTime = isAllDay ? startTime.AddDays(1) : startTime.AddHours(1);
        }

        // 全天事件：确保 EndTime > StartTime（单日全天事件 DTEND==DTSTART 时修正为次日）
        if (isAllDay && endTime.Date <= startTime.Date)
            endTime = startTime.AddDays(1);

        var title = !string.IsNullOrWhiteSpace(icalEvent.Summary)
            ? icalEvent.Summary
            : "(无标题)";

        var location = icalEvent.Location ?? "";
        var description = icalEvent.Description ?? "";

        return new WinCalEvent(
            Title: title,
            StartTime: startTime,
            EndTime: endTime,
            IsAllDay: isAllDay,
            CalendarName: _calendarName,
            Color: _color,
            Location: location,
            Description: description
        );
    }
}
