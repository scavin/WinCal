# WinCal 开发进度

> 最后更新：2026-05-07

## 总体进度：MVP + 设置界面已完成 ✅

---

## 已完成功能清单

### 系统托盘
- [x] 动态生成带今日日期数字的托盘图标
- [x] 左键点击弹出/收起日历面板
- [x] 右键菜单（设置、退出）
- [x] 面板贴近任务栏右下角定位

### 系统日历拦截（⭐ 新功能 2026-05-07）
- [x] 监听 `ShellExperienceHost` 进程的窗口显示事件（`SetWinEventHook`）
- [x] 检测到右下角系统日历/通知中心弹出时，立即隐藏并替换为 WinCal 面板
- [x] 防抖机制（500ms 冷却时间），避免重复触发
- [x] 多显示器支持（`MonitorFromWindow` 获取窗口所在显示器工作区）
- [x] 退出时自动恢复被隐藏的系统日历窗口
- [x] 弹出窗口焦点丢失检测（定时器轮询 + 前台窗口变化判断）

### 月历视图
- [x] 月历网格显示（6行7列）
- [x] 月份切换（上一月/下一月）
- [x] 今日蓝色圆形高亮
- [x] 非当月日期灰显
- [x] 农历显示（每格下方）
- [x] 自定义周起始日（当前默认周日开始）

### 事件展示
- [x] 有事件的日期底部彩色圆点标记
- [x] 圆点颜色与事件来源日历颜色一致
- [x] 近期事件列表（面板下方，分隔线隔开）
- [x] 事件列表固定高度，超出滚动
- [x] 无事件时列表区域自动隐藏
- [x] 事件项左侧颜色条标识
- [x] 事件项显示标题 + 时间摘要

### 事件详情
- [x] 鼠标悬停事件项时，面板左侧弹出详情浮层
- [x] 详情显示：标题、时间、地点、日历来源、描述
- [x] 详情窗口底部与主面板底部对齐
- [x] 鼠标移开自动关闭详情

### 面板体验
- [x] 圆角窗口（12px），圆角外完全透明
- [x] ClipToBounds 防止内容溢出圆角
- [x] 失焦自动关闭
- [x] 事件项 hover 圆角背景高亮（不遮挡面板圆角）

### 主题
- [x] 浅色主题资源字典
- [x] 深色主题资源字典
- [x] 自动跟随系统主题（监听注册表 `AppsUseLightTheme`）

### 设置界面（⭐ 新功能 2026-05-05 ~ 05-06）
- [x] 设置窗口 UI（独立顶层窗口）
- [x] 界面颜色主题（深色 / 浅色 / 跟随系统）
- [x] 界面字体大小（5 档切换）
- [x] 开机自启动开关
- [x] 日历数据源配置（系统邮箱 / 远程 .ics URL）
- [x] 周起始日（周日 / 周一）
- [x] 近期事件显示天数（1/3/7 天）
- [x] 农历显示开关
- [x] .ics 刷新频率（15分钟/30分钟/1小时）
- [x] 主界面顶部时间日期显示开关
- [x] 时间显示格式（12小时/24小时/跟随系统）
- [x] 设置持久化（JSON 文件 `%LOCALAPPDATA%/WinCal/settings.json`）

### 数据服务
- [x] 日历服务接口（ICalendarService）
- [x] 模拟数据服务（MockCalendarService，用于调试）
- [x] .ics 远程日历服务（IcsCalendarService，使用 Ical.Net 解析）
- [x] 聚合日历服务（AggregateCalendarService，多数据源合并）
- [x] 空日历服务（EmptyCalendarService，无数据源时的默认服务）
- [x] Windows 日历 API 接入（WindowsCalendarService，需 Windows SDK 暂未启用）

### 工具类
- [x] 窗口定位（WindowPositionHelper）
- [x] 开机自启注册表操作（StartupHelper）
- [x] 托盘图标动态生成（TrayIconGenerator）
- [x] 农历计算（LunarCalendarHelper）
- [x] 系统主题监听（ThemeHelper）
- [x] 系统日历拦截器（SystemCalendarInterceptor）

### 构建与发布
- [x] publish.bat 一键发布脚本
- [x] build.bat 开发构建脚本
- [x] Self-contained + SingleFile 发布配置
- [x] 移除 PublishTrimmed（.NET 8 SDK 对 WPF 不支持 Trim，且会破坏 SetWinEventHook 回调）

---

## 已完成文件清单

| 文件 | 说明 |
|------|------|
| `WinCal.csproj` | 项目配置 |
| `App.xaml` | 应用入口，托盘资源，主题引用 |
| `App.xaml.cs` | 托盘初始化、弹窗切换、拦截器集成 |
| `Core/Models/CalendarEvent.cs` | 事件数据模型 |
| `Core/Models/CalendarDay.cs` | 日期数据模型 |
| `Core/Models/CalendarAccountInfo.cs` | 日历账户信息模型 |
| `Core/Services/ICalendarService.cs` | 日历服务接口 |
| `Core/Services/WindowsCalendarService.cs` | Windows 日历 API（需 WinRT 暂未启用） |
| `Core/Services/MockCalendarService.cs` | 模拟数据服务 |
| `Core/Services/IcsCalendarService.cs` | .ics 远程日历服务 |
| `Core/Services/AggregateCalendarService.cs` | 多数据源聚合服务 |
| `Core/Services/EmptyCalendarService.cs` | 空日历服务 |
| `Core/Services/AppSettings.cs` | 设置持久化（JSON） |
| `Core/Helpers/WindowPositionHelper.cs` | 窗口定位 |
| `Core/Helpers/StartupHelper.cs` | 开机自启 |
| `Core/Helpers/TrayIconGenerator.cs` | 托盘图标生成 |
| `Core/Helpers/LunarCalendarHelper.cs` | 农历计算 |
| `Core/Helpers/ThemeHelper.cs` | 系统主题监听与切换 |
| `Core/Helpers/SystemCalendarInterceptor.cs` | 系统日历拦截器（⭐ 新增） |
| `ViewModels/CalendarViewModel.cs` | 日历 ViewModel |
| `ViewModels/EventListViewModel.cs` | 事件列表 ViewModel |
| `Views/PopupWindow.xaml` / `.cs` | 弹出日历主窗口 |
| `Views/SettingsWindow.xaml` / `.cs` | 设置窗口 |
| `Views/EventDetailWindow.xaml` / `.cs` | 事件详情浮层窗口 |
| `Views/Controls/MonthCalendar.xaml` / `.cs` | 月历控件 |
| `Views/Controls/DayCell.xaml` / `.cs` | 日期格子控件 |
| `Views/Controls/EventItem.xaml` / `.cs` | 事件项控件 |
| `Views/Controls/EventDetailPopup.xaml` / `.cs` | 事件详情弹出控件（已弃用，改用 EventDetailWindow） |
| `Views/Themes/Light.xaml` | 浅色主题 |
| `Views/Themes/Dark.xaml` | 深色主题 |
| `publish.bat` | 发布脚本 |
| `build.bat` | 构建脚本 |

---

## 待优化项

### 构建与发布命令（备忘）

开发调试构建（不暂停）：
```bat
build_nopause.bat
:: 实际执行: "C:\Program Files\dotnet\dotnet.exe" build -c Debug
```

开发调试构建（暂停查看输出）：
```bat
build.bat
:: 实际执行: "C:\Program Files\dotnet\dotnet.exe" build
```

发布 Release 单文件：
```bat
publish.bat
:: 实际执行: dotnet publish WinCal.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o ./dist
:: 注意：已移除 PublishTrimmed（.NET 8 对 WPF 不支持 Trim，且会破坏 SetWinEventHook 回调）
:: 产物: ./dist/WinCal.exe（单文件，无需 .NET 运行时）
```

> ⚠️ dotnet 必须使用完整路径 `"C:\Program Files\dotnet\dotnet.exe"`，部分终端环境变量中找不到 `dotnet`。

---

### 点选日期后近期事件展示逻辑

**核心入口**：`CalendarViewModel.SelectDate(DateTime date)`

**流程**：
1. 用户点击日历格子中的某一天 → `DayCell` 触发 `SelectDate`
2. `CalendarViewModel.SelectDate()` 更新 `SelectedDate`，计算 `SelectedDayIndex`（选中格子在网格中的位置）
3. 调用 `UpdateUpcomingEventsFromDateAsync(date)` 刷新事件列表：
   - 以选中日期为起点，向后查 `UpcomingDays` 天（默认 3 天，可在设置中改为 1/3/7）
   - 调用 `_calendarService.GetEventsAsync(fromDate, endDate)` 获取事件
   - 按开始时间排序，最多显示 100 条
   - 结果写入 `UpcomingEvents` ObservableCollection → UI 自动刷新
4. 月历导航（上一月/下一月）**不改变** `SelectedDate`，所以事件列表保持不变

**关键点**：
- 事件查询范围是 `[选中日期, 选中日期 + UpcomingDays)`，不受当前显示月份限制
- `UpcomingDays` 从 `AppSettings` 加载，默认 3
- 面板首次弹出时调用 `RefreshDataAsync()` → `GenerateUpcomingEvents()`，以 `SelectedDate`（默认今天）为基准
- 月份切换时 `RefreshDataAsync()` 也会重新调用 `GenerateUpcomingEvents()`，以当前 `SelectedDate` 为基准

---

### Bug 修复记录

#### 2026-05-07 ICS 农历节假日重复显示修复

**问题**：农历节假日（如"端午节"）在同一天显示两条事件——"端午节"和"端午节（休）"，导致事件列表重复。

**根因**：ICS 文件中同一个节日用两个 VEVENT 定义：
- `DTSTART;VALUE=DATE:20250531` → 端午节（单天）
- `DTSTART;VALUE=DATE:20250531 / DTEND;VALUE=DATE:20250602` → 端午节（休，3天假期）

两者标题前缀相同（"端午节"），`GetOccurrences` 展开后在同一天产生两个 Occurrence。

**修复方案**：在 `IcsCalendarService.GetEventsAsync()` 中添加去重逻辑：
- 同日同标题前缀的事件（取第一个 `（` 或 `(` 之前的部分），只保留 **跨度最长**（EndTime 最晚）的版本
- 效果：用户只看到"端午节（休）(5月31日-6月2日)"，不再重复

#### 2026-05-07 EventItem 全天事件日期倒挂修复

**问题**：无 DTEND 的全天事件（如单天"端午节"），`EndTime` 等于 `StartTime`，`EventItem` 中 `AddDays(-1)` 导致 `endDate < startDate`，显示异常。

**根因**：`EventItem.xaml.cs` 中对全天事件统一做 `endDate.AddDays(-1)` 调整（假设 End 是次日），但对单天事件（Start == End）会倒挂。

**修复方案**：在 `EventItem.xaml.cs` 中添加保护：
```csharp
if (endDate < startDate) endDate = startDate;
```

#### 2026-05-07 单日全天事件日历格子圆点缺失修复

**问题**：某些 ICS 节假日（如端午、中秋等单日节日）在日历格子中不显示事件圆点，但事件列表中能正常显示。

**根因**：
1. 部分 ICS 事件的 `DTEND == DTSTART`（如 `DTSTART;VALUE=DATE:20250531, DTEND;VALUE=DATE:20250531`），导致解析后 `EndTime == StartTime`
2. `CalendarViewModel.GenerateCalendarGrid()` 中全天事件匹配条件为 `date >= Start && date < End`，当 `End.Date == Start.Date` 时，条件 `date < End.Date` 永远为 false，事件无法匹配到任何日期

**修复方案**（三层防护）：

① `IcsCalendarService.ConvertOccurrence()`：当全天事件 `EndTime <= StartTime` 时，自动修正为 `EndTime = StartTime + 1天`：
```csharp
if (isAllDay && endTime.Date <= startTime.Date)
    endTime = startTime.AddDays(1);
```

② `CalendarViewModel.GenerateCalendarGrid()`：匹配逻辑增加防御性判断：
```csharp
date.Date < (e.EndTime.Date > e.StartTime.Date ? e.EndTime.Date : e.StartTime.Date.AddDays(1))
```

③ 即使 IcsCalendarService 已修正 EndTime，ViewModel 层仍做兜底，确保其他数据源（如 MockCalendarService）也不会出现此问题。

#### 2026-05-07 日历格子多圆点支持（最多3个）

**问题**：原设计每天只显示1个事件圆点，当同一天有多个不同来源的日历事件时无法区分。

**修复方案**：
1. `DayCell.xaml`：将单个 `Ellipse(EventDot)` 替换为 `StackPanel(EventDots)`，内含 `Dot1/Dot2/Dot3` 三个圆点
2. `DayCell.xaml.cs`：按事件颜色去重，最多显示3种不同颜色的圆点，每种颜色对应一个日历来源
3. 圆点尺寸 4×4，间距 2px，居中排列在日期格子底部

---

### 🔧 右键点击系统时间弹出 WinCal 菜单（暂不实现）

**原因**：Win11 任务栏用 XAML 重写，时钟窗口类名不同于 Win10，`WindowFromPoint` 定位困难。全局鼠标钩子 `WH_MOUSE_LL` 有性能风险（回调卡顿会导致鼠标卡顿），投入产出比不高。

### ✅ 近期事件加载延迟优化（已完成 2026-05-07）

**问题**：面板弹出时，近期事件列表需要等待 ICS 文件网络下载 + 解析后才显示，导致事件列表区域出现空白等待。

**实现方案**：磁盘缓存（`IcsCalendarService`）

**加载优先级**：内存缓存（毫秒级）→ 磁盘缓存（毫秒级）→ 网络下载（秒级）

**核心改动**：
1. `EnsureCacheAsync()` 拆分为三层加载策略：
   - 内存缓存有效 → 直接返回
   - 内存为空 → 从磁盘加载（`%LOCALAPPDATA%/WinCal/cache/{url_hash}.ics`）
   - 已有数据但过期 → 后台异步刷新（不阻塞 UI）
   - 无任何缓存 → 同步网络下载（仅首次启动）
2. 新增 `LoadFromDiskCacheAsync()`：从本地文件读取上次缓存的 ICS 内容
3. 新增 `RefreshFromNetworkAsync()`：网络下载 + 同时写入磁盘缓存
4. `ForceRefreshAsync()`：跳过所有缓存直接网络刷新

**效果**：应用启动后（非首次），面板弹出时事件列表秒级显示，后台按刷新间隔（默认 30 分钟）静默更新。

---

## 开发日志

### 2026-10-08

**解决与 Windows 11 原生通知中心冲突问题（Issue #3）**

1. **设置中增加「替换系统任务栏日历」开关（`InterceptTaskbarCalendar`）**：
   - 允许用户手动控制是否拦截系统任务栏时间弹窗。
   - 开启时接管任务栏时间点击；关闭时保留 Windows 11 原生日历与通知中心，用户可纯粹通过托盘图标使用 WinCal。
   - 切换并保存设置时即时生效，关闭时自动恢复被隐藏的系统原生窗口。
2. **支持并放行 `Win+N` 快捷键**：
   - 增加低级键盘钩子（`WH_KEYBOARD_LL`）侦测 `Win+N` 组合键按压；
   - 在拦截判断中增加 `Win+N` 按键状态检测与临时放行窗口（3秒），确保用户使用原生快捷键呼出 Windows 通知中心时不会被 WinCal 误拦截或隐藏。
3. **主面板与托盘增加通知中心入口**：
   - 主面板（`PopupWindow`）操作按钮栏新增「打开 Windows 通知中心 (Win+N)」图标按钮（小铃铛图标），方便用户查看系统历史通知。
   - 托盘右键菜单项文案优化为「打开 Windows 通知中心 (Win+N)」。
   - 放行或唤起系统通知中心时自动恢复 `SW_SHOW` 状态，防止被隐藏的原生窗口残留。

**修复系统日历失败回退虚假日程 Bug 及设置说明（Issue #2）**

1. **修复 `CreateSystemService` 兜底 Bug**：当 `WindowsCalendarService` 不可用或反射失败时，回退到 `EmptyCalendarService`（空数据），消除在生产环境下误回退到 `MockCalendarService` 产生虚假日程的问题。
2. **`MockCalendarService` 增加 `#if DEBUG` 守卫**：确保开发调试用的模拟日历类绝对不会泄漏或编译进 Release 生产二进制。
3. **`EmptyCalendarService` 完善**：补充打开 Windows 自带日历与日历设置逻辑。
4. **设置界面增加系统日历与 Outlook 经典版说明**：明确告知用户系统日历基于 Windows 自带日历，若使用经典版 Outlook（.ost/MAPI），建议使用 ICS 订阅或同步账户至 Windows 日历中；无事件状态增加对应 ToolTip。

### 2026-05-07

**系统日历拦截器开发与调试**

1. **新增 `SystemCalendarInterceptor`**：使用 `SetWinEventHook` 监听 `ShellExperienceHost` 进程窗口事件
2. **修复 static 构造函数 `IndexOutOfRangeException`**：`EventTypeNames` 字典初始化位置问题
3. **修复 EventTypeNames 查找键不匹配**：事件类型十六进制值与字典 key 不一致
4. **修复多显示器定位**：使用 `MonitorFromWindow` 替代 `SystemParameters.WorkArea`，正确获取窗口所在显示器
5. **修复重复触发问题**：添加 500ms 冷却时间防抖
6. **修复 `App.xaml.cs` 调用逻辑**：拦截器使用 `ShowPopup` 而非 `TogglePopup`，避免意外关闭已显示的面板
7. **修复窗口可见性检查阻塞 UNCLOAK 事件**：移除 `IsWindowVisible` 预检查
8. **修复弹出面板焦点丢失检测**：采用定时器轮询策略，记录弹出时的前台窗口句柄，仅在前台变为新窗口时关闭面板（解决了通过拦截器弹出的窗口无法获得前台焦点导致 `OnDeactivated` 不触发的问题）
9. **修复退出时系统日历恢复**：确保 `Dispose` 时恢复所有被隐藏的系统窗口
10. **修复 dist 发布版本面板不显示**：移除 `PublishTrimmed`，.NET 8 SDK 对 WPF 不支持 Trim，且 Trim 会破坏 `SetWinEventHook` 回调委托
11. **修复 ICS 农历节假日重复显示**：`IcsCalendarService` 添加去重逻辑——同日同标题前缀只保留跨度最长的版本
12. **修复 EventItem 全天事件日期倒挂**：`EventItem.xaml.cs` 添加 `if (endDate < startDate) endDate = startDate;` 保护
13. **修复单日全天事件日历格子圆点缺失**：`IcsCalendarService` + `CalendarViewModel` 三层防护修复 EndTime==StartTime 的匹配失败问题
14. **日历格子多圆点支持**：`DayCell` 控件从单圆点改为最多3个彩色圆点，按事件颜色去重显示

### 2026-05-05 ~ 05-06

**设置界面开发**

1. 实现完整的设置窗口 UI 和逻辑
2. 实现所有 11 项设置功能
3. 实现 .ics 远程日历服务
4. 实现多数据源聚合
5. 实现系统主题自动跟随
6. 实现设置持久化到 JSON 文件

### 2026-05-04 及之前

**MVP 开发**

1. 完成系统托盘、月历视图、事件展示、事件详情、面板体验、主题等基础功能
2. 完成 WPF 项目搭建和基础架构
