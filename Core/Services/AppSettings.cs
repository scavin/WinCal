using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinCal.Core.Services;

/// <summary>
/// 应用设置持久化服务，存储到 %LOCALAPPDATA%/WinCal/settings.json
/// </summary>
public class AppSettings
{
    internal static readonly string AppDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCal");
    private static readonly string SettingsPath = Path.Combine(AppDataDirectory, "settings.json");
    private static readonly string LegacySettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "miniCal");
    private static readonly string LegacySettingsPath = Path.Combine(LegacySettingsDirectory, "settings.json");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // #1 颜色主题：浅色/深色/跟随系统
    public ThemeMode ThemeMode { get; set; } = ThemeMode.FollowSystem;

    // #2 字体大小偏移（-2, -1, 0, +1, +2）
    public int FontSizeOffset { get; set; } = 0;

    // #3 开机自启动
    public bool AutoStartup { get; set; } = false;

    // #12 替换系统任务栏时间弹窗（点击任务栏时间弹出 WinCal，关闭后保留系统原生日历与通知中心）
    public bool InterceptTaskbarCalendar { get; set; } = true;

    // #4 数据源类型
    public DataSourceType DataSource { get; set; } = DataSourceType.SystemCalendar;

    // #4 .ics 远程 URL 列表（支持多个订阅）
    public List<string> IcsUrls { get; set; } = new();

    // #4 .ics 订阅别名列表（与 IcsUrls 一一对应）
    public List<string> IcsAliases { get; set; } = new();

    // #4 .ics 刷新频率（分钟）
    public int IcsRefreshMinutes { get; set; } = 30;

    // #5 近期事件显示天数
    public int UpcomingDays { get; set; } = 3;

    // #6 周起始日
    public WeekStartDay WeekStartDay { get; set; } = WeekStartDay.Sunday;

    // #10 显示顶部时间日期
    public bool ShowHeaderDateTime { get; set; } = true;

    // #11 时间显示格式
    public TimeFormatMode TimeFormat { get; set; } = TimeFormatMode.FollowSystem;

    /// <summary>
    /// 加载设置（如果文件不存在则返回默认设置）
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            var path = GetSettingsPathForLoad();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WinCal: Failed to load settings: {ex}");
        }
        return new AppSettings();
    }

    /// <summary>
    /// 保存设置到文件
    /// </summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppDataDirectory);
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WinCal: Failed to save settings: {ex}");
        }
    }

    private static string GetSettingsPathForLoad()
    {
        if (File.Exists(SettingsPath) || !File.Exists(LegacySettingsPath))
            return SettingsPath;

        try
        {
            Directory.CreateDirectory(AppDataDirectory);
            File.Copy(LegacySettingsPath, SettingsPath, overwrite: false);
            File.Delete(LegacySettingsPath);

            if (!Directory.EnumerateFileSystemEntries(LegacySettingsDirectory).Any())
                Directory.Delete(LegacySettingsDirectory);

            System.Diagnostics.Debug.WriteLine("WinCal: Migrated settings from miniCal to WinCal.");
            return SettingsPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WinCal: Failed to migrate legacy settings: {ex}");
            return File.Exists(SettingsPath) ? SettingsPath : LegacySettingsPath;
        }
    }
}

public enum ThemeMode
{
    FollowSystem,
    Light,
    Dark
}

public enum DataSourceType
{
    SystemCalendar,
    IcsUrl,
    Both
}

public enum WeekStartDay
{
    Sunday,
    Monday
}

public enum TimeFormatMode
{
    FollowSystem,
    Hour12,
    Hour24
}
