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


- [ ] 主界面组件：自适应倒计时
- [ ] 主界面组件：农历日期（很多插件都实现了）
- [ ] 添加纪念日功能，并在指定节点（3天，1周，1个月，1年等）发出提醒 ~~（正计时组件已可累计纪念天数，节点提醒待实现）~~
- [ ] 开发linux版本 ~~(介于为啥没有Mac版本，手头实在没有苹果设备可供测试了)~~
- [ ] 重新设计UI
- [ ] 就先镇多吧，插件上架后以优化为主，欢迎issue，优化使用体验；一下子吃很多消化不了

### 受[1312333/ClassIsland-TimeOfDayTrigger](https://github.com/1312333/ClassIsland-TimeOfDayTrigger) 启发

- [ ] 多时间点支持：单个触发器可添加任意多个时间点，无需重复创建多条自动化规则
- [ ] 触发日志统计：完整记录每次触发的时间、状态与详情，支持统计查看、一键清空

本项目基于 GNU General Public License v3.0 获得许可
