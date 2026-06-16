using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinCal.Core.Helpers;
using WinCal.Core.Models;
using WinCal.ViewModels;

namespace WinCal.Views.Controls;

public partial class EventItem : UserControl
{
    public EventItem()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 事件数据
    /// </summary>
    public static readonly DependencyProperty EventDataProperty =
        DependencyProperty.Register(
            nameof(EventData), typeof(CalendarEvent), typeof(EventItem),
            new PropertyMetadata(null, OnEventDataChanged));

    public CalendarEvent? EventData
    {
        get => (CalendarEvent?)GetValue(EventDataProperty);
        set => SetValue(EventDataProperty, value);
    }

    /// <summary>
    /// 悬停详情回调
    /// </summary>
    public event Action<CalendarEvent?>? HoverDetailRequested;

    private static void OnEventDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (EventItem)d;
        if (e.NewValue is not CalendarEvent evt) return;

        // 标题后追加日期
        // 多天事件显示范围如 "劳动节(5月1日-5月5日)"，单天事件如 "房贷还款日(5月19日)"
        string datePart;
        var startDate = evt.StartTime.Date;
        var endDate = evt.IsAllDay ? evt.EndTime.Date.AddDays(-1) : evt.EndTime.Date;
        // 防止日期倒挂（无 DTEND 的全天事件 EndTime==StartTime，减一天后倒挂）
        if (endDate < startDate) endDate = startDate;
        if (startDate != endDate)
        {
            // 跨天事件：显示起止日期范围
            if (startDate.Year != endDate.Year)
                datePart = $"{startDate.Year}/{startDate.Month}/{startDate.Day}-{endDate.Year}/{endDate.Month}/{endDate.Day}";
            else
                datePart = $"{startDate.Month}月{startDate.Day}日-{endDate.Month}月{endDate.Day}日";
        }
        else
        {
            datePart = $"{startDate.Month}月{startDate.Day}日";
        }
        FlagEmojiTextRenderer.SetText(item.TitleText, $"{evt.Title}({datePart})");

        // 设置时间文本
        item.TimeText.Text = EventListViewModel.FormatEventSummary(evt);

        // 判断事件是否已结束
        var now = DateTime.Now;
        bool isPast = evt.IsAllDay
            ? evt.EndTime.Date <= DateTime.Today
            : evt.EndTime <= now;

        if (isPast)
        {
            // 已结束：灰色文字 + 删除线 + 降低不透明度
            item.TitleText.Foreground = Application.Current.TryFindResource("TextSecondaryBrush") as System.Windows.Media.Brush
                ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x76, 0x76, 0x76));
            item.TitleText.TextDecorations = TextDecorations.Strikethrough;
            item.TimeText.Foreground = Application.Current.TryFindResource("TextSecondaryBrush") as System.Windows.Media.Brush
                ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x76, 0x76, 0x76));
            item.TimeText.TextDecorations = TextDecorations.Strikethrough;
            item.Opacity = 0.5;
        }
        else
        {
            // 未结束：正常样式
            item.TitleText.Foreground = Application.Current.TryFindResource("TextPrimaryBrush") as System.Windows.Media.Brush
                ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x1A));
            item.TitleText.TextDecorations = null;
            item.TimeText.Foreground = Application.Current.TryFindResource("TextSecondaryBrush") as System.Windows.Media.Brush
                ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x76, 0x76, 0x76));
            item.TimeText.TextDecorations = null;
            item.Opacity = 1.0;
        }
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        HoverBorder.Background = Application.Current.TryFindResource("HoverBrush") as System.Windows.Media.Brush;
        HoverDetailRequested?.Invoke(EventData);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        HoverBorder.Background = null;
        HoverDetailRequested?.Invoke(null);
    }
}
