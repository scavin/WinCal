# WinminiCal — Windows 任务栏日历

> 一款轻量、优雅的 Windows 11 任务栏日历工具，灵感来自 macOS 平台的 Itsycal，支持显示事件列表

## 截图

<img width="1408" height="1482" alt="Screenshot-2026-06-16-07 08 33@2x" src="https://github.com/user-attachments/assets/7b6349e2-a639-44b1-88e9-d4a0ed3bd003" />

截图中的日历：https://k.appinn.com/fifa2026

## Emoji 国旗

Windows 中不支持显示 Emoji 国旗，所以使用了图片的方式，将国旗图片写入程序本身，200多个国家新增了来自 twemoji 的国旗图片，近 167KB。

---

## 功能特性

- 🗓️ **系统托盘日历** — 点击任务栏时间区域即可弹出月历面板
- 🔁 **系统日历替换** — 自动拦截 Windows 原生日历弹窗，替换为 WinCal 面板
- 📅 **农历显示** — 每个日期格子下方显示农历日期
- 📌 **事件展示** — 有事件的日期显示彩色圆点，面板下方列出近期日程
- 🔍 **事件详情** — 鼠标悬停事件项，左侧弹出详情（标题、时间、地点、描述）
- 🌐 **ICS 订阅** — 支持远程 .ics 日历订阅（Google Calendar、Outlook 等）
- 🎨 **主题跟随** — 深色 / 浅色主题自动跟随系统
- ⚙️ **丰富设置** — 周起始日、近期事件天数、字体大小、时间格式等

订阅日历可以参考：https://yangh9.github.io/ChinaCalendar/

## 技术栈

| 技术 | 说明 |
|------|------|
| C# 12 + .NET 8 | 主开发语言和运行时 |
| WPF | UI 框架，矢量渲染 + 自定义控件 |
| MVVM 架构 | CommunityToolkit.Mvvm |
| Hardcodet.NotifyIcon.Wpf | 系统托盘图标 |
| Ical.Net | ICS 日历文件解析 |
| SetWinEventHook | 系统日历窗口拦截 |

## 项目结构

```
WinCal/
├── App.xaml / App.xaml.cs          # 应用入口，托盘图标，拦截器
├── WinCal.csproj                   # 项目配置
│
├── Core/                           # 核心层
│   ├── Models/                     #   数据模型（CalendarEvent, CalendarDay）
│   ├── Services/                   #   日历服务（ICS、聚合、模拟、设置持久化）
│   └── Helpers/                    #   工具类（定位、主题、农历、拦截器等）
│
├── ViewModels/                     # ViewModel 层
│   ├── CalendarViewModel.cs        #   日历主逻辑
│   └── EventListViewModel.cs       #   事件列表逻辑
│
├── Views/                          # 视图层
│   ├── PopupWindow.xaml            #   弹出日历主窗口
│   ├── SettingsWindow.xaml         #   设置窗口
│   ├── EventDetailWindow.xaml      #   事件详情浮层
│   ├── Controls/                   #   自定义控件（月历、日期格子、事件项）
│   └── Themes/                     #   主题资源（Light.xaml, Dark.xaml）
│
├── publish.bat                     # 一键发布脚本
├── build.bat                       # 开发构建脚本（暂停查看输出）
└── build_nopause.bat               # 开发构建脚本（不暂停）
```

## 构建与运行

### 前置要求

- .NET 8 SDK
- Windows 10 1903+ 或 Windows 11
- Visual Studio 2022 或 VS Code + C# Dev Kit

### 开发调试

```bat
build.bat
```

### 发布单文件

```bat
publish.bat
```

产物：`dist/WinCal.exe`（单文件，约 50-70 MB，无需安装 .NET 运行时）

## 配置

设置文件位于 `%LOCALAPPDATA%\miniCal\settings.json`，支持：

- 界面主题（深色 / 浅色 / 跟随系统）
- 字体大小（5 档）
- 开机自启动
- 日历数据源（系统邮箱 / ICS URL）
- 周起始日（周日 / 周一）
- 近期事件天数（1 / 3 / 7 天）
- 农历显示开关
- ICS 刷新频率（15 分钟 / 30 分钟 / 1 小时）
- 时间显示格式（12 小时 / 24 小时 / 跟随系统）

## 开发文档

- [产品与开发方案](WinCal_产品与开发方案.md)
- [开发进度记录](PROGRESS.md)

## License

MIT
