using System.Globalization;
using System.Windows;
using WinCal.Core.Services;

namespace WinCal.Core.Helpers;

/// <summary>
/// 界面多语言与本地化管理服务
/// </summary>
public static class LocalizationHelper
{
    private static AppLanguage _currentLanguage = AppLanguage.FollowSystem;
    private static bool _isEnglish;
    private static CultureInfo _currentCulture = CultureInfo.CurrentCulture;

    public static AppLanguage CurrentLanguage => _currentLanguage;
    public static bool IsEnglish => _isEnglish;
    public static CultureInfo CurrentCulture => _currentCulture;

    private static readonly Dictionary<string, string> ZhStrings = new()
    {
        // 托盘与应用
        ["Loc_AppName"] = "miniCal",
        ["Loc_OpenNotificationCenter"] = "打开 Windows 通知中心 (Win+N)",
        ["Loc_Settings"] = "设置",
        ["Loc_Exit"] = "退出",
        ["Loc_ErrorTitle"] = "miniCal 错误",
        ["Loc_ErrorLogged"] = "错误已写入桌面 minical_error.log",

        // 主面板
        ["Loc_UpcomingEvents"] = "近期事件",
        ["Loc_RefreshCalendar"] = "刷新日历",
        ["Loc_NoUpcomingEvents"] = "暂无近期事件",
        ["Loc_NoEventsTip"] = "若使用 Outlook 桌面经典版，请在设置中通过 ICS 订阅链接添加",

        // 月历
        ["Loc_Today"] = "今天",

        // 设置窗口
        ["Loc_SettingsTitle"] = "miniCal 设置",
        ["Loc_SettingsHeader"] = "⚙ 设置",
        ["Loc_Language"] = "界面语言",
        ["Loc_Lang_FollowSystem"] = "跟随系统 (Auto)",
        ["Loc_Lang_English"] = "English",
        ["Loc_Lang_SimplifiedChinese"] = "简体中文",
        ["Loc_Theme"] = "颜色主题",
        ["Loc_Theme_FollowSystem"] = "跟随系统",
        ["Loc_Theme_Light"] = "浅色",
        ["Loc_Theme_Dark"] = "深色",
        ["Loc_FontSize"] = "字体大小",
        ["Loc_AutoStartup"] = "开机自启动",
        ["Loc_InterceptCalendar"] = "替换系统任务栏日历",
        ["Loc_InterceptCalendarDesc"] = "点击任务栏时间时弹出 WinCal；关闭后原生日历和通知中心正常弹出",
        ["Loc_DataSource"] = "日历数据源",
        ["Loc_DataSource_System"] = "系统日历",
        ["Loc_DataSource_Ics"] = "ICS 订阅链接",
        ["Loc_DataSource_Both"] = "两者都用",
        ["Loc_SystemCalendarTipTitle"] = "💡 说明：",
        ["Loc_SystemCalendarTipContent"] = "系统日历读取 Windows 自带「日历」应用数据。若你使用 Outlook 桌面经典版（Exchange / MAPI），其日程存储于独立数据文件（.ost），暂不支持直接读取。请通过「ICS 订阅链接」添加，或将账户同步至 Windows 自带日历中。",
        ["Loc_IcsAliasTip"] = "为此订阅设置一个显示名称",
        ["Loc_IcsUrlTip"] = "输入 .ics 日历订阅链接",
        ["Loc_Add"] = "添加",
        ["Loc_RefreshInterval"] = "刷新频率",
        ["Loc_Min10"] = "10 分钟",
        ["Loc_Min30"] = "30 分钟",
        ["Loc_Min60"] = "60 分钟",
        ["Loc_Min120"] = "120 分钟",
        ["Loc_UpcomingDaysRange"] = "近期事件显示范围",
        ["Loc_Day1"] = "1 天",
        ["Loc_Day3"] = "3 天",
        ["Loc_Day7"] = "7 天",
        ["Loc_WeekStartDay"] = "周起始日",
        ["Loc_Sunday"] = "周日",
        ["Loc_Monday"] = "周一",
        ["Loc_RestoreDefaults"] = "恢复默认",
        ["Loc_ExitApp"] = "退出程序",
        ["Loc_Save"] = "保存",

        // 对话框与提示
        ["Loc_InvalidUrlMsg"] = "请输入有效的 HTTP/HTTPS 链接",
        ["Loc_PromptTitle"] = "提示",
        ["Loc_ResetDefaultsConfirm"] = "确定恢复所有设置为默认值？",
        ["Loc_ResetDefaultsTitle"] = "恢复默认",
        ["Loc_AutoStartupFail"] = "设置开机自启动失败：{0}",
        ["Loc_Error"] = "错误",
        ["Loc_ExitAppConfirm"] = "确定退出 miniCal？",
        ["Loc_ExitAppTitle"] = "退出程序",

        // 事件与订阅
        ["Loc_AllDay"] = "全天",
        ["Loc_NoTitle"] = "(无标题)",
        ["Loc_IcsSubscription"] = "ICS 订阅",
        ["Loc_SubscriptionPrefix"] = "订阅"
    };

    private static readonly Dictionary<string, string> EnStrings = new()
    {
        // 托盘与应用
        ["Loc_AppName"] = "miniCal",
        ["Loc_OpenNotificationCenter"] = "Open Windows Notification Center (Win+N)",
        ["Loc_Settings"] = "Settings",
        ["Loc_Exit"] = "Exit",
        ["Loc_ErrorTitle"] = "miniCal Error",
        ["Loc_ErrorLogged"] = "Error details written to desktop minical_error.log",

        // 主面板
        ["Loc_UpcomingEvents"] = "Upcoming Events",
        ["Loc_RefreshCalendar"] = "Refresh Calendar",
        ["Loc_NoUpcomingEvents"] = "No upcoming events",
        ["Loc_NoEventsTip"] = "If using classic Outlook desktop, add via ICS subscription in Settings",

        // 月历
        ["Loc_Today"] = "Today",

        // 设置窗口
        ["Loc_SettingsTitle"] = "miniCal Settings",
        ["Loc_SettingsHeader"] = "⚙ Settings",
        ["Loc_Language"] = "Language",
        ["Loc_Lang_FollowSystem"] = "Follow System (Auto)",
        ["Loc_Lang_English"] = "English",
        ["Loc_Lang_SimplifiedChinese"] = "简体中文",
        ["Loc_Theme"] = "Theme",
        ["Loc_Theme_FollowSystem"] = "Follow System",
        ["Loc_Theme_Light"] = "Light",
        ["Loc_Theme_Dark"] = "Dark",
        ["Loc_FontSize"] = "Font Size",
        ["Loc_AutoStartup"] = "Launch at startup",
        ["Loc_InterceptCalendar"] = "Replace taskbar calendar",
        ["Loc_InterceptCalendarDesc"] = "Clicking taskbar clock opens WinCal; when disabled, native calendar & notification center open normally",
        ["Loc_DataSource"] = "Calendar Data Source",
        ["Loc_DataSource_System"] = "System Calendar",
        ["Loc_DataSource_Ics"] = "ICS Subscription URL",
        ["Loc_DataSource_Both"] = "Both",
        ["Loc_SystemCalendarTipTitle"] = "💡 Note: ",
        ["Loc_SystemCalendarTipContent"] = "System calendar reads data from Windows built-in Calendar app. If you use classic Outlook desktop (Exchange / MAPI), its events are stored in a separate data file (.ost) and cannot be read directly. Please add via ICS subscription URL or sync your account to Windows Calendar.",
        ["Loc_IcsAliasTip"] = "Set a display name for this subscription",
        ["Loc_IcsUrlTip"] = "Enter .ics calendar subscription URL",
        ["Loc_Add"] = "Add",
        ["Loc_RefreshInterval"] = "Refresh Interval",
        ["Loc_Min10"] = "10 minutes",
        ["Loc_Min30"] = "30 minutes",
        ["Loc_Min60"] = "60 minutes",
        ["Loc_Min120"] = "120 minutes",
        ["Loc_UpcomingDaysRange"] = "Upcoming Events Range",
        ["Loc_Day1"] = "1 day",
        ["Loc_Day3"] = "3 days",
        ["Loc_Day7"] = "7 days",
        ["Loc_WeekStartDay"] = "First Day of Week",
        ["Loc_Sunday"] = "Sunday",
        ["Loc_Monday"] = "Monday",
        ["Loc_RestoreDefaults"] = "Restore Defaults",
        ["Loc_ExitApp"] = "Exit App",
        ["Loc_Save"] = "Save",

        // 对话框与提示
        ["Loc_InvalidUrlMsg"] = "Please enter a valid HTTP/HTTPS link",
        ["Loc_PromptTitle"] = "Notice",
        ["Loc_ResetDefaultsConfirm"] = "Are you sure you want to reset all settings to defaults?",
        ["Loc_ResetDefaultsTitle"] = "Reset Defaults",
        ["Loc_AutoStartupFail"] = "Failed to set launch at startup: {0}",
        ["Loc_Error"] = "Error",
        ["Loc_ExitAppConfirm"] = "Are you sure you want to exit miniCal?",
        ["Loc_ExitAppTitle"] = "Exit App",

        // 事件与订阅
        ["Loc_AllDay"] = "All day",
        ["Loc_NoTitle"] = "(No title)",
        ["Loc_IcsSubscription"] = "ICS Subscription",
        ["Loc_SubscriptionPrefix"] = "Subscription"
    };

    static LocalizationHelper()
    {
        ApplyLanguage(AppLanguage.FollowSystem);
    }

    /// <summary>
    /// 应用语言设置并更新应用程序资源字典
    /// </summary>
    public static void ApplyLanguage(AppLanguage language)
    {
        _currentLanguage = language;

        if (language == AppLanguage.FollowSystem)
        {
            var uiLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            _isEnglish = !uiLang.Equals("zh", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            _isEnglish = (language == AppLanguage.English);
        }

        _currentCulture = _isEnglish ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("zh-CN");

        var app = Application.Current;
        if (app != null)
        {
            var dict = _isEnglish ? EnStrings : ZhStrings;
            foreach (var (k, v) in dict)
            {
                app.Resources[k] = v;
            }
        }
    }

    /// <summary>
    /// 获取当前语言的格式化字符串
    /// </summary>
    public static string GetString(string key, params object[] args)
    {
        var dict = _isEnglish ? EnStrings : ZhStrings;
        if (dict.TryGetValue(key, out var val))
        {
            return args.Length > 0 ? string.Format(val, args) : val;
        }
        return key;
    }

    /// <summary>
    /// 获取字体大小滑块的标签数组
    /// </summary>
    public static string[] GetFontSizeLabels() => _isEnglish
        ? new[] { "Smallest", "Smaller", "Default", "Larger", "Largest" }
        : new[] { "最小", "较小", "标准", "较大", "最大" };
}
