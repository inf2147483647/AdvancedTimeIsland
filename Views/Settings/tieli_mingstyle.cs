using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandTieliMingStyle", "贴里 明制", true, SettingsPageCategory.Debug)]
public class TieliMingStylePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        var markdown = @"# 明制贴里·女装

## 一、核心定位
贴里**并非明代女性的主流常规服饰**，不属于明代女性冠服制度的标准形制；仅在特定场景下存在女性穿着案例，属于非主流的特例穿着，未形成独立的女性贴里服饰体系。

## 二、文献与形象佐证
1. **文献记载**：明代正史、官方冠服典制中，无针对女性的贴里服用制度与品级规定；《酌中志》《明宫史》等记载贴里的一手史料，均指向男性群体。
2. **形象与实物**：
   - 现存明代女性人物画像、壁画中，穿贴里的女性形象多为女扮男装角色、侍女杂役，而非女性正装形象。
   - 目前国内已发掘的明代女性墓葬中，极少出土贴里实物，无成体系的女性贴里墓葬实证。
3. **学术界定**：服饰史学术研究普遍将贴里归入明代男子服饰体系，仅在""女扮男装""""侍女服饰""的细分研究中提及女性穿着案例。

## 三、女性穿着的主要场景
1. **女扮男装**：是女性穿贴里最常见的场景，多见于文学作品记载、戏曲形象，以及部分特殊身份女性的便服穿搭，属于功能性、模仿性穿着。
2. **侍从劳作**：明代宫廷、贵族府邸中的下层侍女、杂役女性，因劳作便利需求，会穿着简化版的贴里类短款服饰，不属于正式女装范畴。

## 四、形制特点
女性穿着的贴里无专属定制形制，基本沿用男性贴里的结构逻辑：前后分裁、腰部打褶、大襟右衽；仅在衣长、袖型上根据穿着者身形适度调整，未发展出女性独有的版型、纹样制度与穿搭体系。

## 五、现代认知说明
1. 当前汉服市场中的""女款贴里""，多为现代商家基于审美需求开发的改良衍生款，并非对明代女性传统服饰的考据复原。
2. 若追求严格的形制考据，贴里的核心复原依据均为男性文物与制度，不建议将其作为明代女性的正统形制进行宣传与穿搭。
3. 明代女性的主流常服体系为交领袄/衫搭配马面裙，立领袄、比甲、披风等是明代女性的典型外搭服饰。";

        RenderMarkdown(panel, markdown);
    }
}