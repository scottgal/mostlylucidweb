# Simple 'Donut Hole' Caching with HTMX

# Introduction
Donut hole caching can be a useful technique where you want to cache certain elements of a page but not all. However it can be tricky to implement. In this post I will show you how to implement a simple donut hole caching technique using HTMX.

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024-09-12T16:00</datetime>
[TOC]
# The Problem
One issue I was having with this site is that I wanted to use Anti-forgery tokens with my forms. This is a good security practice to prevent Cross-Site Request Forgery (CSRF) attacks. However, it was causing a problem with the caching of the pages. The Anti-forgery token is unique to each page request, so if you cache the page, the token will be the same for all users. This means that if a user submits a form, the token will be invalid and the form submission will fail. ASP.NET Core prevents this by disabling all caching on request where the Anti-forgery token is used. This is a good security practice, but it means that the page will not be cached at all. This is not ideal for a site like this where the content is mostly static.

# The Solution
A common way around this is 'donut hole' caching where you cache the majority of the page but certain elements. There's a bunch of ways to achieve this in ASP.NET Core using the partial view framework however it's complex to implement and often requires specific packages and config. I wanted a simpler solution.

As I already use the excellent [HTMX](https://htmx.org/examples/lazy-load/) in this project there's a super simple way to get dynamic 'donut hole' functionality by dynamically loading Partials with HTMX.
I already blogged about [using AntiForgeryRequest Tokens with Javascript](/blog/addingxsrfforjavascript) however again the issue was that this effectively disabled caching for the page.

NOW I can reinstate this functionality when using HTMX to dynamically load partials.

```razor
  <li class="group relative mb-1 hidden lg:block ml-2" id="typeaheadelement">
    <div  hx-trigger="load" hx-get="/typeahead" hx-target="#typeaheadelement" hx-swap="innerHTML"></div>
</li>
```
Dead simple, right? All this does is call into the one line of code in the controller that returns the partial view. This means that the Anti-forgery token is generated on the server and the page can be cached as normal. The partial view is loaded dynamically so the token is still unique to each request.

NOTE: You if you use a 'SPA' like approach as I do with HTMX you need to ensure that the `load` event doesn't fire again on the back button. I make this happen by setting the typeahead to overwrite the target on the first load.

This means that the first time it runs it clears the originating div and replaces it with the new content from the partial returned by the controller below. As the Anto-forgery token is generated on the server and stored in a session cookie it should still work with this approach (until I redeploy the app). 

We set the `hx-target` to the outer element mainly to avoid a JS error; as HTMX needs a valid target when completing the request. So you can't remove the element which triggered the HTMX request.

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```
Within the partial we still have the plain simple form with the Anti-forgery token.

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
This then encapsulates all the code for the typeahead search and when it's submitted it pulls the token and adds it to the request (exactly as before).

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

# In Conclusion
This is a super simple way to get 'donut hole' caching with HTMX. It's a great way to get the benefits of caching without the complexity of an extra package. I hope you find this useful. Let me know if you have any questions in the comments below.