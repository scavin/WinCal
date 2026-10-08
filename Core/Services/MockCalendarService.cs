#if DEBUG
using System.Diagnostics;
using WinCal.Core.Models;

namespace WinCal.Core.Services;

/// <summary>
/// 模拟日历服务（仅限开发调试使用，生产构建不包含此服务）
/// </summary>
public class MockCalendarService : ICalendarService
{
    private static readonly string[] MockColors = { "#0078D4", "#4285F4", "#FC3D39", "#0F9D58", "#FF6D00" };

    public Task<List<CalendarEvent>> GetEventsAsync(DateTime start, DateTime end)
    {
        var events = new List<CalendarEvent>();
        var today = DateTime.Today;
        var rng = new Random();

        // 为未来 14 天生成随机事件
        for (int i = 0; i < 14; i++)
        {
            var date = today.AddDays(i);
            var eventCount = rng.Next(0, 4); // 每天 0~3 个事件

            for (int j = 0; j < eventCount; j++)
            {
                var hour = rng.Next(8, 18);
                var minute = rng.Next(0, 4) * 15; // 0, 15, 30, 45
                var durationHours = rng.Next(1, 4);
                var isAllDay = rng.Next(10) < 2; // 20% 概率全天事件

                events.Add(new CalendarEvent(
                    Title: GetRandomEventTitle(rng, date),
                    StartTime: isAllDay ? date : date.AddHours(hour).AddMinutes(minute),
                    EndTime: isAllDay ? date.AddDays(1) : date.AddHours(hour + durationHours).AddMinutes(minute),
                    IsAllDay: isAllDay,
                    CalendarName: GetRandomCalendar(rng),
                    Color: MockColors[rng.Next(MockColors.Length)],
                    Location: rng.Next(3) < 1 ? "会议室 A" : "",
                    Description: ""
                ));
            }
        }

        return Task.FromResult(events.Where(e => e.StartTime >= start && e.StartTime < end).ToList());
    }

    public Task<bool> IsAvailableAsync() => Task.FromResult(true);

    public Task<List<CalendarAccountInfo>> GetCalendarAccountsAsync()
    {
        return Task.FromResult(new List<CalendarAccountInfo>
        {
            new("模拟账户", "工作日历", "mock-work", "#0078D4", true),
            new("模拟账户", "个人日历", "mock-personal", "#4285F4", true),
        });
    }

    public Task ForceRefreshAsync() => Task.CompletedTask;

    public void OpenSystemCalendarApp()
    {
        Debug.WriteLine("WinCal: Mock - would open system calendar app.");
    }

    private static string GetRandomEventTitle(Random rng, DateTime date)
    {
        var titles = new[]
        {
            "团队周会", "产品评审", "季度总结", "1:1 会议", "午餐",
            "客户电话", "代码审查", "设计讨论", "项目排期", "培训"
        };
        return titles[rng.Next(titles.Length)];
    }

    private static string GetRandomCalendar(Random rng)
    {
        var calendars = new[] { "Outlook", "Google Calendar", "iCloud" };
        return calendars[rng.Next(calendars.Length)];
    }
}
#endif
