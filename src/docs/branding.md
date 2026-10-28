# Logo 与品牌资源

2026-10-28 按用户要求替换旧 Logo。设计是双向对话箭头组成的 S，蓝色圆角底板与浅色主体。使用内置 imagegen 生成并修订，没有调用外部图像 API。

## 文件与入口

- 仓库 `images/logo-master.png`：imagegen 生成的原始透明母版。
- `STranslate/Resources/app-logo.png`：应用统一图标蒙版后的 1024 像素 PNG，欢迎页及空态使用。
- `STranslate/Resources/app.ico`：程序、任务栏及各窗口图标，含 16/20/24/32/40/48/64/96/128/256 像素。
- `STranslate/Resources/updater.ico`：沿用同一图标，供上游安装打包流程使用。
- `STranslate/Images/app.png`：256 像素 Windows 通知图标。
- 仓库 `images/favicon.png`：512 像素 README 图标。
- `Controls/STranslateBadge.xaml`：保留 `IconSvg` 资源键及 256 DIP 布局边界，内容改为新 PNG；原透明度和欢迎/空态绑定不变。
- `Resources/BitmapImageLoc.cs`：普通托盘使用 64 像素 PNG，其余托盘状态以新 Logo 加 WPF 状态角标呈现；不改变原状态优先级或热键行为。

母版替换后运行仓库 `tools/export-logo.ps1 -InputPath <PNG绝对路径>`。脚本用 WPF 高质量缩放和 PNG 编码完成尺寸/格式导出，保留透明度，并用 ICO 解码器读回所有尺寸。导出统一应用 7% 安全边距、20% 半径的圆角图标蒙版，避免生成母版外缘的残余杂点进入成品；不重新绘制中央 Logo。

## 图像提示词

初始设计：一个 STranslate 原生桌面程序图标，以两条衔接的圆角对话/交换形状构成清晰的 S，强调小尺寸辨识度、均衡负空间、圆角底板和透明外背景；不使用文字、Apple 标志或设备样机。

最终修订提示词：

> Refine this into an impeccably clean FLAT VECTOR STYLE production icon. Keep the same bold interlocking speech-arrow S silhouette and rounded blue square, but REMOVE ALL luminous/glowing/3D/airbrush treatment. The S should be clean ivory white with only the faintest cool tonal variation. The rounded-square tile should be a clean even azure-to-deep-blue surface. Everything must have razor-clean geometric antialiased edges. Absolutely zero particles, zero splashes, zero stray pixels, zero grain, zero brush texture, zero glow, zero shadow outside the tile. Delete the visible cyan debris above the icon and purple debris below and around it completely. Outside the single closed rounded-square path must be fully transparent with alpha zero. Keep a 6 percent uniform transparent margin so no edge touches the canvas. Simple, strong and legible as a 16 pixel Windows taskbar icon. One icon only, no lettering besides the abstract S shape, no mockup.

## 迁移与构建

本机新根目录为 `D:\STtranslate`；源码在 `outputs/STranslate`，便携版在 `outputs/STranslate-Apple`，SDK 与 NuGet 缓存在 `work`。根目录快捷方式启动便携版。迁移核验后删除旧目录，不保留目录联接或第二份项目；之前聊天中的本地文件路径会失效。

`work/build-local.ps1` 以自身位置计算根目录；完整 Release 构建命令为 `./work/build-local.ps1 -Source current -Target solution -Configuration Release`。正常恢复依赖会更新中间文件中的绝对路径。迁移后不要把旧缓存路径当作固定路径写入源码。

应用仍使用上游单实例逻辑；只运行一个副本。不要清空用户的 `PortableConfig`，发布包与本机配置应分开保管。没有为迁移新增启动项或系统环境变量。
