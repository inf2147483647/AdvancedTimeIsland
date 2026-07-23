using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandMamianQunBaiZhe", "马面裙 百褶 明制", true, SettingsPageCategory.Debug)]
public class MamianQunBaiZhePage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        var markdown = @"# 明代百褶马面裙

## 一、简介

百褶马面裙是明代女式马面裙的细分款式，沿用「两片共腰、前后交叠形成光面裙门（马面）」的基础结构，核心特征为**裙身两侧褶裥数量更多、排布更细密**，区别于[常规宽疏侧褶的马面裙](route:AdvancedTimeIslandMamianQunCeZhe)，明中后期开始流行，适配各阶层女性日常与礼仪穿着。

## 二、简要历史

1. 源头：宋代旋裙奠定两片交叠结构
2. 明初：马面裙定型，以疏朗侧褶为主（3-6对活褶）
3. 明中后期：裙褶逐渐加密，百褶形制成型并流行

## 三、形制与「常规侧褶马面裙」核心区别

二者基础结构一致，差异集中在褶裥设计：

| 对比维度 | 常规侧褶马面裙 | 明代百褶马面裙 |
| :--- | :--- | :--- |
| 褶裥数量 | 3-8对活褶，数量少 | 十数对及以上，褶量较多 |
| 褶型宽度 | 宽幅活褶，间距大 | 窄幅活褶，排列紧密 |
| 视觉效果 | 端庄平整，褶感弱 | 走动如水波，层次感强 |
| 主流时期 | 明初至明中期 | 明中后期至明末 |

补充共性：裙腰多用白棉布（取白头偕老寓意），两端设系带；裙身多饰膝襕、底襕纹样；由多幅面料纵向拼接而成。

## 四、穿着要点，注意事项

前往[马面裙 侧褶](route:AdvancedTimeIslandMamianQunCeZhe)

## 参考资料

1. [https://www.chinasilkmuseum.com/zggd/info_21.aspx?itemid=1905](https://www.chinasilkmuseum.com/zggd/info_21.aspx?itemid=1905)
2. [http://www.artmuseum.tsinghua.edu.cn/cpsj/zlxx/zlhg/202509/t20250926_20317.shtml](http://www.artmuseum.tsinghua.edu.cn/cpsj/zlxx/zlhg/202509/t20250926_20317.shtml)
3. [http://fzxb.paperonce.org/oa/darticle.aspx?type=view&id=20244002](http://fzxb.paperonce.org/oa/darticle.aspx?type=view&id=20244002)
4. [http://kaogu.cssn.cn/xsqy/kycg/xslw/202607/W020260706531412362048.pdf](http://kaogu.cssn.cn/xsqy/kycg/xslw/202607/W020260706531412362048.pdf)";

        RenderMarkdown(panel, markdown);
    }
}
