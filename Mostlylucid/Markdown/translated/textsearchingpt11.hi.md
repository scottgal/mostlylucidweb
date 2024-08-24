# पूरा पाठ ढूंढा जा रहा है (प्रयोग 1. 1)

<!--category-- Postgres, Alpine -->
<datetime class="hidden">2024- 0. 2121टी20: 30</datetime>

## परिचय

में [अंतिम लेख](/blog/textsearchingpt1) मैंने आपको दिखाया कि कैसे पूरे पाठ खोज को पोस्टग्रियों की पूर्ण पाठ खोज क्षमताओं में इस्तेमाल किया जा रहा है. जब मैं एक खोज का पता लगाया मैं वास्तव में इसका उपयोग करने के लिए एक रास्ता नहीं था... यह एक चिढ़ाई का एक सा था. इस लेख में मैं तुम्हें दिखाता हूँ कि अपने डाटाबेस में पाठ ढूंढने के लिए खोज का उपयोग कैसे करें.

इस क्रम में पिछला हिस्सा:

- [पोस्ट- धर्म के साथ पूरा पाठ खोज रहा है](/blog/textsearchingpt1)

इस क्रम में अगले भाग में:

- [ढूंढने के लिए परिचय](/blog/textsearchingpt2)
- [सी# के साथ खोज खोलें](/blog/textsearchingpt3)

यह साइट के शीर्ष पर एक छोटा सा सर्च बक्से जोड़ेगा जो उपयोक्ता को ब्लॉग पोस्ट में पाठ ढूंढने की अनुमति देगा.

![ढूंढें](searchbox.png?format=webp&quality=25)

**ध्यान दीजिए: कमरे में हाथी यह काम करने का सबसे बेहतरीन तरीका नहीं समझता । बहु-पिंजन का समर्थन करने के लिए सुपर जटिल है (मैं प्रति भाषा में एक अलग स्तम्भ की जरूरत होगी) और मैं बनाने के काम और अन्य भाषा विशिष्ट चीजों को संभालने की जरूरत होगी. मैं अब इसके लिए और सिर्फ अंग्रेजी पर ध्यान केंद्रित करने के लिए जा रहा हूँ. LACAS हम इसे खोलने के लिए कैसे संभाल लेंगे.**

[विषय

## पाठ के लिए खोज रहा है

खोज क्षमता जोड़ने के लिए मैं खोज के लिए कुछ परिवर्तन बनाने थे. मैंने वाक्यों को प्रयोग में लाने के लिए जोड़ा `EF.Functions.WebSearchToTsQuery("english", processedQuery)`

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

यह वैकल्पिक रूप से प्रयोग किया जाता है जब प्रश्न में कोई स्थान होता है

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

अन्यथा मैं मौजूदा खोज विधि का उपयोग करता हूँ जो प्रीफ़िक्स जोड़ता है.

```csharp
EF.Functions.ToTsQuery("english", query + ":*")

```

## खोज नियंत्रण

उपयोग में [यू. एस.](https://alpinejs.dev/) मैं एक सरल आंशिक नियंत्रण बनाया जो एक बहुत ही सरल खोज बॉक्स प्रदान करता है.

```razor
<div x-data="window.mostlylucid.typeahead()" class="relative"    x-on:click.outside="results = []">

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

इसमें या तो अंधेरा या लाइट मोड के लिए सही तरह से रेंडर करने के लिए सीएसएस क्लासों का समूह है. यू.एस. कोड बहुत सरल है. यह एक सरल प्रकार का सिर नियंत्रण है कि खोज एक पिल्ला कॉल करते हैं जब उपयोक्ता प्रकार खोज बक्से में.
हमारे पास खोज परिणामों को बंद करने के लिए अप्रचलित करने के लिए एक छोटा - सा कोड भी है ।

```html
   x-on:click.outside="results = []"
```

ध्यान दीजिए कि यहाँ हमारे पास निवेदन के साथ सर्वर को सिजदा करने से बचे रहने के लिए चोरी से बचे रहें.

## टाइप Bagarden. kgm

यह विकल्प हमारे जेएस फंक्शन में बुलाया जाता है (इन में पारिभाषित है) `src/js/main.js`)

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

जैसा कि आप देख सकते हैं, यह काफ़ी सरल है (क्योंकि जटिलताएँ परिणाम चुनने के लिए और नीचे की कुंजियाँ काम कर रही हैं) ।
यह पोस्ट हमारे लिए `SearchApi`
जब परिणाम चुना जाता है तो हम परिणाम के यूआरएल पर नेविगेट करते हैं.

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

### एचएमएमएक्स

मैं HMAX के साथ लाने को भी बदल दिया, यह सिर्फ परिवर्तन `search` HMAX ताज़ा करने का विधि:

```javascript
    selectResult(result) {
    htmx.ajax('get', result.url, {
        target: '#contentcontainer',  // The container to update
        swap: 'innerHTML', // Replace the content inside the target
    }).then(function() {
        history.pushState(null, '', result.url); // Push the new url to the history
    });

    this.results = []; // Clear the results
    this.highlightedIndex = -1; // Reset the highlighted index
    this.query = ''; // Clear the query
}
```

ध्यान दीजिए कि हम भीतरीएचटीएमएल में बदलें `contentcontainer` खोज के परिणाम से. यह पृष्ठ की सामग्री को बिना पृष्ठ के खोज परिणाम के अद्यतन का सरल तरीका है.
इतिहास में हम यूआरएल को नए यूआरएल में भी बदल देते हैं.

## ऑन्टियम

यह साइट में एक शक्तिशाली लेकिन सरल खोज क्षमता जोड़ता है. यह उपभोक्ताओं को वे क्या देख रहे हैं खोजने में मदद करने का एक महान तरीका है.
यह इस साइट को एक अधिक पेशेवर महसूस करता है और समझने में आसान बनाता है ।