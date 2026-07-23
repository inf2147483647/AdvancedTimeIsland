你正在接管开发ClassIsland(api 2.0.0.0)（简称CI2）插件,名称为AdvancedTimeIsland  ，目的是实现更高级的ClassIsland的时间管理，以弥补原生的不足。

注意:1. 若无特殊解释,unix时间戳单位为秒

1. 若无特殊解释,全部功能都需要使用异步操作,避免阻塞UI线程造成卡顿
2. 必要时可前往D:\temp\ClassIsland查看源代码（或C:\Users\Administrator\citemp\ClassIsland）
3. 所有主界面控件都需要随主题深浅自适应
4. 需要同时兼容FluentAvalonia 3.0.0.0(C:\Users\Administrator\citemp\FluentAvalonia或D:\temp\FluentAvalonia)（简称FA3）和2.x版本（简称FA2）
5. 新增页面都需要在Plugin.cs中注册
6. 确保所有页面以及组件的Guid两两互异

在汉服页面中

1. 新增页面需要按照hanfu_page_template.cs模板文件套用（不能套用别的文件）
2. 新增页面后对应的按钮需要变为已开发的状态，更新按钮文本颜色为应用强调色
3. 使用继承模板文件以精简代码，避免重复编写相同的代码
4. 新增页面都需要在Plugin.cs中注册
6. 确保所有页面以及组件的Guid两两互异

最后调用AdvisorTool进行vibe review
