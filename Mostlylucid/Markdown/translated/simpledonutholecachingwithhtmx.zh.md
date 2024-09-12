# 简单“ 门洞” 与 HTMX 缓存

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

甜甜圈洞的缓存可以是一种有用的技术, 您可以在其中隐藏页面的某些元素, 但不是全部 。 然而,执行起来可能很困难。 使用HTMX(HTMX)的简单甜甜圈洞缓存技术。

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024-009-12T16:00</datetime>
[技选委

# 问题

我对这个网站有一个问题, 就是我想用我的表格 来使用反伪造的标志。 这是防止跨界请求伪造(CSRF)攻击的良好安全做法。 然而,这却造成网页的封存问题。 反伪造标记是每个页面请求所独有的, 所以如果您隐藏页面, 所有用户都会使用相同的标记 。 这意味着,如果用户提交表格,该符号将无效,而表格提交将失败。 ASP.NET核心防止了这种情况,它应请求,在使用反伪造标记时,停止了所有封存。 这是一个良好的安全做法,但这意味着该页面不会被隐藏。 对于像这样的网站来说,这不是理想的,因为其内容大多是静态的。

# 解决方案

一个常见的方法是“甜甜圈”缓存, 隐藏页面的大部分部分, 但有一些元素 。 在 ASP.NET Core 中,有一系列方法可以实现这一点, 使用部分视图框架, 但是它执行起来很复杂, 常常需要具体的软件包和配置。 我想要一个更简单的解决方案

因为我已经用到极好的了 [HTMX](https://htmx.org/examples/lazy-load/) 在此工程中有一个极简单的方法, 通过动态装入 HTMX 部分, 来获得动态“ 甜甜圈” 功能 。
我已经在博客上写了 [使用带有 Javascript 的反伪造请求 Tokens](/blog/addingxsrfforjavascript) 然而,问题仍然是,这实际上使页面的缓存功能被切断。

现在,我可以恢复这个功能 当使用 HTMX 动态装入部分时。

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

很简单吧? 所有这一切都是对控制器中返回部分视图的单行代码的调用。 这意味着在服务器上生成了反伪造标记, 并且页面可以与正常一样缓存 。 部分视图是动态装入的, 所以符号仍然为每个请求所独有 。

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

我们的片段 仍然有简单的形式 与反伪造的标志。

```razor
<div x-data="window.mostlylucid.typeahead()" class="relative" id="searchelement"  x-on:click.outside="results = []">
    @Html.AntiForgeryToken()
    <label class="input input-sm dark:bg-custom-dark-bg bg-white input-bordered flex items-center gap-2">
        <input
            type="text"
            x-model="query"
            x-on:input.debounce.300ms="search"
            x-on:keydown.down.prevent="moveDown"
            x-on:keydown.up.prevent="moveUp"
            x-on:keydown.enter.prevent="selectHighlighted"
            placeholder="Search..."
            class="border-0 grow  input-sm text-black dark:text-white bg-transparent w-full"/>
        <i class="bx bx-search"></i>
    </label>
    <!-- Dropdown -->
    <ul x-show="results.length > 0"
        class="absolute z-10 my-2 w-full bg-white dark:bg-custom-dark-bg border border-1 text-black dark:text-white border-b-neutral-600 dark:border-gray-300   rounded-lg shadow-lg">
        <template x-for="(result, index) in results" :key="result.slug">
            <li
                x-on:click="selectResult(result)"
                :class="{
                    'dark:bg-blue-dark bg-blue-light': index === highlightedIndex,
                    'dark:hover:bg-blue-dark hover:bg-blue-light': true
                }"
                class="cursor-pointer text-sm p-2 m-2"
                x-text="result.title"
            ></li>
        </template>
    </ul>
</div>
```

然后,它包罗了打印头搜索的所有代码, 当它提交时, 它会拉动标语并添加到请求中( 和以前完全一样 ) 。

```javascript
        let token = document.querySelector('#searchelement input[name="__RequestVerificationToken"]').value;
            console.log(token);
            fetch(`/api/search/${encodeURIComponent(this.query)}`, { // Fixed the backtick and closing bracket
                method: 'GET', // or 'POST' depending on your needs
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token // Attach the AntiForgery token in the headers
                }
            })
```

# 在结论结论中

这是用HTMX卡住"甜甜圈"的超级简单方法。 这是一个伟大的方法 获得利益 收缩 没有复杂的 额外的包包。 我希望你觉得这很有用。 如果在下文的评论中有任何问题,请通知我。