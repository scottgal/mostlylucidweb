# A Smarter Search Dropdown with HTMX

# Introduction 
In a previous post I showed you how to create a [search dropdown using Alpine.js and HTMX](/blog/textsearchingpt11) then I showed how we could enable Cross-Site Request Forgery protection using the `AntiforgeryRequestToken` in ASP.NET Core with JavaScript [using HTMX to implement a Donut Hole cache](/blog/simpledonutholecachingwithhtmx). One outstanding issue was how it loaded pages.

[TOC]
<!--category-- HTMX, Alpine.js -->
<datetime class="hidden">2024-09-16T22:30</datetime>

# The Problem
The issue was I was using HTMX AJAX to do the requested page loading once you had selected the result from the drop down page. This only KINDA worked.


```javascript
  selectResult(result) {
            htmx.ajax('get', result.url, {
                target: '#contentcontainer',  // The container to update
                swap: 'innerHTML',            // Replace the content inside the target
            }).then(function() {
                history.pushState(null, '', result.url);
                window.scrollTo({
                    top: 0,
                    behavior: 'smooth'
                });
            });
```
The issue was that while this would load the right page and update the shown URL with the new one, it messed up the back button. As the page WASN'T REALLY loaded into the history properly. 

As with my [last article on back button shennanigans](/blog/htmxtomakeyoursitemorespalike) this was something I wanted to fix.

# The Solution
As with previously the solution was to let HTMX handle this directly. To do this I updated my template I use for search results.

## `_typeahead.cshtml`

```html
<div x-data="window.mostlylucid.typeahead()" class="relative" id="searchelement" x-on:click.outside="results = []">
    @Html.AntiForgeryToken()
    <label class="input input-sm bg-neutral-500 bg-opacity-10 input-bordered flex items-center gap-2">
        <input
            type="text"
            x-model="query"
            x-on:input.debounce.200ms="search"
            x-on:keydown.down.prevent="moveDown"
            x-on:keydown.up.prevent="moveUp"
            x-on:keydown.enter.prevent="selectHighlighted"
            placeholder="Search..."
            class="border-0 grow input-sm text-black dark:text-white bg-transparent w-full"/>
        <i class="bx bx-search"></i>
    </label>
    <!-- Dropdown -->
    <ul x-show="results.length > 0"
        id="searchresults"
        class="absolute z-100 my-2 w-full bg-white dark:bg-custom-dark-bg border border-1 text-black dark:text-white border-neutral-600 rounded-lg shadow-lg">
        <template x-for="(result, index) in results" :key="result.slug">
            <li :class="{
                'dark:bg-blue-dark bg-blue-light': index === highlightedIndex,
                'dark:hover:bg-blue-dark hover:bg-blue-light': true
            }"
                class="cursor-pointer text-sm p-2 m-2">
                <!-- These are the key changes.-->
                <a
                    x-on:click="selectResult(index)"
                    @* :href="result.url" *@
                    :hx-get="result.url"
                    hx-target="#contentcontainer"
                    hx-swap="innerHTML"
                    hx-push-url="true"
                    x-text="result.title"
                   >
                </a>
                <-- End of changes -->
            </li>
        </template>
    </ul>

</div>

```
You'll see that I now generate *proper* HTMX links in this code block. Letting us use the correct HTMX behaviour.

## `typeahead.js`

To enable this in my backend JavaScript code I added the following to my search method (shown below). The `this.$nextTick` is an Alpine.js construct that delays this until Alpine has finished processing the template I showed above.

I then use `htmx.process()` on the search element which will ensure the HTMX attributes work as expected. 

``` javascript

.then(data => {
 this.results = data;
this.highlightedIndex = -1; // Reset index on new search
 this.$nextTick(() => {
    htmx.process(document.getElementById('searchresults'));
 });
})
```

<details>
<summary>typeahead.js search </summary>

```javascript
   search() {
            if (this.query.length < 2) {
                this.results = [];
                this.highlightedIndex = -1;
                return;
            }
            let token = document.querySelector('#searchelement input[name="__RequestVerificationToken"]').value;
            console.log(token);
            fetch(`/api/search/${encodeURIComponent(this.query)}`, { // Fixed the backtick and closing bracket
                method: 'GET', // or 'POST' depending on your needs
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token // Attach the AntiForgery token in the headers
                }
            })
                .then(response => {
                    if(response.ok){
                        return  response.json();
                    }
                    return Promise.reject(response);
                })
                .then(data => {
                    this.results = data;
                    this.highlightedIndex = -1; // Reset index on new search
                    this.$nextTick(() => {
                        htmx.process(document.getElementById('searchresults'));
                    });
                })
                .catch((response) => {
                    console.log(response.status, response.statusText);
                    if(response.status === 400)
                    {
                        console.log('Bad request, reloading page to try to fix it.');
                        window.location.reload();
                    }
                    response.json().then((json) => {
                        console.log(json);
                    })
                    console.log("Error fetching search results");
                });
        }
```
</details>

Later on once a page is selected I handle the code to select the page, click on the link an clear the results (to close the search box).

```javascript
selectHighlighted() {
            if (this.highlightedIndex >= 0 && this.highlightedIndex < this.results.length) {
                this.selectResult(this.highlightedIndex);
                
            }
        },

        selectResult(selectedIndex) {
       let links = document.querySelectorAll('#searchresults a');
       links[selectedIndex].click();
            this.$nextTick(() => {
                this.results = []; // Clear the results
                this.highlightedIndex = -1; // Reset the highlighted index
                this.query = ''; // Clear the query
            });
        }
```
This is selected through the onclick of the link in the search results.

```html
 <a
  x-on:click="selectResult(index)"
  :hx-get="result.url"
  hx-target="#contentcontainer"
  hx-swap="innerHTML"
  hx-push-url="true"
  x-text="result.title"
  >
  </a>
```
Which will then load the page and update the URL correctly.

I also have code in the parent box whick allows you to use the arrow keys and enter.

```html
    <label class="input input-sm bg-neutral-500 bg-opacity-10 input-bordered flex items-center gap-2">
        <input
            type="text"
            x-model="query"
            x-on:input.debounce.200ms="search"
            x-on:keydown.down.prevent="moveDown"
            x-on:keydown.up.prevent="moveUp"
            x-on:keydown.enter.prevent="selectHighlighted"
            placeholder="Search..."
            class="border-0 grow input-sm text-black dark:text-white bg-transparent w-full"/>
        <i class="bx bx-search"></i>
    </label>

```

You'll see that this has all the code necessary to enable you to just hit enter and navigate to the selected page.

# In Conclusion
Just a quick update article to the existing search dropdown to enhance the user experience when using search. Again this is a MINIMAL user facing change but just enhances the user experience; who as a web developer are your primary concern (beyond getting paid :)).