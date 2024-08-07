# 旧的又变新了

## 网络应用发展模式

<datetime class="hidden">2024-07-30-13:30</datetime>

在我的大学(30年)建立网络应用程序的历史中, 有许多方法可以建立网络应用程序。

1. 纯 HTML 1990 - > - 建立网络应用程序的第一个机制( 如果您忽略 BBS / 基于文本的系统) 是普通的 HTML 。 建立一个网页, 列出一系列产品, 并在地址、 电话号码甚至电子邮件中提供邮件, 以发送命令 。
   这有一些好处和(许多)缺点。

- 首先很简单; 你刚刚给出了一堆产品的清单, 用户选择了他们想要的东西, 然后寄了一张支票到地址, 等待得到你的东西
- 很快(在那些日子很重要, 因为大多数人通过调制解调器上网,*千字节*(每秒))
- 曾经是*公平公平*直接更新。 您只需更新 HTML 文件并将其上传到您使用的任何服务器( 最常用的 FTP )
- 邮递服务不快, 支票兑现缓慢等等。

2. [CGG 统计、GGI 统计和GGI](https://webdevelopmenthistory.com/1993-cgi-scripts-and-early-server-side-web-programming/)1993 - 1993 > - 可以说是第一个用于网络的“ 主动” 技术。 您可以使用 C( 我使用的第一语言) 或 Perl 等类似语言生成 HTML 内容

- 您终于要使用“ 现代” 网络的起始点, 这些将使用各种“ 数据” 格式来保存内容, 以及较早期的数据库, 以便让互动水平与“ 现代” 应用程序相仿 。

- 它们是复杂的代码和更新。它们是 CODE, 后一种是用于输出 HTML 的模板语言, 仍然不简单 。

- 否 无*实实数*调试。

- 在您可以接受信用卡的最初几天,这些交易是:*相对*早付款的网关仍然有点荒凉。

3. “ 板块” 语言 (~ 1995 - > ) 。 PHP、 ColdFusion 和 是 ASP ( no. net!) 的类似语言是允许网络应用程序使用“ Rapid Development” 的开始 。

- 更新速度相对较快(仍大多使用FTP)
- 到那时,在电子商务网址上普遍采用SSL,所以你终于能够合理地安全地在网上输入付款细节。
- 数据库已开始成熟,因此现在有可能有一个“适当”数据库系统来处理产品数据、客户数据等。
- 许多新的网站和商店出现, 许多网站和商店都失败了(到2000年代初,

4. 现代时代(2001年- > ) 。 在电子商务刺激的首次潮流之后,开始出现了更“成熟”的网络编程框架,从而可以使用更固定的模式和方法。

- [监监委](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93controller)这其实是一种组织代码的方法,允许将责任划分为应用设计中的有说服力部分。我最初的经验是在J2EE & JSP时代。
- [雷达](https://en.wikipedia.org/wiki/Rapid_application_development)快速应用开发。 如名称所示, 重点是“ 快速让东西起作用 ” 。 这是 ASP. NET ( form 1999 - >) 与 WebForms 框架所遵循的方法 。