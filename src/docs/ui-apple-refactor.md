# Rounded UI 设计与维护

## 来源与设计

基线为 v2.0.10，不以滚动 main 分支为起点。参考 Apple 官方 [Layout](https://developer.apple.com/design/human-interface-guidelines/layout)、[Motion](https://developer.apple.com/design/human-interface-guidelines/motion)、[Materials](https://developer.apple.com/design/human-interface-guidelines/materials)，以及 [WWDC25 新设计系统](https://developer.apple.com/videos/play/wwdc2025/356/)。采用内容优先、相关操作就近、层级圆角、统一对齐与短动作反馈；内容区域保持清晰，材质集中在窗口层。并非移植 macOS 的窗口按钮，也不声称逐像素复制 Apple 的连续曲率。

用户提供的本地输入条参考动画是固定圆形 Logo 上浮加透明度，展开 420ms、收起 230ms；无缩放和过冲。该节奏用于服务浮层。普通内容只用 180ms、6 DIP 的轻微位移，避免流式结果被持续动画打断。

## 文件结构与边界

| 文件 | 职责 |
| --- | --- |
| `Resources/AppleTheme.xaml` | 共享 10/16/20 DIP 圆角与控件样式；颜色跟随 iNKORE 系统深浅主题 |
| `Resources/CustomStyles.xaml` | 结果 Expander；展开时内容正常参与布局，动效只作用于渲染 |
| `Views/MainWindow.xaml` | 测量式两行主窗、语言栏、输入卡与独立临时服务行 |
| `Controls/HeaderControl.*` | 常用/更多分组，窄窗口收纳次要入口；保留用户可见项 |
| `Controls/ServiceSwitcherPopup.cs` | 浮层进度、错峰、反向、焦点、外部点击和事件解绑 |
| `Controls/InputControl.*` | 输入状态、工具、翻译主按钮；文本、IME、RTL 和命令保持原逻辑 |
| `Controls/IconButton.xaml` | 显式 `STIconButtonStyle` 基础模板；所有局部样式必须从此键继承 |
| `Controls/OutputControl.xaml` | 服务标题、提示词、结果及其就近操作；设置按钮支持键盘焦点 |
| `Controls/SurfaceMotion.cs` | 渲染位移和淡入；卸载时取消动画，没有后台计时器 |
| `Views/SettingsWindow.xaml` | 紧凑侧栏、统一线性图标、圆角设置内容 |
| `Core/Settings.cs` | 新配置默认宽 520 DIP，关闭自动上游更新；已有保存值仍优先 |

外层窗口仍由 Windows/iNKORE 的窗口框架、DWM、DPI 与缩放命中机制管理。系统版本影响原生外轮廓，不应为追求任意半径而改成透明分层窗口，导致输入、性能与缩放退化。内部卡片和控件圆角由本项目控制。

## 状态与消息逻辑

服务列表不是互斥模型选择器。`QuickServiceItems` 汇集翻译、OCR、TTS 和生词本，点击继续调用原来的 `ToggleQuickServiceCommand`。不要将 `IsChecked` 改为直接写服务对象而绕过原保存逻辑。

`ServiceSwitcherPopup.IsExpanded` 是请求状态，`IsOpen` 是窗口实际存在状态。请求关闭时先播放退出，完成后才关闭 Popup；展开途中反向从当前进度续播。`CompositionTarget.Rendering` 只在动画过程中订阅，关闭和卸载都解绑。失去激活或点击别处时不抢回焦点，明确选择/Esc 关闭时才恢复输入。减少动画读取 `SystemParameters.ClientAreaAnimation`。

`SurfaceMotion` 只挂在没有其它变换的内容容器上。不要挂到已被图片缩放或拖动占用的 RenderTransform。流式文字更新不会重新启动动画。结果正文完整保留 RTL、错误、重试、复制、播放、回译绑定。

主窗 Header 与滚动区域现在由实际布局尺寸分行；不要恢复硬编码顶部 37px 偏移。输入工具用可换行布局；临时服务另占一行，避免把语种下拉挤窄。`InputControl.OnGotFocus` 仅在自身获取焦点时转给文本框，不能抢走子按钮和下拉框的键盘焦点。

已实际遇到的样式陷阱：局部 IconButton 使用同类型隐式键作为 `BasedOn` 时，运行中的 iNKORE 主题初始化可能得到空基础样式，出现有占位但无按钮的情况。必须引用显式 `STIconButtonStyle`，并检查模板的值来源是 Style，不能仅依赖测试环境的 DefaultStyle 兜底。结果分隔线使用动态主题画刷，避免脱离 ContentDialog 时静态资源解析失败。

## 构建、验证与风险

`build-ui.ps1` 构建 Release 全方案并运行自动测试；不调用上游发布脚本，不下载官方安装包。UI 专项需要额外检查 400/520/800 DIP、深浅主题、较大字体、长服务名、少量/大量服务、浮层快速反向、Esc、外部点击与减少动画。静态 XML、成功编译与普通逻辑测试不能代替视觉和交互检查。

2026-10-28 验证记录：Release 主程序及 21 个插件编译成功，274 项自动测试通过。独立 WPF 检查器加载产品资源与 Header/Input/Output，覆盖 400/520/800 DIP、输入字号 14/24、长名称和非空输出，并确认 IconButton 模板来自显式基础样式。实机预览使用独立便携配置，验证更多菜单、Esc、服务启停后焦点恢复、设置页，以及微软内置翻译成功。Google 只添加到测试配置，未在线请求。未实测多屏 DPI、所有深色页面、长时间运行或系统减少动画开关；未测量帧率。临时检查器不作为产品测试项目交付。

原版依赖 `SQLitePCLRaw.lib.e_sqlite3 2.1.11` 的 NU1903 警告属于基线。本次未扩大到依赖升级。未配置密钥的服务不能据 UI 测试宣称在线请求可用；OCR、远程翻译、TTS 的端到端功能也必须单独验证。

交付不应包含开发机器的 PortableConfig、密钥、历史数据库或日志。独立预览配置应在 work 下；正式包只包含新的初始配置。不要覆盖用户现有安装或新增开机启动，除非用户明确要求。
