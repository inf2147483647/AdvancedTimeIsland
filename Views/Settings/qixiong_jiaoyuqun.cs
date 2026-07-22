using AdvancedTimeIsland.Helpers;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandQiXiongJiaoYuQun", "交窬裙", true, SettingsPageCategory.Debug)]
public class QiXiongJiaoYuQunPage : HanfuPageTemplate
{
    protected override void BuildContent(StackPanel panel)
    {
        var markdown = @"# 交窬裙

交窬裙是隋唐五代时期[齐胸襦裙](route:AdvancedTimeIslandQiXiong)的主要下装形制，其特点是裙腰束于胸部，裙摆宽大，行走时飘逸灵动。裙分为一片式和两片式，穿法不同。

## 简介

齐胸衫裙已有文物出土，新疆阿斯塔纳唐墓出土两条唐裙。唐代国家统一，经济繁荣，社会风尚也比较开放，服饰款式也是群芳争艳，瑰丽多姿，大气飘逸又华丽绚烂。

齐胸襦裙一般分为对襟齐胸襦裙和交领齐胸襦裙两种，其中对襟齐胸襦裙的使用范围更为广泛。齐胸襦裙在唐朝仕女中非常盛行，现在保留的不少古画、出土文物都有它的踪迹。

## 发展历程

齐胸襦裙最早见于南北朝时期。在隋代及初唐时期，妇女的短襦都用小袖，下着紧身长裙，裙腰高系，一般都在腰部以上，有的甚至系在腋下，并以丝带系扎。

北宋时期，妇女的服饰已经从唐代的齐胸襦裙逐渐将裙子高度下移，以齐腰襦裙的形制居多。齐胸襦裙经历了隋、唐、五代直到宋朝理学兴起才被历史淘汰，大约有1000年的历史。

## 基本特征

齐胸襦裙是对隋唐五代时期特有的一种女子襦裙装的称呼，裙腰束得非常高，多在胸上。形制分为对襟齐胸襦裙和交领齐胸襦裙，裙分为一片式和两片式，穿法不同。

### 历史背景

下裙面料以丝织品为主，以多幅为佳，裙腰上提，此时裙色鲜艳，多为深红、绛紫、月青、草绿等，其中以石榴红裙流行的时间最长，色彩多样，多中求异，让人眼花缭乱，目不暇接。

如唐中宗的女儿安乐公主的百鸟裙，堪称中国织绣史上之名作；武则天时的响铃裙，将裙四角缀十二铃，行之随步，叮当作响，可谓千姿百态，美不胜收，与短襦和披肩相配一体，尽显盛唐女子雍容华贵的丰腴风韵，表现出极富诗意的美与韵律。

## 一片式齐胸襦裙穿法步骤

1. 上襦系带

2. 套入裙子，先将后片裙子的系带绕到胸前系住

3. 提起前片盖住后片的带子，将前片的带子绕到背后交叉

4. 带子在背后交叉后绕回前面

5. 将带子绕回到胸前系住";

        RenderMarkdown(panel, markdown);
    }
}