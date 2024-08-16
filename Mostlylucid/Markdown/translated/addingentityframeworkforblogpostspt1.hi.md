# ब्लॉग पोस्ट के लिए एंटिटी फ्रेमवर्क जोड़े ( पार्ट 1, डाटाबेस को सेट करें)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024- 0. 911टी0: 53</datetime>

हंस क्योंकि यह एक लंबा हो जाएगा!

आप भाग 2 और 3 देख सकते हैं [यहाँ](/blog/addingentityframeworkforblogpostspt2) और [यहाँ](/blog/addingentityframeworkforblogpostspt3).

## परिचय

जब मैं ब्लॉगिंग पर आधारित अपनी फ़ाइल पर आधारित खुश हूँ, तो मैंने फैसला किया कि मैं पोस्ट पोस्ट पोस्ट तथा टिप्पणीओं को जमा करने के लिए पोस्ट- एजेंटों का उपयोग करने के लिए वहाँ जाने का फैसला करूँगा। इस पोस्ट में मैं दिखाएगा कि कुछ सुझावों और चाल मैं जिस तरह से उठाया है.

[विषय

## डाटाबेस विन्यास किया जा रहा है

पोस्टग्रेस कुछ महान विशेषताओं के साथ एक फ्री डाटाबेस है. मैं लंबे समय से एसक्यूएल सर्वर उपयोगकर्ता (मैं भी कुछ वर्षों के बाद Microsoft के लिए प्रदर्शन प्रदर्शन करता था) लेकिन पोस्टग्रेस एक महान वैकल्पिक विकल्प है. यह मुक्त, खुला स्रोत है, और एक महान समुदाय है; और Plobidin, यह प्रदान करने के लिए औजार है और एसक्यूएल सर्वर प्रबंधन के ऊपर कंधे.

शुरू करने के लिए, आप पोस्टग्रिस और पिमामिन को स्थापित करने की आवश्यकता होगी. आप या तो विंडो सेवा के रूप में या डॉक का उपयोग कर सकते हैं जैसा कि मैंने पिछले पोस्ट में प्रस्तुत किया था [डॉकर](/blog/dockercomposedevdeps).

## ईएफ कोर

इस पोस्ट में मैं पहले ईएफ कोर में कोड का उपयोग कर रहा हूँ, इस तरह आप कोड पूरी तरह से कोड का प्रबंधन कर सकते हैं कोड कोड के माध्यम से. आप निश्चित रूप से डाटाबेस को दस्ती रूप से सेटअप कर सकते हैं और ईएफ को मॉडलों को आकार देने के लिए EF का उपयोग कर सकते हैं. या निश्चित रूप से प्पर या अन्य औज़ार इस्तेमाल करें तथा अपने एसक्यूएल को हाथों से लिखें (या किसी माइक्रोOMOMOMOWOWNK के साथ).

पहली बात जो आपको करना होगा वह ईएफ कोरस पैकेज संस्थापित है. यहाँ मैं उपयोग:

- माइक्रोसॉफ़्ट- से- पर- को- को- को- को- को- को- को- को- ईएफ पैकेज
- MFCTICTENDCT - इसे काम करने के लिए ईएफ कोर औज़ारों के लिए आवश्यक है
- Npegll. segrummramrra. प्रेषित ईएफ कोर के लिए पोस्ट- विग्रेट प्रदान करता है

आप इन पैकेजों को नुतो पैकेज प्रबंधक या डॉटनेट क्लोज I के उपयोग से संस्थापित कर सकते हैं.

फिर हम डाटाबेस वस्तुओं के लिए मॉडल के बारे में सोचने की जरूरत है, ये दृश्य मोडर से अलग हैं जो डेटा को दृश्य में भेजने के लिए इस्तेमाल किया जाता है। मैं ब्लॉग पोस्ट और टिप्पणी के लिए एक सरल मॉडल का उपयोग कर रहा हूँ।

```csharp
public class BlogPost
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public string Title { get; set; }
    public string Slug { get; set; }
    public string HtmlContent { get; set; }
    public string PlainTextContent { get; set; }
    public string ContentHash { get; set; }

    
    public int WordCount { get; set; }
    
    public int LanguageId { get; set; }
    public Language Language { get; set; }
    public ICollection<Comments> Comments { get; set; }
    public ICollection<Category> Categories { get; set; }
    
    public DateTimeOffset PublishedDate { get; set; }
    
}
```

ध्यान दीजिए कि मैंने इन गुणों के एक जोड़े के साथ सजाया है

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

इन ईएफ को सूचित करता है कि आईडी क्षेत्र प्राथमिक कुंजी है और यह डाटाबेस द्वारा उत्पन्न किया जाना चाहिए.

मेरे पास भी वर्ग है

```csharp
public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<BlogPost> BlogPosts { get; set; }
}
```

भाषाएँ

```csharp
public class Language
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<BlogPost> BlogPosts { get; set; }
}
```

और टिप्पणी

```csharp
public class Comments
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Comment { get; set; }
    public string Slug { get; set; }
    public int BlogPostId { get; set; }
    public BlogPost BlogPost { get; set; } 
}
```

आप देख सकते हैं कि मैं टिप्पणीओं में ब्लॉग फ्रेंड का उल्लेख करता हूँ, और B में टिप्पणीओं और वर्गों के बारे में ICols के चुनाव। ये सूचना गुण हैं और कैसे ईएफ को पता चलता है कि मेज़ों के साथ कैसे जुड़ना है.

## डीबी संदर्भ सेट किया जा रहा है

डीबी संदर्भ क्लास में आप तालिकाओं और संबंधों को परिभाषित करने की आवश्यकता होगी। यहाँ मेरा है:

<details>
<summary>Expand to see the full code</summary>
```csharp
public class MostlylucidDbContext : DbContext
{
    public MostlylucidDbContext(DbContextOptions<MostlylucidDbContext> contextOptions) : base(contextOptions)
    {
    }

    public DbSet<Comments> Comments { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<Category> Categories { get; set; }

    public DbSet<Language> Languages { get; set; }


    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasIndex(x => new { x.Slug, x.LanguageId });
            entity.HasIndex(x => x.ContentHash).IsUnique();
            entity.HasIndex(x => x.PublishedDate);

            entity.HasMany(b => b.Comments)
                .WithOne(c => c.BlogPost)
                .HasForeignKey(c => c.BlogPostId);

            entity.HasOne(b => b.Language)
                .WithMany(l => l.BlogPosts).HasForeignKey(x => x.LanguageId);

            entity.HasMany(b => b.Categories)
                .WithMany(c => c.BlogPosts)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogPostCategory",
                    c => c.HasOne<Category>().WithMany().HasForeignKey("CategoryId"),
                    b => b.HasOne<BlogPost>().WithMany().HasForeignKey("BlogPostId")
                );
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasMany(l => l.BlogPosts)
                .WithOne(b => b.Language);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id); // Assuming Category has a primary key named Id

            entity.HasMany(c => c.BlogPosts)
                .WithMany(b => b.Categories)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogPostCategory",
                    b => b.HasOne<BlogPost>().WithMany().HasForeignKey("BlogPostId"),
                    c => c.HasOne<Category>().WithMany().HasForeignKey("CategoryId")
                );
        });
    }
}
```

</details>
मॉडल बनाने के तरीके में...... मैं मेज के बीच संबंधों को परिभाषित करता हूँ. मैं तालिकाओं के बीच संबंध को परिभाषित करने के लिए फ्लू एपीआई इस्तेमाल किया है. यह डाटा एनोटेशन्स के प्रयोग से थोड़ा सा अधिक ROTRENT है लेकिन मैं इसे अधिक पढ़ने योग्य पाता हूँ.

आप देख सकते हैं कि मैं ब्लॉग पोस्ट टेबल पर निर्देशिकाओं के एक जोड़े सेट कर सकते हैं। जब डाटाबेस क्वैरी किया जा रहा हो तो यह प्रदर्शन के साथ मदद करने के लिए है; आपको इन इंडिडेंस को चुनना चाहिए कि आप डाटा को कैसे क्वैरी करेंगे. इस मामले में, Sug, प्रकाशित तिथि और भाषा सभी क्षेत्र हैं मैं पर प्रश्न कर रहा हूँ.

### सेटअप

अब हम अपने मॉडल और डीबी संदर्भ सेट है हम इसे DB में हुक करने की जरूरत है. मेरा हमेशा अभ्यास विस्तार विधियों को जोड़ने के लिए है, यह सब कुछ अधिक व्यवस्थित रखने में मदद करता है:

```csharp
public static class Setup
{
    public static void SetupEntityFramework(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MostlylucidDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    public static async Task InitializeDatabase(this WebApplication app)
    {
        try
        {
            await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
            
            var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();
            await blogService.Populate();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Failed to migrate database");
        }        
    }
}
```

यहाँ मैं डाटाबेस कनेक्शन सेट किया है और फिर उत्प्रवासन चलाएँ. मैं भी डाटाबेस को भरने के लिए एक तरीका कहते हैं (अपने मामले में मैं अभी भी फ़ाइल आधार पर पहुँच रहा हूँ इसलिए मुझे मौजूदा पोस्टों के साथ डाटाबेस को भरने की जरूरत है).

आपका कनेक्शन स्ट्रिंग इस तरह कुछ दिखाई देगा:

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

एक्सटेंशन के उपयोग का मतलब है कि मेरा प्रोग्राम फ़ाइल अच्छा और साफ है.

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

नीचे दिया गया भाग उत्प्रवासन चलाने के लिए है और वास्तव में डेटाबेस को सेटिंग के लिए ज़िम्मेदार है. वह `MigrateAsync` विधि तब बना रहेगा जब यह मौजूद नहीं है और किसी उत्प्रवासन को चालू करे जो आवश्यक है. यह अपने मॉडलों के साथ सिंक में अपने डाटाबेस रखने का महान तरीका है.

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## उत्प्रवासन

एक बार आप यह सब सेट किया है...... आप अपने आरंभिक उत्प्रवासन बनाने की जरूरत है. यह आपके मॉडल के वर्तमान स्थिति का स्नेपशॉट है और डाटाबेस बनाने के लिए प्रयोग में लिया जाएगा. आप यह कर सकते हैं डॉटनेट क्लिक I का उपयोग कर सकते हैं (देखें) [यहाँ](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) यदि आवश्यक हो तो डॉटनेट ईएफ उपकरण संस्थापित करने पर जानकारी के लिए:

```bash
dotnet ef migrations add InitialCreate
```

यह उत्प्रवासन फ़ाइलों के साथ आपके परियोजना में फ़ोल्डर बनाएगा. आप उसके बाद उत्प्रवासन को डाटाबेस के प्रयोग से लागू कर सकते हैं:

```bash
dotnet ef database update
```

यह आपके लिए डाटाबेस तथा तालिका तैयार करेगा.