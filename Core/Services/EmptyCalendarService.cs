using System.Diagnostics;
using WinCal.Core.Models;

namespace WinCal.Core.Services;

/// <summary>
/// 空日历服务（系统日历不可用或未配置 ICS URL 时的占位服务）
/// </summary>
public class EmptyCalendarService : ICalendarService
{
    public Task<List<CalendarEvent>> GetEventsAsync(DateTime start, DateTime end)
        => Task.FromResult(new List<CalendarEvent>());

    public Task<bool> IsAvailableAsync()
        => Task.FromResult(false);

    public Task<List<CalendarAccountInfo>> GetCalendarAccountsAsync()
        => Task.FromResult(new List<CalendarAccountInfo>());

    public Task ForceRefreshAsync()
        => Task.CompletedTask;

    public void OpenSystemCalendarApp()
    {
        try
        {
            // 尝试打开 Windows 日历应用
            Process.Start(new ProcessStartInfo("outlookcal:") { UseShellExecute = true });
        }
        catch
        {
            try
            {
                // 备用：打开 Windows 设置中的日历
                Process.Start(new ProcessStartInfo("ms-settings:calendar") { UseShellExecute = true });
            }
            catch
            {
                Debug.WriteLine("WinCal: Cannot open system calendar app.");
            }
        }
    }
}
