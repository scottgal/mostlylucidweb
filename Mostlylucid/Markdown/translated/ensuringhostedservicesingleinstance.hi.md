# सुनिश्चित करने के लिए अपनी यात्रा (या जीवन यात्रा) एक ही उदाहरण है

<!--category-- ASP.NET -->
<datetime class="hidden">2024- 0. 3121T16: 08</datetime>

## परिचय

यह एक गूंगा छोटा सा लेख है क्योंकि मैं एक बिट उलझन में था कि कैसे मुझे सुनिश्चित करने के लिए `IHostedService` एक ही उदाहरण था । मैं यह वास्तव में यह था की तुलना में एक बिट से अधिक जटिल था. तो मैंने सोचा कि मैं इसके बारे में एक छोटा सा लेख लिखना होगा। बस किसी और के बारे में उलझन में था.

में [पहले लेख](/blog/addingasyncsendingforemails), हम एक पृष्ठभूमि सेवा का उपयोग करने के लिए कैसे कवर किया `IHostedService` ई- मेल भेजने के लिए इंटरफेस. इस लेख में चर्चा की जाएगी कि आप यह कैसे कर सकते हैं `IHostedService` एक ही उदाहरण है ।
यह कुछ के लिए स्पष्ट हो सकता है, लेकिन यह दूसरों के लिए नहीं है (और मेरे लिए तुरंत नहीं था!___

[विषय

## यह मसला क्यों है?

पत्रिका के ज़्यादातर लेखों में इस विषय पर चर्चा की गयी है । `IHostedService` लेकिन वे यह सुनिश्चित करने के लिए कैसे कवर नहीं है कि सेवा एक ही उदाहरण है. यह महत्वपूर्ण है क्योंकि आप सेवा के कई उदाहरण एक साथ समय पर चल रहा नहीं चाहते हैं.

मेरा क्या मतलब है? खैर में ठीक है. आप इस का उपयोग करने के लिए एक IHobed सेवा या Ibawaped जीवन यात्रा को फिर से शुरू करने के लिए रास्ता. (जीवन प्रबंधन प्रबंधन के लिए एक ही तरह से अधिक ओवरराइड के साथ) आप इस का उपयोग करते हैं.

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

क्या इस बैकेंड कोड में कॉल करता है:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

जो ठीक है और dandy है लेकिन क्या होगा यदि आप एक नया संदेश सीधे इस सेवा के लिए पोस्ट करना चाहते हैं एक कहने से `Controller` कार्यवाही?

```csharp

public class ContactController(EmailSenderHostedService sender,ILogger<BaseController> logger) ...
{
   [HttpPost]
    [Route("submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([Bind(Prefix = "")] ContactViewModel comment)
    {
        ViewBag.Title = "Contact";
        //Only allow HTMX requests
        if(!Request.IsHtmx())
        {
            return RedirectToAction("Index", "Contact");
        }
      
        if (!ModelState.IsValid)
        {
            return PartialView("_ContactForm", comment);
        }

        var commentHtml = commentService.ProcessComment(comment.Comment);
        var contactModel = new ContactEmailModel()
        {
            SenderEmail = string.IsNullOrEmpty(comment.Email) ? "Anonymous" : comment.Email,
            SenderName = string.IsNullOrEmpty(comment.Name) ? "Anonymous" : comment.Name,
            Comment = commentHtml,
        };
        await sender.SendEmailAsync(contactModel);
        return PartialView("_Response",
            new ContactViewModel() { Email = comment.Email, Name = comment.Name, Comment = commentHtml });

        return RedirectToAction("Index", "Home");
    }
   }
```

या तो आपको एक इंटरफेस तैयार करना होगा जो स्वयं काम करता है `IHostedService` फिर उस विधि में कॉल करें या आपको यह निश्‍चित करने की ज़रूरत है कि सेवा एक ही उदाहरण है । ऐसा करने का सबसे आसान तरीका है, अपने हालात की जाँच करना ।

### आई- होस्ट- सर्विस

आप यहाँ नोट करेंगे कि यह एक के रूप में सेवा रजिस्टर करता है `IHostedService`, यह इस सेवा के जीवनदायी प्रबंधन के साथ है एक गुप्त के रूप में इस सेवा का उपयोग इस सेवा की घटनाओं को आग में करने के लिए`StartAsync` और `StopAsync` और मैंने उसके लिए अच्छी तरह जीवन-मार्ग समतल किया नीचे देखें, `IHostedlifeCycleService` यह सिर्फ एक अधिक विस्तृत संस्करण है IHHobed सेवा.

```csharp
  /// <summary>
  /// Defines methods for objects that are managed by the host.
  /// </summary>
  public interface IHostedService
  {
    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous Start operation.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous Stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
  }

namespace Microsoft.Extensions.Hosting
{
  /// <summary>
  /// Defines methods that are run before or after
  /// <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StartAsync(System.Threading.CancellationToken)" /> and
  /// <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" />.
  /// </summary>
  public interface IHostedLifecycleService : IHostedService
  {
    /// <summary>
    /// Triggered before <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StartAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StartingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered after <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StartAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StartedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered before <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StoppingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered after <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the stop process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StoppedAsync(CancellationToken cancellationToken);
  }
}
```

## कैसे सुनिश्चित करने के लिए कि आपके आई-Hound सेवा एक ही उदाहरण है कि

### इंटरफेस इंटरेक्शन्सGenericName

इस बारे में सही - सही जानकारी आपके हालात पर निर्भर कर सकती है । यहाँ पर आप से विरासत में जो इंटरफेस जोड़ते हैं वह भी जोड़ते हैं `IHostedService` और फिर उस इंटरफेस के लिए एक विधि जोड़िए जो आप अपने नियंत्रण से कॉल कर सकते हैं।

```csharp
    public interface IEmailSenderHostedService : IHostedService
    {
        Task SendEmailAsync(BaseEmailModel message);
        void Dispose();
    }
```

तो हमें सब की जरूरत है यह एक टन के रूप में रजिस्टर है और फिर इसे हमारे नियंत्रण में इस्तेमाल करते हैं।

```csharp
        services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
```

एनईएसई.NT यह देखने के लिए कि इसके पास सही इंटरफेस है और इस पंजीकरण का उपयोग इस पंजीकरण को चलाने के लिए करेगा `IHostedService`.

### फैक्टरी विधि पहुंचने लायक

यह सुनिश्चित करने के लिए एक और `IHostedService` एक एकल उदाहरण है प्रयोग के लिए `AddSingleton` अपनी सेवा रजिस्टर करने का विधि तब पास करें `IHostedService` एक 'अनुप्रयोग' विधि के रूप में पंजीयन करें. इससे यह निश्‍चित होगा कि आपकी सेवा का केवल एक ही उदाहरण अनुप्रयोग के पूरे जीवनकाल में बनाया जाता है और प्रयोग किया जाता है ।

* ए *फैक्टरी* एक ऐसा तरीका है जिससे किसी चीज़ की एक मिसाल कायम की जा सकती है ।

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

तो आप यहाँ देख के रूप में मैं अपने पहले रजिस्टर `IHostedService` (या `IHostedLifeCycleService`एक एकलटन के रूप में और फिर मैं इस्तेमाल `AddHostedService` सेवा को फैक्टरी विधि के रूप में रजिस्टर करने का विधि. यह निश्‍चित ही इस बात को निश्‍चित करेगा कि अनुप्रयोग के पूरे जीवनकाल में केवल सेवा का एक ही उदाहरण बनाया जाता है और प्रयोग किया जाता है ।

## ऑन्टियम

हमेशा के रूप में वहाँ एक बिल्ली को त्वचा के लिए कुछ तरीके है. इंटरफेस का तरीका शायद सबसे आसान तरीका है, जिससे आप यह तय कर सकते हैं `IHostedService` एक ही उदाहरण है । लेकिन फैक्टरी का तरीक़ा यह निश्‍चित करने का भी एक अच्छा तरीक़ा है कि आपकी सेवा एक ही उदाहरण है । यह आप के पास ले जा रहा है कि आप के लिए ऊपर है. मुझे आशा है कि इस लेख ने आपको यह समझने में मदद दी है कि आपका कैसे भला होगा `IHostedService` एक ही उदाहरण है ।