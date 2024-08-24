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
        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var currentPosts = await PostsQuery().ToListAsync();
        foreach (var post in posts)
        {
            var existingCategories = Context.Categories.Local.ToList();
            var currentPost =
                currentPosts.FirstOrDefault(x => x.Slug == post.Slug && x.LanguageEntity.Name == post.Language);
            await AddCategoriesToContext(post.Categories, existingCategories);
            existingCategories = Context.Categories.Local.ToList();
            await AddBlogPostToContext(post, languages[post.Language], existingCategories, currentPost);
        }
    }
```

यहाँ पर हम संदर्भ का उपयोग करते हैं. मौजूदा में जोड़ी गई वर्ग को ट्रैक करने के लिए स्थानीय जो संदर्भ में जोड़ा गया है (वे डाटाबेस में सहेजा गया है) `SaveAsync` कॉल.
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
        List<CategoryEntity> existingCategories)
    {
        foreach (var category in categoryList)
        {
            if (existingCategories.Any(x => x.Name == category)) continue;

            var cat = new CategoryEntity { Name = category };

             await Context.Categories.AddAsync(cat);
        }
    }

```

यह फिर से जांच करता है कि श्रेणी मौजूद है और यदि यह डाटाबेस में जोड़ नहीं जाता है.

#### ब्लॉग पोस्ट जोड़ा जा रहा है

फिर हम उसको धीरे-धीरे अपनी ओर समेट लेते है `AddBlogPostToContext` विधि, यह तब कॉल करता है `EFBaseService` डाटाबेस में पोस्ट को सहेजने के लिए.

```csharp
    private async Task AddBlogPostToContext(
        BlogPostViewModel post,
        LanguageEntity postLanguageEntity,
        List<CategoryEntity> categories,
        BlogPostEntity? currentPost)
    {
        await SavePost(post, currentPost, categories, new List<LanguageEntity> { postLanguageEntity });
    }
```

हम इसे फोन द्वारा करते हैं `SavePost` विधि जो कि डाटाबेस में पोस्ट को सहेजता है. यह विधि एक बिट जटिल है जैसा कि यह जाँचने के लिए है कि पोस्ट बदल गया है या नहीं और यदि इस तरह के पोस्ट को डाटाबेस में अद्यतन करें.

```csharp

   public async Task<BlogPostEntity?> SavePost(BlogPostViewModel post, BlogPostEntity? currentPost =null ,
        List<CategoryEntity>? categories = null,
        List<LanguageEntity>? languages = null)
    {
        if (languages == null)
            languages = await Context.Languages.ToListAsync();

    var postLanguageEntity = languages.FirstOrDefault(x => x.Name == post.Language);
        if (postLanguageEntity == null)
        {
            Logger.LogError("Language {Language} not found", post.Language);
            return null;
        }
        categories ??= await Context.Categories.Where(x => post.Categories.Contains(x.Name)).ToListAsync();
         currentPost ??= await PostsQuery().Where(x=>x.Slug == post.Slug).FirstOrDefaultAsync();
        try
        {
            var hash = post.HtmlContent.ContentHash();
            var currentCategoryNames = currentPost?.Categories.Select(x => x.Name).ToArray() ?? Array.Empty<string>();
            var categoriesChanged = false;
            if (!currentCategoryNames.All(post.Categories.Contains) ||
                !post.Categories.All(currentCategoryNames.Contains))
            {
                categoriesChanged = true;
                Logger.LogInformation("Categories have changed for post {Post}", post.Slug);
            }

            var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
            var titleChanged = currentPost?.Title != post.Title;
            if (!titleChanged && !dateChanged && hash == currentPost?.ContentHash && !categoriesChanged)
            {
                Logger.LogInformation("Post {Post} has not changed", post.Slug);
                return currentPost;
            }

            
            var blogPost = currentPost ?? new BlogPostEntity();
            
            blogPost.Title = post.Title;
            blogPost.Slug = post.Slug;
            blogPost.OriginalMarkdown = post.OriginalMarkdown;
            blogPost.HtmlContent = post.HtmlContent;
            blogPost.PlainTextContent = post.PlainTextContent;
            blogPost.ContentHash = hash;
            blogPost.PublishedDate = post.PublishedDate;
            blogPost.LanguageEntity = postLanguageEntity;
            blogPost.Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList();

            if (currentPost != null)
            {
                Logger.LogInformation("Updating post {Post}", post.Slug);
                Context.BlogPosts.Update(blogPost); // Update the existing post
            }
            else
            {
                Logger.LogInformation("Adding new post {Post}", post.Slug);
                Context.BlogPosts.Add(blogPost); // Add a new post
            }
            return blogPost;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error adding post {Post}", post.Slug);
        }

        return null;
    }

```

जैसा कि आप देख सकते हैं यह परिवर्तन का एक परीक्षण है सुनिश्चित करने के लिए कि हम फिर से पोस्टों को नहीं बदल दिया है। हम विषयवस्तु के हैश, वर्गों, तारीख तथा शीर्षक की जाँच करें. यदि इनमें से किसी ने बदल दिया है तो हम इस पोस्ट को डाटाबेस में अद्यतन करते हैं.

एक बात तो यह है कि एक तारीख के समय की जाँच कितनी चिढ़ती है; हमें इसे यूटीसी में बदलना है और फिर इसकी तुलना करने के लिए तारीख मिल जाती है. यह इसलिए है क्योंकि `DateTimeOffset` एक समय घटक है और हम सिर्फ तारीख की तुलना करना चाहते हैं.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# ऑन्टियम

अब हमारे पास एक पूरी तरह से काम कर रहे ब्लॉग तंत्र है जो कि विशिष्ट फ़ाइलों से दूधदानित किए जा सकते हैं तथा उन फ़ाइलों का उपयोग कर रहे हैं. अगले भाग में हम सरल सेवा को कवर करेंगे जो हम डाटाबेस में जमा पोस्ट प्रदर्शित करने के लिए उपयोग करेंगे.