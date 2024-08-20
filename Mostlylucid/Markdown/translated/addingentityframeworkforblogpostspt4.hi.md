# ब्लॉग पोस्ट के लिए एंटिटी फ्रेमवर्क जोड़े (Pt) ४

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024- 0. 1417टी20: 00</datetime>

हिस्से में देखें [1](/blog/addingentityframeworkforblogpostspt1) और [2](/blog/addingentityframeworkforblogpostspt2) और [3](/blog/addingentityframeworkforblogpostspt3) पिछले चरण के लिए.

# परिचय

पहले भाग में हमने इस डाटाबेस को कैसे स्थापित किया, हमारा नियंत्रण और दृष्टिकोण कैसे बनता है, और हमारी सेवाओं ने कैसे कार्य किया । इस भाग में हम कुछ प्रारंभिक डेटा और EF आधारित सेवाओं के साथ डाटाबेस कैसे वंश के लिए विवरणों को कवर करेंगे.

हमेशा के रूप में आप मेरे GiB पर इस सब स्रोत के लिए देख सकते हैं [यहाँ](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog), सबसे पुराने/ ब्लॉग फ़ोल्डर में.

[विषय

# डाटाबेस का निर्माण

पिछले भाग में हम कैसे कवर किया [सेवाओं को प्रारंभ तथा नियत करें](/blog/addingentityframeworkforblogpostspt2#setup)___ इस भाग में हम डाटाबेस को कुछ प्रारंभिक डाटा के साथ कैसे व्यवस्थित करेंगे. यह किया जाता है `EFBlogPopulator` वर्ग. यह वर्ग एक सेवा के रूप में पंजीकृत है `SetupBlog` एक्सटेंशन विधि

```csharp
    public async Task Populate()
    {
        var posts = await _markdownBlogService.GetPages();
        var languages = _markdownBlogService.LanguageList();

        var languageEntities = await EnsureLanguages(languages);
        await EnsureCategoriesAndPosts(posts, languageEntities);

        await Context.SaveChangesAsync();
    }
```

आप में यह देख सकते हैं `Populate` जिस विधि जिसे हम काल कर रहे हैं `_markdownBlogService.GetPages()` यह हमारे mker डाउन फ़ाइलें के माध्यम से चलाता है और एक गुच्छा भरता है `BlogViewModels` जिसमें सभी पोस्ट हैं.
फिर हम भाषाओं के लिए भी ऐसा ही करते हैं; यह हमारी दृष्टि में दिखता है `translated` सभी अनुवादित चिह्न नीचे फ़ाइलों के लिए फ़ोल्डर जो हमने आसानNMT का उपयोग किया है (देखें) [यहाँ](/blog/autotranslatingmarkdownfiles) यह हम कैसे कर सकते हैं के लिए.

## भाषाएँ जोड़ रहे

फिर हम को अपनी तरफ बुला रहे हैं `EnsureLanguages` विधि जो सुनिश्चित करता है कि सभी भाषाएँ डेटाबेस में हैं. यह एक सरल तरीका है जो जांचता है यदि भाषा मौजूद है और इसे डाटाबेस में जोड़ नहीं देता.

```csharp
  private async Task<List<LanguageEntity>> EnsureLanguages(Dictionary<string, List<string>> languages)
    {
        var languageList = languages.SelectMany(x => x.Value).ToList();
        var currentLanguages = await Context.Languages.Select(x => x.Name).ToListAsync();

        var languageEntities = new List<LanguageEntity>();
        var enLang = new LanguageEntity { Name =MarkdownBaseService.EnglishLanguage };

        if (!currentLanguages.Contains(MarkdownBaseService.EnglishLanguage)) Context.Languages.Add(enLang);
        languageEntities.Add(enLang);

        foreach (var language in languageList)
        {
            if (languageEntities.Any(x => x.Name == language)) continue;

            var langItem = new LanguageEntity { Name = language };

            if (!currentLanguages.Contains(language)) Context.Languages.Add(langItem);

            languageEntities.Add(langItem);
        }

        await Context.SaveChangesAsync(); // Save the languages first so we can reference them in the blog posts
        return languageEntities;
    }
```

आप यह Prerettty सरल है और सिर्फ सुनिश्चित करता है कि सभी भाषाएँ हम से प्राप्त की गई सभी भाषाओं डाटाबेस में हैं; और जैसा कि हम निर्दिष्ट करते हैं कि Ids स्वचालित उत्पन्न कर रहे हैं हम की जरूरत है `SaveChanges` यह सुनिश्चित करने के लिए कि आईडी उत्पन्न कर रहे हैं.

### श्रेणियाँ तथा पोस्ट जोड़े

फिर हम को अपनी तरफ बुला रहे हैं `EnsureCategoriesAndPosts` विधि जो सुनिश्चित करता है कि सभी श्रेणियाँ और पोस्ट डाटाबेस में हैं. यह एक बिट से अधिक जटिल के रूप में हम सुनिश्चित करने के लिए की जरूरत है कि श्रेणी डाटाबेस में हैं और फिर हमें सुनिश्चित करने की आवश्यकता है कि पोस्ट्स डाटाबेस में हैं।

```csharp
    private async Task EnsureCategoriesAndPosts(
        IEnumerable<BlogPostViewModel> posts,
        List<LanguageEntity> languageEntities)
    {
        var existingCategories = await Context.Categories.Select(x => x.Name).ToListAsync();

        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var categories = new List<CategoryEntity>();

        foreach (var post in posts)
        {
            var currentPost =
                await PostsQuery().FirstOrDefaultAsync(x => x.Slug == post.Slug && x.LanguageEntity.Name == post.Language);
            await AddCategoriesToContext(post.Categories, existingCategories, categories);
            await AddBlogPostToContext(post, languages[post.Language], categories, currentPost);
        }
    }
```

आप देख सकते हैं कि हम में फोन `PostsQuery` हमारे बेस वर्ग का विधि जो एक सादा विधि है जो कि क्वैरी योग्य है `BlogPostEntity` ताकि हम पोस्ट्स के लिए डाटाबेस क्वैरी कर सकते हैं.

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### श्रेणियाँ जोड़ें

फिर हम उसको धीरे-धीरे अपनी ओर समेट लेते है `AddCategoriesToContext` विधि जो सुनिश्चित करता है कि सभी वर्गों को डेटाबेस में हैं. यह एक सरल तरीका है जो जांचता है यदि वर्ग मौजूद है और इसे डाटाबेस में जोड़ नहीं देता.

```csharp
    private async Task AddCategoriesToContext(
        IEnumerable<string> categoryList,
        List<string> existingCategories,
        List<CategoryEntity> categories)
    {
        foreach (var category in categoryList)
        {
            if (categories.Any(x => x.Name == category)) continue;

            var cat = new CategoryEntity { Name = category };

            if (!existingCategories.Contains(category)) await Context.Categories.AddAsync(cat);

            categories.Add(cat);
        }
    }

```

यह फिर से जांच करता है कि श्रेणी मौजूद है और यदि यह डाटाबेस में जोड़ नहीं जाता है.

#### ब्लॉग पोस्ट जोड़ा जा रहा है

फिर हम उसको धीरे-धीरे अपनी ओर समेट लेते है `AddBlogPostToContext` विधि जो सुनिश्चित करता है कि सभी पोस्ट डाटाबेस में हैं. यह हम सुनिश्चित करने के लिए एक बिट और अधिक जटिल है कि पोस्ट डाटाबेस में है और फिर हमें सुनिश्चित करने की आवश्यकता है कि श्रेणी डाटाबेस में हैं।

```csharp
  private async Task AddBlogPostToContext(
        BlogPostViewModel post,
        LanguageEntity postLanguageEntity,
        List<CategoryEntity> categories,
        BlogPostEntity? currentPost)
    {
        try
        {
            var hash = post.HtmlContent.ContentHash();
            var currentCategoryNames = currentPost?.Categories.Select(x => x.Name).ToArray() ?? Array.Empty<string>();
            var categoriesChanged = false;
            if (!currentCategoryNames.All(post.Categories.Contains) ||
                !post.Categories.All(currentCategoryNames.Contains))
            {
                categoriesChanged = true;
                _logger.LogInformation("Categories have changed for post {Post}", post.Slug);
            }

            var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
            var titleChanged = currentPost?.Title != post.Title;
            if (!titleChanged && !dateChanged && hash == currentPost?.ContentHash && !categoriesChanged)
            {
                _logger.LogInformation("Post {Post} has not changed", post.Slug);
                return;
            }

            var blogPost = new BlogPostEntity
            {
                Title = post.Title,
                Slug = post.Slug,
                HtmlContent = post.HtmlContent,
                PlainTextContent = post.PlainTextContent,
                ContentHash = hash,
                PublishedDate = post.PublishedDate,
                LanguageEntity = postLanguageEntity,
                LanguageId = postLanguageEntity.Id,
                Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList()
            };


            if (currentPost != null)
            {
                _logger.LogInformation("Updating post {Post}", post.Slug);
                Context.BlogPosts.Update(blogPost);
            }
            else
            {
                _logger.LogInformation("Adding post {Post}", post.Slug);
                await Context.BlogPosts.AddAsync(blogPost);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding post {Post}", post.Slug);
        }
    }

```

जैसा कि आप देख सकते हैं यह परिवर्तन का एक परीक्षण है सुनिश्चित करने के लिए कि हम फिर से पोस्टों को नहीं बदल दिया है। हम विषयवस्तु के हैश, वर्गों, तारीख तथा शीर्षक की जाँच करें. यदि इनमें से किसी ने बदल दिया है तो हम इस पोस्ट को डाटाबेस में अद्यतन करते हैं.

एक बात तो यह है कि एक तारीख के समय की जाँच कितनी चिढ़ती है; हमें इसे यूटीसी में बदलना है और फिर इसकी तुलना करने के लिए तारीख मिल जाती है. यह इसलिए है क्योंकि `DateTimeOffset` एक समय घटक है और हम सिर्फ तारीख की तुलना करना चाहते हैं.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# ऑन्टियम

अब हमारे पास एक पूरी तरह से काम कर रहे ब्लॉग तंत्र है जो कि विशिष्ट फ़ाइलों से दूधदानित किए जा सकते हैं तथा उन फ़ाइलों का उपयोग कर रहे हैं. अगले भाग में हम सरल सेवा को कवर करेंगे जो हम डाटाबेस में जमा पोस्ट प्रदर्शित करने के लिए उपयोग करेंगे.