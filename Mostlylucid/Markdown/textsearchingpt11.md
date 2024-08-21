﻿# Full Text Searching (Pt 1.1)

<!--category-- Postgres, Alpine -->
<datetime class="hidden">2024-08-21T20:30</datetime>
## Introduction
In the last article I showed you how to set up a full text search using the built in full text search capabilities of Postgres. While I exposed a search api I dodn't have a way to actually use it so...it was a bit of a tease. In this article I'll show you how to use the search api to search for text in your database.

[TOC]

## Searching for text
To add a search capability I had to make some changes to the search api. I added handling for phrases using the `EF.Functions.WebSearchToTsQuery("english", processedQuery)`

```csharp
    private async Task<List<(string Title, string Slug)>> GetSearchResultForQuery(string query)
    {
    

        var processedQuery = query;
        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                (x.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", processedQuery)) // Use precomputed SearchVector for title and content
                || x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", processedQuery)))) // Search in categories
                && x.LanguageEntity.Name == "en")// Filter by language
            
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.WebSearchToTsQuery("english", processedQuery))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug,  })
            .Take(5)
            .ToListAsync();
        return posts.Select(x=> (x.Title, x.Slug)).ToList();
    }
```

This is optionally used when there's a space in the query
```csharp
    if (!query.Contains(" "))
        {
            posts = await GetSearchResultForComplete(query);
        }
        else
        {
            posts = await GetSearchResultForQuery(query);
        }
```
Otherwise I use the existing search method which appends the prefix character.
```csharp
EF.Functions.ToTsQuery("english", query + ":*")

```
## Search Control
Using [Alpine.js](https://alpinejs.dev/) I made a simple Partial control which provides a super simple search box. 

```razor
<div x-data="window.mostlylucid.typeahead()" class="relative">
    <input
        type="text"
        x-model="query"
        x-on:input.debounce.300ms="search"
        x-on:keydown.down.prevent="moveDown"
        x-on:keydown.up.prevent="moveUp"
        x-on:keydown.enter.prevent="selectHighlighted"
        placeholder="Search..."
        class="input input-bordered w-full"
    />

    <!-- Dropdown -->
    <ul x-show="results.length > 0"
        class="absolute z-10 mt-2 w-full bg-white border border-gray-300 rounded-lg shadow-lg">
        <template x-for="(result, index) in results" :key="result.slug">
            <li
                x-on:click="selectResult(result)"
                :class="{
                    'bg-accent': index === highlightedIndex,
                    'hover:bg-accent-content': true
                }"
                class="cursor-pointer text-sm p-2"
                x-text="result.title"
            ></li>
        </template>
    </ul>
</div>
```

Note we have a debounce in here to avoid hammering the server with requests.

## The Typeahead JS
This calls into our JS function (defined in `src/js/main.js`)

```javascript
window.mostlylucid = window.mostlylucid || {};

window.mostlylucid.typeahead = function () {
    return {
        query: '',
        results: [],
        highlightedIndex: -1, // Tracks the currently highlighted index

        search() {
            if (this.query.length < 2) {
                this.results = [];
                this.highlightedIndex = -1;
                return;
            }

            fetch(`/api/search/${encodeURIComponent(this.query)}`)
                .then(response => response.json())
                .then(data => {
                    this.results = data;
                    this.highlightedIndex = -1; // Reset index on new search
                });
        },

        moveDown() {
            if (this.highlightedIndex < this.results.length - 1) {
                this.highlightedIndex++;
            }
        },

        moveUp() {
            if (this.highlightedIndex > 0) {
                this.highlightedIndex--;
            }
        },

        selectHighlighted() {
            if (this.highlightedIndex >= 0 && this.highlightedIndex < this.results.length) {
                this.selectResult(this.results[this.highlightedIndex]);
            }
        },

        selectResult(result) {
           window.location.href = result.url;
            this.results = []; // Clear the results
            this.highlightedIndex = -1; // Reset the highlighted index
        }
    }
}
```

As you can see this is pretty simple (much of the complexity is handling the up and down keys to select results).
This posts to our `SearchApi`
When a result is selected we navigate to the url of the result.

```javascript
     search() {
            if (this.query.length < 2) {
                this.results = [];
                this.highlightedIndex = -1;
                return;
            }

            fetch(`/api/search/${encodeURIComponent(this.query)}`)
                .then(response => response.json())
                .then(data => {
                    this.results = data;
                    this.highlightedIndex = -1; // Reset index on new search
                });
        },
```

## In Conclusion
This adds a powerful yet simple search capability to the site. It's a great way to help users find what they're looking for. 
It gives this site a more professional feel and makes it easier to navigate.
![Search](searchbox.png)