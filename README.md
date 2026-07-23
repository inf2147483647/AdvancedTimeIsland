# AdvancedTimeIsland

在ClassIsland自动化添加支持高级触发时间的插件

![demo](demo.png)

图片仅供参考
[![下载量](https://img.shields.io/github/downloads/inf2147483647/AdvancedTimeIsland/total?style=social&label=下载量&logo=github)](https://github.com/inf2147483647/AdvancedTimeIsland/releases/latest)

> \[!caution]
>
> # 免责声明
>
> 插件内包含大量文本输入框，插件作者不对使用者在其中输入的内容做任何担保，如果使用者因输入不当内容导致造成不良影响，使用者需自行承担相关责任，插件作者概不负责。

> \[!caution]
> 本项目的代码将由AI生成，本项目能够保证的是功能正常 ~~（图标是人类做的，部分文案是AI生成的，介绍也是）（这句话也是人类写的）~~

# 协助开发

<details>
<summary>PR(pull request)教程：</summary>

### 提交流程概览 / Submission Workflow Overview

Fork 仓库 → 添加你的文件 → 提交 Commit → 发起 Pull Request → 等待审核合并
Fork repo → Add your files → Commit → Open Pull Request → Wait for review & merge

---

### 第一步：Fork 仓库 / Step 1: Fork the Repository

> **Fork** = 把别人的仓库复制一份到你自己的 GitHub 账号下，让你可以自由修改，不会影响原项目。
> **Fork** = Creating your own copy of someone else's repository under your GitHub account, so you can freely make changes without affecting the original.

1. 打开目标开源仓库页面
2. 点击页面右上角的 **Fork** 按钮
3. 这会在你的 GitHub 账号下创建一份仓库副本

---

1. Go to the target open-source repository page
2. Click the **Fork** button at the top right
3. This creates a copy of the repository under your GitHub account

---

### 第二步：添加你的文件 / Step 2: Add Your Files

> **Commit** = 给你的修改拍一张快照，记录你改了什么内容，每次 Commit 都会生成一条历史记录。
> **Commit** = Taking a "snapshot" of your changes, recording what you modified. Each commit creates a point in the project history.

#### 方法 A：直接在 GitHub 网页上传（最简单）/ Method A: Upload via GitHub Web (Easiest)

1. 进入你 Fork 后的仓库页面（`https://github.com/<你的用户名>/<仓库名>`）
2. 导航到你需要添加文件的对应目录
3. 点击 **Add file** → **Create new file**，填写带路径的文件名即可自动创建对应目录与文件
4. 在编辑区填写文件内容
5. 点击 **Commit changes** 完成提交
6. 若需上传本地文件，再次点击 **Add file** → **Upload files**，选择本地文件并上传至目标目录
7. 点击 **Commit changes** 完成提交

---

1. Go to your forked repository (`https://github.com/<your-username>/<repository-name>`)
2. Navigate to the directory where you want to add files
3. Click **Add file** → **Create new file**. Enter a filename with path to automatically create the corresponding directory and file
4. Fill in the file content in the editor
5. Click **Commit changes** to finish the submission
6. To upload local files, click **Add file** → **Upload files** again, select local files and upload them to the target directory
7. Click **Commit changes** to finish the submission

---

#### 方法 B：使用 Git 命令行 / Method B: Using Git CLI

> **Clone** = 把远程仓库下载到本地电脑。
> **Push** = 把本地的 Commit 上传同步到远程仓库（你的 Fork）。
> **Clone** = Downloading the remote repository to your local machine.
> **Push** = Uploading your local commits back to the remote repository (your Fork).

```bash
# 1. 克隆你 Fork 的仓库 / Clone your forked repository
# 若仓库 commit 历史较多，完整克隆速度较慢，推荐使用 --depth 1 仅拉取最近一次提交，提升下载速度
# The repo has a large commit history, so a full clone may be slow.
# Consider using --depth 1 to only fetch the latest commit for a faster download
git clone --depth 1 https://github.com/<你的用户名>/<仓库名>.git
cd <仓库名>

# 2. 创建目标目录并添加你的文件 / Create target directories and add your files
mkdir -p <目标目录路径>
cp /path/to/your/files/* <目标目录路径>/

# 3. 提交更改 / Commit your changes
git add .
git commit -m "描述本次提交的内容"

# 4. 推送到你的 Fork 仓库 / Push to your forked repository
git push origin main
```

---

### 第三步：发起 Pull Request / Step 3: Open a Pull Request

> **Pull Request（PR）** = 向原仓库的维护者提出申请：我在我的 Fork 里做了一些修改，请把它们合并到原项目里。
> **Pull Request (PR)** = A request to the original repository's maintainers saying: "I've made some changes in my Fork — please merge them into the original project!"

1. 回到你 Fork 的仓库页面
2. 你会看到提示 **"This branch is X commits ahead"**，点击 **Contribute** → **Open pull request**
3. 填写 PR 模板中的描述和检查清单
4. 点击 **Create pull request**

---

1. Go back to your forked repository page
2. You should see a prompt saying **"This branch is X commits ahead"** — click **Contribute** → **Open pull request**
3. Fill in the description and checklist in the PR template
4. Click **Create pull request**

---

### 第四步：等待审核 / Step 4: Wait for Review

提交 PR 后，项目的维护者会对你的提交进行审核，常见校验项包括文件规范、格式要求、内容合规性等。

提交内容需遵守文化尊重原则，**若改动内容涉及汉服及相关中华传统文化元素，严禁出现恶意抹黑、歪曲丑化、侮辱诋毁的表述、图像或其他素材，违规提交将直接驳回，不予合并。**

如果校验未通过，系统或维护者会在评论区留下修改说明。你按照提示完成修改后重新推送，PR 会自动更新。

维护者审核通过后，你的 PR 就会被合并。恭喜你完成了一次对此项目的开源贡献！~~（轰，嚓嚓嚓，推推）~~

</details>

# 大饼（功能规划）

## 1. 主界面

- [X] 中○ 添加显示、指定经度的地方时
- [X] 中○ 添加显示 指定时区的区时
- [X] 短○ 更好的日期：支持显示 年-月-日，周\_/周\_ 年-月-日（YYYY-MM-DD）
- [X] 长○ 更好的多农历倒计时（支持设置多例倒计时，目标时间精确到秒，按照从近到远自动排序，倒计时到达后自动换下一个，若没有则提示"倒计时已结束"；支持每例倒计时独立控制是否通知及内容及时长）
- [X] 长○ 更好的多倒计时（支持设置多例倒计时，目标时间精确到秒，按照从近到远自动排序，倒计时到达后自动换下一个，若没有则提示"倒计时已结束"；支持每例倒计时独立控制是否通知及内容及时长）

## 2. 自动化/超级汽车岛

### (1) 触发器（Trigger）

- [X] 短 ○ 精确时间是(YYYY-MM-DD-hh-mm-ss)
- [X] 中 ○ 在每年的(MM-DD-hh-mm-ss)触发（2月29日则是每闰年）
- [X] 中 ○ 在每周的周\_\_，(hh-mm-ss)触发
- [X] 中 ○ 在每天的(hh-mm-ss)触发
- [X] 中 ○ 在每小时的(mm-ss)触发
- [X] 中 ○ 在每分钟的(ss)触发
- [X] 中 ○ 在绝对时间(unix时间戳)触发（单位为秒,注意需用64位有符号整数）（精确到整数）
- [X] 中 ○ 在每月的(DD-hh-mm-ss)触发（若当月不存在该日则跳过）
- [X] 长 ○ 在精确农历日期(L\_YYYY-L\_MM-L\_DD-hh-mm-ss)触发（考虑闰月）
- [X] 长 ○ 在每农历年(L\_MM-L\_DD-hh-mm-ss)触发
- [X] 长 ○ 在农历每月(L\_DD-hh-mm-ss)（选“三十”则是每大月，选“初一～廿九”则每月）（当年不存在该日则跳过）
- [X] 长 ○ 在每农历年(L\_MM)的倒数第n天(hh-mm-ss)触发
- [X] 长 ○ 在\[经度]地方时的\[YYYY-MM-DD-hh-mm-ss]触发
- [X] 长 ○ 在\[时区]区时的(YYYY-MM-DD-hh-mm-ss)触发
- [X] 长 ○在\[经度]地方时每月\[DD-hh-mm-ss]触发
- [X] 长 ○在\[时区]区时每月\[DD-hh-mm-ss]触发
- [X] 长 ○在\[经度]地方时每周\[周\_-hh-mm-ss]触发
- [X] 长 ○在\[时区]区时每周\[周\_-hh-mm-ss]触发
- [X] 长 ○在\[经度]地方时每天\[hh-mm-ss]触发
- [X] 长 ○在\[时区]区时每天\[hh-mm-ss]触发
- [X] 长 ○在\[经度]地方时每小时\[mm-ss]触发
- [X] 长 ○在\[经度]地方时每分钟\[ss]触发
- [X] 长 ○接入SuperAutoIsland blockly

### (2) 条件

- [X] 短 ○ 精确时间在范围\[YYYY-MM-DD-hh-mm-ss]₁ \~ \[YYYY-MM-DD-hh-mm-ss]₂内
- [X] 中 ○ 在每年的\[MM-DD-hh-mm-ss]₁ \~ \[MM-DD-hh-mm-ss]₂内
- [X] 中 ○ 在每月的\[DD-hh-mm-ss]₁ \~ \[DD-hh-mm-ss]₂内
- [X] 中 ○ 在每天的\[hh-mm-ss]₁ \~ \[hh-mm-ss]₂内
- [X] 中 ○ 在每小时的\[mm-ss]₁ \~ \[mm-ss]₂内
- [X] 中 ○ 在每分钟的\[ss]₁ \~ \[ss]₂内
- [X] 长 ○ 在\[经度]地方时间上面6条
- [X] 长 ○ 在\[时区]区时间同前4条
- [X] 长 ○ 在农历\[L\_YYYY-L\_MM-L\_DD-hh-mm-ss]₁ \~ \[L\_YYYY-L\_MM-L\_DD-hh-mm-ss]₂内
- [X] 长 ○ 在农历每年\[L\_MM-L\_DD-hh-mm-dd]₁ \~ \[L\_MM-L\_DD-hh-mm-ss]₂内
- [X] 长 ○ 在农历每月\[L\_DD-hh-mm-ss]₁ \~ \[L\_DD-hh-mm-ss]₂内
- [X] 中 ○ 在绝对时间(unix时间戳)指定范围内(单位为秒,支持小数，精确到3位小数，需64位有符号浮点数)

### (3) 行动

- [X] 中 ○ 从服务器同步ClassIsland时间

## 3. 设置页面

### 顶部通用区域

- [X] 左侧：标注「\[图标]」的方形图标占位框
- [X] 右侧第一行：插件名称「AdvancedTimeIsland」
- [X] 右侧第二行：作者 ID「inf2147483647」+ 按钮「项目主页」
- [X] 标签导航栏：关于 | 时间格式转换 | 插件设置 | 专业名词解释

### (1) 关于

- [X] 显示作者：inf2147483647
- [X] 显示版本：1.0.0.0
- [X] 显示介绍：一个好的插件，先从它的介绍开始

### (2) 时间格式转换

- [X] 清空按钮：单击‘清空’可清除下方输入框全部内容

#### 北京时间转换模块

- [X] 标签：北京时间
- [X] 日期输入框（格式年/月/日/星期；默认空，点击弹出日期选择，选择后替换）
- [X] 时间选择框（默认空）
- [X] 按钮「选取当前时间」
- [X] 转换按钮：转为 Unix 时间戳
- [X] 转换按钮：转为农历
- [X] 转换按钮：转为区时
- [X] 转换按钮：转为地方时

#### Unix 时间戳转换模块

- [X] 标签：Unix 时间戳
- [X] 输入框（仅整数，默认空）
- [X] 按钮「复制」
- [X] 转换按钮：转为北京时间
- [X] 转换按钮：转为农历
- [X] 转换按钮：转为区时
- [X] 转换按钮：转为地方时

#### 农历转换模块

- [X] 标签：农历
- [X] 年份下拉框（默认1984-2043，可选范围：1924-1983；1984-2043；2044-2103）
- [X] 天干下拉框（默认空，甲～癸）
- [X] 生肖下拉框（默认空，子～亥）
- [X] 月份下拉框（默认空，需处理闰月）
- [X] 日期下拉框（默认空）
- [X] 时间选择框（默认空）
- [X] 转换按钮：转为北京时间
- [X] 转换按钮：转为 Unix 时间戳
- [X] 转换按钮：转为区时
- [X] 转换按钮：转为地方时

#### 区时转换模块

- [X] 标签：区时
- [X] 日期输入框（格式年/月/日/星期；默认空，点击日期选择替换）
- [X] 时间选取框（默认空）
- [X] 时区下拉框（默认「中时区」，范围西十二区～东十二区）
- [X] 转换按钮：转为北京时间
- [X] 转换按钮：转为 Unix 时间戳
- [X] 转换按钮：转为农历
- [X] 转换按钮：转为地方时

#### 地方时转换模块

- [X] 标签：地方时
- [X] 日期输入框（格式年/月/日/星期；默认空，点击日期选择替换）
- [X] 时间选取框（默认空）
- [X] 经度输入框（度，3位小数，正东经负西经，取值范围(-180,180]，转换结果四舍五入到秒）
- [X] 转换按钮：转为北京时间
- [X] 转换按钮：转为 Unix 时间戳
- [X] 转换按钮：转为农历
- [X] 转换按钮：转为区时

#### 注意事项

- [X] 提示：转为区时/地方时前，需先选好对应的时区/经度，然后再转换
- [X] 提示：试试连续点顶部通用区域的图标
- [X] 提示：经度处填正数为东经，负数为西经，取值范围为 (-180, 180]

### (3) 插件设置

- [X] 启用农历功能：右侧开关按钮（默认开启）
- [X] 启用区时 / 地方时：右侧开关按钮（默认开启）
- [X] 时间基准：右侧下拉选择框（默认「ClassIsland」，可选：ClassIsland、系统）
- [X] 女装：右侧开关按钮（默认不可见，触发彩蛋后可见且状态开启，关闭后重启恢复不可见）

### (4) 专业名词解释

- [X] 使用 Markdown 解释 Unix 时间戳（含示例：北京时间2026-6-6 12:00:00 → 1780718400）
- [X] 使用 Markdown 解释闰秒（定义，调整规则，加在年末或六月末）
- [X] 使用 Markdown 解释区时（按经度划分24时区，中央经线地方平太阳时，相邻时区差1小时，例：东八区）
- [X] 使用 Markdown 解释时区（经度划分，全球24时区，跨15°，中国用东八区作为国家标准时）

# 接下来的规划

现在上面的功能已经基本实现，现在将会降低开发频率，实现以下功能

- [X] 主界面组件：周期性倒计时
- [ ] 主界面组件：自适应倒计时
- [ ] 主界面组件：农历日期（很多插件都实现了）
- [ ] 开发linux版本 ~~(介于为啥没有Mac版本，手头实在没有苹果设备可供测试了)~~
- [ ] 重新设计UI
- [ ] 新增桌面时间表悬浮窗（置底）
- [ ] 开发CI 1.7版
- [X] 管理启用的功能（issue #1）
- [ ] 添加纪念日功能，并在指定节点（3天，1周，1个月，1年等）发出提醒
- [ ] 就先镇多吧，插件上架后以优化为主，欢迎issue，优化使用体验；一下子吃很多消化不了

# 受[1312333/ClassIsland-TimeOfDayTrigger](https://github.com/1312333/ClassIsland-TimeOfDayTrigger) 启发

- [ ] 多时间点支持：单个触发器可添加任意多个时间点，无需重复创建多条自动化规则
- [ ] 触发日志统计：完整记录每次触发的时间、状态与详情，支持统计查看、一键清空

本项目基于 GNU Lesser General Public License v3.0 获得许可
