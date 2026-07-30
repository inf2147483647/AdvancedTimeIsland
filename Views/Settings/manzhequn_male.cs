using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandManZheQunMale", "满褶裙 明制（男）", true, SettingsPageCategory.Debug)]
public class ManZheQunMalePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        var markdown = @"
<infobar='type:warning, closable:true'>当前文档内部可能包含不兼容字体的字符：“𧜽”</infobar>

男性裙子，在明代北方（北京、山东地区）又称为𧜽(xuán)子，形制与女性一样，分[马面裙](route:AdvancedTimeIslandMaMianQunMale)、满褶裙两类。

### 北方文献

《酌中志》、《金瓶梅》

> 世人所穿𧜽子，如女裙之制者，神庙（万历皇帝）亦间尚之，曰衬褶袍。想即古人下裳之义也。

### 南方文献

《水浒传》、南方墓葬衣物疏

> 洪教头先脱了衣裳，拽扎起裙子，掣条棒使个旗鼓，喝道：“来，来，来！”

> 忽的有个大汉子，秃着头，不带巾帻，绾了个髻，穿一领雷州细葛布短敞衫，系一条单纱裙子。
        ";

        RenderMarkdown(panel, markdown);
    }
}
