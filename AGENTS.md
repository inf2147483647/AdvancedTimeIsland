你正在接管开发ClassIsland(api 2.0.0.0)（简称CI2）插件,名称为AdvancedTimeIsland  ，目的是实现更高级的ClassIsland的时间管理，以弥补原生的不足。

注意:1. 若无特殊解释,unix时间戳单位为秒
2.WPF版已停止维护，若没有说明则不改动WPF版

1. 若无特殊解释,全部功能都需要使用异步操作,避免阻塞UI线程造成卡顿
2. 必要时可前往D:\temp\ClassIsland查看源代码（或C:\Users\Administrator\citemp\ClassIsland）
3. 所有主界面控件都需要随主题深浅自适应
4. 需要同时兼容FluentAvalonia 3.0.0.0(C:\Users\Administrator\citemp\FluentAvalonia或D:\temp\FluentAvalonia)（简称FA3）和2.x版本（简称FA2）
5. 新增页面都需要在Plugin.cs中注册
6. 确保所有页面以及组件的Guid两两互异
7. 尽量不要做出破坏性更改，如果出现破坏性更改，需要明确指出更改了什么，以及有什么影响
8. 尽可能复用ClassIsland已有的组件样式，避免重复编写相同的代码
9. 思考链（<thinking>标签内）使用中文

对于汉服页面：

1. 新增页面需要按照hanfu_page_template.cs模板文件套用（不能套用别的文件）
2. 新增页面后对应的按钮需要变为已开发的状态，更新按钮文本颜色为应用强调色
3. 使用继承模板文件以精简代码，避免重复编写相同的代码
4. 新增页面都需要在Plugin.cs中注册
5. 确保所有页面以及组件的Guid两两互异

抽屉（DrawerHost / 自建滑入面板）踩坑记录：

1. ClassIsland.Core 的 DrawerHost 抽屉宽度由 DrawerContent 根控件的 Width + 水平 Margin 决定——DrawerContent 必须是带固定 Width 的 Border（如「管理启用的功能」抽屉为 500），放无 Width 的 StackPanel 会导致抽屉撑满整屏
2. 抽屉内容要滚动时，ScrollViewer 必须放在有界高度约束下：用 Grid（RowDefinitions="Auto,*"）让标题栏固定在 Auto 行、ScrollViewer 占 * 行；直接放进纵向 StackPanel 会拿到无限高度而不出滚动条
3. WPF 版（ClassIsland 1.x SDK）没有 DrawerHost，用「半透明遮罩 + 右侧 Border + TranslateTransform 动画」自建抽屉，宽度与 Avalonia 版保持一致，内容同样包 ScrollViewer

最后调用AdvisorTool进行vibe review，没有此工具则跳过

然后执行Build-Package.ps1打包插件（构建命令已包含，全部success代表构建成功），若没有改动WPF版则跳过WPF版构建打包
更改的内容需写入CHANGELOG.md文件中,若没有说明不要更新版本号
每次打包后，需计算编译产物AdvancedTimeIsland.dll的MD5值，追加写入TraceIDlist.txt文件中，格式为[目标框架WPF/FA2/FA3]-[版本号]-[构建时间YYYY-MM-DD-hh-mm-ss]-[MD5值]

