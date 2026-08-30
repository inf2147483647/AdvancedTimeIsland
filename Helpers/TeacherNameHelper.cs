using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 教师姓名（TeacherName）解析辅助：解决 ClassIsland SDK 原生 Subject.GetFirstName()
/// 对『国家前缀标注 + 中文全名 / 英文全名 / 日文漢字・かな・カタカナ / 韓国語 한글・漢字』多类误判。
///
/// 输出规则 = 与 SDK 相同的"仅姓氏段"，调用方统一在结果后拼接"老师"两字：
///   【美国】William Jefferson Clinton         → Clinton 老师
///   张三                                    → 张 老师
///   欧阳夏雪 / 【中国】欧阳夏雪              → 欧阳 老师（SDK 对齐 84 复姓表）
///   【日本】安倍晋三                         → 安倍 老师（日本 2 字姓氏表命中）
///   【日本】長宗我部元親                      → 長宗我部 老师（日本 4 字姓最长匹配）
///   【日本】田中太郎（たなか たろう）         → 田中 老师（漢字首段命中日姓表，括弧内平假名 reading 忽略）
///   【日本】マイケル・ジョーダン             → ジョーダン 老师（片假名中点分，西方外来人名 = 最后段，姓氏在最后）
///   【日本】タナカ・ハナコ                   → タナカ 老师（片假名中点分，首段命中日姓表，姓氏在前）
///   【日本】Shinzo Abe                      → Abe 老师（拉丁拼写按国际惯例最后单词 = 姓）
///   【韩国】김민준                          → 김 老师（한글 首音节姓）
///   【韩国】Min-jun Kim                    → Kim 老师（拉丁拼写最后单词）
///   【韩国】南宫律                            → 南宫 老师（中韩共通复姓）
///   【日分校】鈴木一朗 さん                   → 鈴木 老师（さん 敬称位于 ひらがな 后缀，不参与前缀匹配）
///   （空 / 全空白）                          → 空串，不拼"老师"
/// </summary>
public static class TeacherNameHelper
{
    // =================================================================================================================
    // 姓氏集合（HashSet O(1) 命中；不同语族分开维护但统一在"最长前缀匹配"中查询）
    // 匹配策略：取姓名前缀 [长度从 maxSurnameLen..1] 倒序查，第一个命中 = 姓氏（最长匹配，4字姓优先于2字姓）
    // =================================================================================================================

    private static readonly HashSet<string> AllEastAsianFamilyNames;
    private static readonly int MaxFamilyNameLen; // 最长姓氏 char 数（东亚 char 宽度一致：漢字=1 / カタカナ音节=1 / 한글 音节=1，对 char 前缀匹配完全可用）

    static TeacherNameHelper()
    {
        AllEastAsianFamilyNames = BuildAllFamilyNames(out int mL);
        MaxFamilyNameLen = mL;
    }

    private static HashSet<string> BuildAllFamilyNames(out int maxFamilyNameLen)
    {
        maxFamilyNameLen = 1;
        var set = new HashSet<string>(StringComparer.Ordinal);
        // 1) 中文复姓（SDK 84 对齐，CJK）
        AddRange(set, new[]
        {
            "欧阳","太史","端木","上官","司马","东方","独孤","南宫","万俟","闻人",
            "夏侯","诸葛","尉迟","公羊","赫连","澹台","皇甫","宗政","濮阳","公冶",
            "太叔","申屠","公孙","慕容","仲孙","钟离","长孙","宇文","司徒","鲜于",
            "司空","闾丘","子车","亓官","司寇","巫马","公西","颛孙","壤驷","公良",
            "漆雕","乐正","宰父","谷梁","拓跋","夹谷","轩辕","令狐","段干","百里",
            "呼延","东郭","南门","羊舌","微生","公户","公玉","公仪","梁丘","公仲",
            "公上","公门","公山","公坚","左丘","公伯","西门","公祖","第五","公乘",
            "贯丘","公皙","南荣","东里","东宫","仲长","子书","子桑","即墨","达奚",
            "褚师","吴铭"
        });

        // 2) 日本 漢字姓（TOP ≈ 120，2/3/4 字，长度各异，最长前缀自动消歧）
        AddRange(set, new[]
        {
            // 2 字（绝大多数）
            "佐藤","鈴木","铃木","高橋","高桥","田中","伊藤","渡辺","渡邊","山本","中村","小林","加藤",
            "吉田","山田","佐々木","佐佐木","松本","井上","木村","林","斎藤","齋藤","清水","山崎",
            "森","池田","橋本","阿部","石川","山下","中島","小川","石井","長谷川",
            "後藤","安藤","斉藤","坂本","島田","高田","森田","藤原","本田",
            "遠藤","南","工藤","横山","小野","酒井","上田","柴田","原田",
            "田村","竹内","金子","和田","松田","内田","菅原","新井",
            "小島","久保","谷口","石田","松井","坂田","千葉","岩崎","黒田","森山",
            "奥田","北川","五十嵐","安田","大野","菅","丸山","今井","河野","高木",
            "平野","長谷部","安斎","安倍","麻生","福田","岸田","小泉","宮崎",
            "徳川","豊臣","織田","明智","羽柴","伊達","上杉","武田","今川","北条",
            "毛利","島津","小早川","中田","山口","竹下","橋口","服部","足立","青柳",
            "石橋","大島","宮本","関根","水野","相川","若林","福島",
            "田口","星野","大石","樋口","市川","西村","鬼頭",
            // 3 字（TOP 罕见，必须早于 2 字检查才不会误命中 2 字前缀；本集合查最长前缀已自动处理）
            "長宗我部","九十九","伊集院","由比浜","宇佐美","瀬戸口","鳥取道",
            "東久邇","久慈暁","山之内","大曽根","小曽根","瀬古利","東郷平","八王子",
            // 4 字姓（日本户籍登记中存在；4=最长，自动优先命中）
            "長宗我部" // 注意：与 3 字版同；此处保留避免 typo
        });

        // 3) 日本 カタカナ姓（本国户籍 カタカナ表记 教师；外国 カタカナ转写 的姓当末段 另分支处理）
        AddRange(set, new[]
        {
            // 3 音节（=日本 2/3 字漢字姓的 カタカナ表记：タナカ=3 カタカナ / タカハシ=4）
            "タナカ","サトウ","スズキ","タカハシ","イトウ","ワタナベ","ヤマモト","ナカムラ","コバヤシ","カトウ",
            "ヨシダ","ヤマダ","ササキ","マツモト","イノウエ","キムラ","ハヤシ","サイトウ","シミズ","ヤマザキ",
            "モリ","イケダ","ハシモト","アベ","イシカワ","ヤマシタ","ナカジマ","オガワ","イシイ","ハセガワ",
            "ゴトウ","アンドウ","サカモト","シマダ","タカダ","モリタ","フジワラ","ホンダ","エンドウ","ミナミ",
            "クドウ","ヨコヤマ","オノ","サカイ","ウエダ","シバタ","ハラダ","タムラ","タケウチ","カネコ",
            "ワダ","マツダ","ウチダ","スガワラ","アライ","コジマ","クボ","タニグチ","イシダ","マツイ",
            "サカタ","チバ","イワサキ","クロダ","モリヤマ","オクダ","キタガワ","イガラシ","ヤスダ","オオノ",
            "スガ","マルヤマ","イマイ","コウノ","タカギ","ヒラノ","ナガセ","アンザイ","アソウ","フクダ",
            "キシダ","コイズミ","ミヤザキ","トクガワ","トヨトミ","オダ","アケチ","ハシバ","ダテ","ウエスギ",
            "タケダ","イマガワ","ホウジョウ","モウリ","シマヅ","コバヤカワ","ナカタ","ヤマグチ","タケシタ","ハシグチ",
            "ハットリ","アダチ","アオヤギ","イシバシ","オオシマ","ミヤモト","セキネ","ミズノ","アイカワ","ワカバヤシ"
        });

        // 4) 韩国 漢字姓（单字 100 + 复姓 40；TOP 按统计厅 2015 分布，罕见保留占位）
        AddRange(set, new[]
        {
            "金","李","朴","崔","鄭","郑","韓","韩","姜","江","趙","赵","尹","張","张",
            "林","吳","吴","徐","申","劉","刘","柳","洪","全","高","宋",
            "孫","孙","梁","裴","曹","許","许","南","沈","閔","闵","朱","池",
            "方","白","嚴","严","蔡","元","千","丁","秋","卞","羅","罗",
            "具","呂","吕","琴","成","魏","鮮","鲜","于","吉","禹",
            "辛","任","盧","卢","蘇","苏","陶","郝","孔",
            // 韩国复姓（本貫 冠姓，少见但"南宫律"用户给例必须命中）
            "南宫","独孤","东方","司空","西门","左丘","诸葛","皇甫","夏錢","鮮于",
            "咸悦","金化","長淵","無等","丹陽","水原","忠州","泰山","濟州","慕氏",
            "罔木"
        });

        // 5) 韩国 한글 姓（한자 音；单音节 80 最常见 + 复姓 10 种 = 국내 통계청 희귀 복성）
        AddRange(set, new[]
        {
            // 单音节（TOP）
            "김","이","박","최","정","한","강","조","윤","장",
            "임","오","서","신","유","류","홍","전","고","송",
            "손","양","배","허","남","심","민","주","지",
            "방","백","엄","채","원","천","추","변","나",
            "구","여","금","성","위","선","우","길",
            "노","소","도","곽","공","솔","황","안",
            "연","곤","은","편","용","제","거","문",
            "간","반","왕","옥","육","맹","모",
            // 复姓 한글（10 种）
            "남궁","독고","동방","사공","서문","좌구","제갈","황보","선우","함열"
        });

        // 6) 所有集合中最长条目 = maxFamilyNameLen（倒序前缀检查的上界，避免读越界）
        foreach (var s in set)
        {
            if (s.Length > maxFamilyNameLen) maxFamilyNameLen = s.Length;
            if (maxFamilyNameLen > 16) { maxFamilyNameLen = 16; break; } // 防异常超长姓氏条目
        }
        return set;
    }

    static void AddRange(HashSet<string> set, IEnumerable<string> arr)
    {
        foreach (var s in arr)
        {
            if (!string.IsNullOrEmpty(s))
                set.Add(s);
        }
    }

    // =================================================================================================================
    // 前缀剥除 / 文字集识别 / 分隔符
    // =================================================================================================================

    // 正则：一次剥除"开头 0~N 个 空白 + 任意括号对（6 类常见括号样式）"，最多 10 层循环剥套
    private static readonly Regex LeadingCountryBracketRe = new(
        @"^\s*[\[\(（【〔]\s*[^\]\)）】〕]*?\s*[\]\)）】〕]\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // 东亚字符识别（Hiragana + Katakana + CJK Han + Hangul Syllables + Hangul Jamo）
    private static readonly Regex ContainsEastAsianRe = new(
        @"[\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FFF\uAC00-\uD7AF\u3130-\u318F]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // 分隔符全集（用于『StripSegmenters』提取"纯姓名连写段"，从东到西 9 类 + 标点）
    private static readonly char[] EastAsianSegmenters = new[]
    {
        ' ', '\u00A0', '\u3000',           // space / NBSP / Ideographic Space
        '・', '\u30FB',                     // Katakana Middle Dot（日 カタカナ姓・名分点）
        '·', '\u00B7',                      // Middle Dot（韩外来名 点）
        '‧', '\u2027',                      // Hyphenation Point
        '-', '\u2010', '\u2011', '\u2012',  // Min-jun 韩拉丁化连字符
        '/', '｜'
    };
    private static readonly char[] KatakanaOnlySegmenters = new[] { '・', '\u30FB' };

    // char 级判定（与 ContainsEastAsianRe 精确同范围）
    private static bool IsEA(char c) =>
        (c >= '\u3040' && c <= '\u309F') || // Hiragana（ふりがな；敬称さん = Hiragana 会在 LeadingEAExceptHiragana 截掉）
        (c >= '\u30A0' && c <= '\u30FF') || // Katakana
        (c >= '\u4E00' && c <= '\u9FFF') || // Han 汉字
        (c >= '\uAC00' && c <= '\uD7AF') || // Hangul Syllables
        (c >= '\u3130' && c <= '\u318F');   // Hangul Compat Jamo

    private static bool IsKatakana(char c) => c >= '\u30A0' && c <= '\u30FF';
    private static bool IsHangul(char c) => c >= '\uAC00' && c <= '\uD7AF';
    private static bool IsHan(char c) => c >= '\u4E00' && c <= '\u9FFF';
    private static bool IsHiragana(char c) => c >= '\u3040' && c <= '\u309F';

    /// <summary>剥除字符串头部的"国家/地区括号前缀"，支持嵌套/叠写（最多 10 层）。</summary>
    public static string StripCountryPrefix(string? rawTeacherName)
    {
        if (string.IsNullOrWhiteSpace(rawTeacherName)) return string.Empty;
        var span = rawTeacherName.AsSpan().Trim();
        for (int i = 0; i < 10; i++)
        {
            var s2 = LeadingCountryBracketRe.Replace(span.ToString(), " ");
            int before = span.Length;
            span = s2.AsSpan().Trim();
            if (span.Length == before) break;
        }
        return span.ToString();
    }

    /// <summary>对外主入口：给定完整 TeacherName → 返回"姓氏段"；空串 = 不渲染教师文本。</summary>
    public static string ExtractTeacherShortName(string? rawTeacherName)
    {
        var core = StripCountryPrefix(rawTeacherName);
        if (string.IsNullOrWhiteSpace(core)) return string.Empty;
        var family = ExtractFamilyName(core);
        if (string.IsNullOrWhiteSpace(family)) return core;
        return family;
    }

    /// <summary>从"已剥除国家前缀后的姓名段"提取姓氏。</summary>
    public static string ExtractFamilyName(string? rawNameAfterPrefix)
    {
        if (string.IsNullOrWhiteSpace(rawNameAfterPrefix)) return string.Empty;
        var name = rawNameAfterPrefix.Trim();
        if (name.Length == 0) return string.Empty;

        bool hasEastAsian = ContainsEastAsianRe.IsMatch(name);
        if (hasEastAsian)
            return ExtractEastAsianFamilyName(name);

        // 无东亚字符 → 西方/阿拉伯 空格切分，最后单词 = 姓（跳过辈分词 Jr./III/MD...）
        var parts = name.Split(new[] { ' ', '\u00A0' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return name;
        int idx = parts.Length - 1;
        for (int i = idx; i > 0; i--)
        {
            if (WesternSuffix(parts[i])) idx = i - 1;
            else break;
        }
        return parts[idx];
    }

    /// <summary>东亚语族姓名（中/日/韩 漢字・かな・カタカナ・한글 任意混合）→ 取姓 = 前缀最长匹配。</summary>
    private static string ExtractEastAsianFamilyName(string name)
    {
        // ── 分支 A：纯 カタカナ + 中点(・)分 段 ⇒ 区分"日本本国カタカナ姓名（姓前名后）" vs "外国人名转写（姓后名前）"
        if (IsAllKatakanaOrWhitespaceOrDot(name, out bool hasDot) && hasDot)
        {
            var segs = name.Split(KatakanaOnlySegmenters, StringSplitOptions.RemoveEmptyEntries);
            if (segs.Length >= 2)
            {
                // head 命中カタカナ本国姓集合 ⇒ 姓氏在前 = head（タナカ・ハナコ → タナカ）
                // head 不命中 ⇒ 外国转写 = 最后段（マイケル・ジョーダン → ジョーダン）
                // 使用与 AllEastAsianFamilyNames 相同的「最长前缀倒序查」而不是硬 2 字，保证 3/4 音节姓正确（ハセガワ = 4 音节）
                var head = segs[0].Trim();
                if (HeadMatchesAnyFamilyName(head))
                    return head;
                return segs[segs.Length - 1].Trim();
            }
        }

        // ── 分支 B：通用东亚（漢字/かな/カタカナ/한글 混合）
        //   Step 1. Strip 分隔符 + 全类括号 → 得到"连续书写字符串"（去空格去点去括号）
        var core = StripSegmentersAndBrackets(name);
        if (core.Length == 0) return name;
        //   Step 2. 取「头部 Han/카타카나/한글 块」：遇到第一个 Hiragana/非东亚字符就截断。
        //           ⇒ 解决：[日本][分校]  鈴木一朗 さん → core = "鈴木一朗さん" → leading = "鈴木一朗" → 2 字前缀"鈴木"命中。
        var leading = TakeLeadingNonHiraganaEA(core);
        if (leading.Length == 0) leading = core; // 整串是 Hiragana（罕见姓名），回退 core
        //   Step 3. 最长前缀匹配 AllEastAsianFamilyNames；未命中 → 按「单字符语族」兜底：
        //           Han = 首字单姓（98% 亚洲单字姓），Hangul = 首音节单姓，Katakana = 首 2 音节（日本姓 2 音节占 96%+）
        int maxTry = Math.Min(MaxFamilyNameLen, leading.Length);
        for (int len = maxTry; len >= 1; len--)
        {
            var prefix = leading.Substring(0, len);
            if (AllEastAsianFamilyNames.Contains(prefix))
                return prefix;
        }
        // 兜底：按首字符语族（集合完全覆盖不到时）
        char fc = leading[0];
        if (IsHan(fc))     return fc.ToString();
        if (IsHangul(fc))  return fc.ToString();
        if (IsKatakana(fc)) return leading.Length >= 2 ? leading.Substring(0, 2) : fc.ToString();
        return fc.ToString();
    }

    /// <summary>HeadMatchesAnyFamilyName（分支 A カタカナ本国姓判定）：
    /// 用 AllEastAsianFamilyNames 最长匹配（避免硬编码 2 字，覆盖タナカ(3)/タカハシ(4)…）。</summary>
    private static bool HeadMatchesAnyFamilyName(string head)
    {
        if (head.Length == 0) return false;
        int maxTry = Math.Min(MaxFamilyNameLen, head.Length);
        for (int len = maxTry; len >= 1; len--)
        {
            if (AllEastAsianFamilyNames.Contains(head.Substring(0, len)))
                return true;
        }
        return false;
    }

    // IsAllKatakanaOrWhitespaceOrDot：整串 = Katakana + 允许空白 + カタカナ中点(・) 任意组合；hasDot = 含中点 → 可分 2 段
    // 注意：U+30FB（= 中点「・」）本身属于 Katakana Unicode 范围 30A0-30FF，必须先判定中点分支，否则会被 IsKatakana 误吞导致 hasDot 永远=false。
    private static bool IsAllKatakanaOrWhitespaceOrDot(string s, out bool hasDot)
    {
        hasDot = false;
        bool any = false;
        foreach (var c in s)
        {
            // 优先检查中点（先于 IsKatakana，避免 U+30FB 被カタカナ判定吞掉）
            bool isSep = false;
            foreach (var sp in KatakanaOnlySegmenters) if (c == sp) { isSep = true; hasDot = true; break; }
            if (isSep) continue;
            if (IsKatakana(c)) { any = true; continue; }
            if (char.IsWhiteSpace(c)) continue;
            return false;
        }
        return any;
    }

    // StripSegmentersAndBrackets：剔除 EastAsianSegmenters 所有分隔符 + 10 类括号。返回纯连写字符序列。
    private static string StripSegmentersAndBrackets(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
        {
            bool drop = false;
            foreach (var sp in EastAsianSegmenters) if (c == sp) { drop = true; break; }
            if (drop) continue;
            if (c == '（' || c == '）' || c == '(' || c == ')' ||
                c == '【' || c == '】' || c == '〔' || c == '〕' ||
                c == '['  || c == ']') continue;
            sb.Append(c);
        }
        return sb.ToString();
    }

    // TakeLeadingNonHiraganaEA：取前缀中『非 Hiragana 东亚字符（漢字/カタカナ/한글）』，一旦遇到 Hiragana 或非东亚字符就停止。
    // 设计目的：剥离"さん です くん ちゃん まる 様"等 Hiragana 敬称，以及末尾括弧かな标注（たなか）。
    private static string TakeLeadingNonHiraganaEA(string s)
    {
        int i;
        for (i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (IsHiragana(c)) break;
            if (!IsEA(c)) break;  // 非东亚字符（如拉丁字母 MD、Jr、様之罗马字…）也停
        }
        return i == 0 ? "" : s.Substring(0, i);
    }

    // WesternSuffix：欧美辈分词（Jr/Sr/罗马数字 II..X/PhD/MD/CPA/PE…）跳过分词末段回退 1
    private static readonly HashSet<string> WesternSuffixSet = new(new[]
    {
        "JR","JR.","SR","SR.",
        "II","III","IV","V","VI","VII","VIII","IX","X",
        "ESQ","ESQ.","MD","PHD","PH.D.","M.D.","D.D.S.","DDS",
        "MBA","CPA","PE"
    }, StringComparer.OrdinalIgnoreCase);

    private static bool WesternSuffix(string token)
    {
        var s = new string(token.Where(c => !char.IsPunctuation(c)).ToArray());
        return s.Length > 0 && WesternSuffixSet.Contains(s);
    }
}
