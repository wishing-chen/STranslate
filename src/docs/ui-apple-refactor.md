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

## 翻译页供应商下拉与窗口缩放

2026-10-29：供应商入口并入语言选择同一行右侧，固定 58×36 DIP，只显示当前图标和箭头；名称与多选数量放在悬停提示和无障碍名称中。展开后是透明背景上的纵向悬浮圆钮（44 DIP 圆形底、30 DIP Logo、柔和阴影、选中描边），没有整块菜单面板。列表高度超过 324 DIP 后滚动。复用 ServiceSwitcherPopup 的 420/230 ms 节奏，列表由上至下错峰淡入，箭头随同一进度旋转 180 度，快速反向从当前状态继续。减少动画沿用系统设置。无供应商时入口显示“译”，浮层显示圆形添加按钮；已有供应商时只显示供应商，设置仍在主窗口顶部。原顶部多类型服务开关仍独立保留。

选择调用 SelectTranslationProviderCommand：取消旧请求，只启用所选翻译供应商并设为自动执行；切换本身不发送文本。其他类型服务、密钥和提示词不变。入口跟踪服务启停、改名与集合变化，并在卸载时解除监听；顶部多服务开关启用多个供应商时，入口图标显示首项，悬停提示显示名称与额外数量。

主窗移除 PersistentSizeToContent=Height，改为手动高度与 Settings.MainWindowHeight 双向绑定（新配置默认 560 DIP，最小 280 DIP，最大仍受显示器工作区比例限制）。高度与宽度一样防抖保存；隐藏时的尺寸回写被忽略。原先显式屏蔽纵向缩放的 WM_NCHITTEST 改为四边和四角命中，边缘宽度按当前窗口 DPI 缩放为 6 DIP；长结果通过滚动显示，不再把用户选定的高度顶回去。

验证：Release 编译成功，现有 274 项自动测试通过。另用隔离配置加载实际 MainWindow、三个内置供应商和真实资源，21 项内部断言通过：高度 640/320 DIP 回写、四边四角和中心命中、紧凑尺寸、当前供应商、浮层跨窗口命令绑定、选择后收起、互斥状态和快速反向动画。该检查没有在线翻译请求，也未修改用户供应商配置。实机仅检查了初版紧凑入口和空菜单；用户要求尽量不使用电脑操作后，后续检查均走内部测试。未声明实机鼠标拖动、多 DPI 或系统减少动画开关已验证。

同日紧凑布局追加验证：Release 编译成功，30 项内部检查通过，包括 400/520/800 DIP 下语言选择与供应商同排且不重叠、圆钮尺寸与主题底色、透明浮层、选择命令、关闭和可反向动画。已导出并查看实际 WPF 控件的离屏渲染，确认单行入口和悬浮圆钮；本轮未使用 Computer Use。离屏预览不包含系统 Mica 背景，不作为实机屏幕截图。
