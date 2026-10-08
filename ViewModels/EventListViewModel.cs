using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using WinCal.Core.Helpers;
using WinCal.Core.Models;

namespace WinCal.ViewModels;

/// <summary>
/// 事件列表 ViewModel：格式化事件显示文本、悬停详情
/// </summary>
public class EventListViewModel : INotifyPropertyChanged
{
    private CalendarEvent? _selectedEvent;
    private bool _isDetailVisible;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 当前选中/悬停的事件
    /// </summary>
    public CalendarEvent? SelectedEvent
    {
        get => _selectedEvent;
        set
        {
            _selectedEvent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetailTitle));
            OnPropertyChanged(nameof(DetailTime));
            OnPropertyChanged(nameof(DetailLocation));
            OnPropertyChanged(nameof(DetailDescription));
            OnPropertyChanged(nameof(DetailCalendar));
        }
    }

    /// <summary>
    /// 详情浮层是否可见
    /// </summary>
    public bool IsDetailVisible
    {
        get => _isDetailVisible;
        set { _isDetailVisible = value; OnPropertyChanged(); }
    }

    // 详情显示属性
    public string DetailTitle => SelectedEvent?.Title ?? string.Empty;
    public string DetailCalendar => SelectedEvent?.CalendarName ?? string.Empty;
    public string DetailLocation => SelectedEvent?.Location ?? string.Empty;
    public string DetailDescription => SelectedEvent?.Description ?? string.Empty;

    public string DetailTime
    {
        get
        {
            if (SelectedEvent == null) return string.Empty;
            return FormatEventTime(SelectedEvent);
        }
    }

    /// <summary>
    /// 格式化事件的相对日期标签（今天/明天/周X）
    /// </summary>
    public static string GetRelativeDayLabel(DateTime eventDate)
    {
        var today = DateTime.Today;
        var diff = (eventDate.Date - today).Days;

        if (LocalizationHelper.IsEnglish)
        {
            return diff switch
            {
                0 => "Today",
                1 => "Tomorrow",
                2 => "In 2 days",
                -1 => "Yesterday",
                _ when diff > 0 && diff < 7 => LocalizationHelper.CurrentCulture.DateTimeFormat
                    .GetDayName(eventDate.DayOfWeek),
                _ => eventDate.ToString("MMM d", LocalizationHelper.CurrentCulture)
            };
        }

        return diff switch
        {
            0 => "今天",
            1 => "明天",
            2 => "后天",
            -1 => "昨天",
            _ when diff > 0 && diff < 7 => CultureInfo.CurrentCulture.DateTimeFormat
                .GetDayName(eventDate.DayOfWeek),
            _ => eventDate.ToString("M月d日")
        };
    }

    /// <summary>
    /// 格式化事件时间显示
    /// </summary>
    public static string FormatEventTime(CalendarEvent evt)
    {
        if (evt.IsAllDay)
            return LocalizationHelper.GetString("Loc_AllDay");

        var start = evt.StartTime;
        var end = evt.EndTime;

        if (start.Date == end.Date)
        {
            // 同一天：显示 "10:00 - 11:00"
            return $"{start:HH:mm} - {end:HH:mm}";
        }

        // 跨天
        if (LocalizationHelper.IsEnglish)
        {
            return $"{start.ToString("MMM d HH:mm", LocalizationHelper.CurrentCulture)} - {end.ToString("MMM d HH:mm", LocalizationHelper.CurrentCulture)}";
        }

        return $"{start:M月d日 HH:mm} - {end:M月d日 HH:mm}";
    }

    /// <summary>
    /// 格式化事件在列表中的摘要文本
    /// </summary>
    public static string FormatEventSummary(CalendarEvent evt)
    {
        string time = evt.IsAllDay ? LocalizationHelper.GetString("Loc_AllDay") : evt.StartTime.ToString("HH:mm");
        return time;
    }

    /// <summary>
    /// 显示事件详情
    /// </summary>
    public void ShowDetail(CalendarEvent evt)
    {
        SelectedEvent = evt;
        IsDetailVisible = true;
    }

    /// <summary>
    /// 隐藏事件详情
    /// </summary>
    public void HideDetail()
    {
        IsDetailVisible = false;
        SelectedEvent = null;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
