# ईमेल हेतु पृष्ठभूमि भेज रहा है

<!--category-- ASP.NET -->
<datetime class="hidden">2024- 08- 0. 70708: 15</datetime>

♪ इंडस्ट्रीक्शन

मेरे पिछले पोस्ट में मैं स्पष्ट रूप से कैसे ईमेल भेजने के लिए लेकिन इस अंक में ईमेल भेजने में देरी है. SMTP सर्वर को समय सीमा से बाहर करता है और ई- मेल भेजने के लिए कुछ समय ले सकता है. इससे उपभोक्ताओं के लिए ठेस पहुँच सकती है और आपके आवेदन में लॉग्‌म की तरह महसूस हो सकती है ।

इस आसपास जाने का एक तरीका है ई- मेल को पृष्ठभूमि में भेजना. यह जिस तरह से उपयोक्ता ईमेल को भेजने के लिए इंतजार किए बगैर अनुप्रयोग का प्रयोग जारी रख सकता है. यह वेब अनुप्रयोगों में एक सामान्य पैटर्न है तथा एक पृष्ठभूमि कार्य के प्रयोग से प्राप्त किया जा सकता है.

[विषय

## क्यूईएसटी कोर में पृष्ठभूमि विकल्प

VURIG.NT कोर में आप दो मुख्य विकल्प हैं (अधिक उन्नत विकल्प की तरह है वार्मेत / के रूप में)

- यह विकल्प आपको आपके पृष्ठभूमि कार्य के लिए मूल जीवनीय प्रबंधन देता है. आप सेवा शुरू कर सकते हैं और यह पृष्ठभूमि में चला सकते हैं.
- यह विकल्प आपको आपके पृष्ठभूमि कार्यों के जीवन - चक्र पर और अधिक नियंत्रण देता है । आप सेवा शुरू कर सकते हैं और बंद कर सकते हैं और यह पृष्ठभूमि में चला जाएगा लेकिन आप और अधिक नियंत्रण है और बंद, बंद करना बंद, आदि...

इस उदाहरण में मैं एक सरल IHobed सेवा का उपयोग करूंगा पृष्ठभूमि में ईमेल भेजने के लिए.

## स्रोत कोड

इस के लिए पूरा स्रोत नीचे है.

<details>
<summary>Background Email Service</summary>
```csharp
using System.Threading.Tasks.Dataflow;
using Mostlylucid.Email.Models;

namespace Mostlylucid.Email
{
    public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();

        public async Task SendEmailAsync(BaseEmailModel message)
        {
            await _mailMessages.SendAsync(message);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting background e-mail delivery");
            // Start the background task
            _sendTask = DeliverAsync(cancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping background e-mail delivery");

            // Cancel the token to signal the background task to stop
            await cancellationTokenSource.CancelAsync();

            // Wait until the background task completes or the cancellation token triggers
            await Task.WhenAny(_sendTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }

        private async Task DeliverAsync(CancellationToken token)
        {
            logger.LogInformation("E-mail background delivery started");

            while (!token.IsCancellationRequested)
            {
                BaseEmailModel? message = null;
                try
                {if(_mailMessages.Count == 0) continue;
                    message = await _mailMessages.ReceiveAsync(token);
                    switch (message)
                    {
                        case ContactEmailModel contactEmailModel:
                            await emailService.SendContactEmail(contactEmailModel);
                            break;
                        case CommentEmailModel commentEmailModel:
                            await emailService.SendCommentEmail(commentEmailModel);
                            break;
                    }
                    logger.LogInformation("Email from {SenderEmail} sent", message.SenderEmail);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, "Couldn't send an e-mail from {SenderEmail}", message?.SenderEmail);
                    await Task.Delay(1000, token); // Delay and respect the cancellation token
                    if (message != null)
                    {
                        await _mailMessages.SendAsync(message, token);
                    }
                }
            }

            logger.LogInformation("E-mail background delivery stopped");
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }
    }
}
```

</details>
यहाँ आप देख सकते हैं कि हम सेवा की शुरूआत को संभाल सकते हैं और एक नए बफर ब्लॉक को ईमेल को पकड़े रखने के लिए.

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

हमने एक नया कार्य भी स्थापित किया ताकि ई- मेल को पृष्ठभूमि में भेज सकें ।
और जब हम सेवा को रोकने के लिए काम रद्द करने के लिए एक रद्दीकरण

फिर हम होस्ट सर्विस प्रारंभ करें प्रारंभ करें तथा एक ईमेल भेजने के लिए प्रविष्टि बिंदु दें.

```csharp
 public async Task SendEmailAsync(BaseEmailModel message)
        {
            await _mailMessages.SendAsync(message);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting background e-mail delivery");
            // Start the background task
            _sendTask = DeliverAsync(cancellationTokenSource.Token);
            return Task.CompletedTask;
        }
```

अपनी सेटअप क्लास में हम अब DI संग्राहक के साथ सेवा रजिस्टर करने की जरूरत है और होस्ट सर्विस सर्विस

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

अब हम ई-मेल भेजने के लिए ई-मेल भेज सकते हैं ई- मेल की मांग पर ईमेल करने के द्वारा।
उदाहरण के लिए, संपर्क फार्म के लिए हम यह करते हैं.

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

इस संदेश को आगे भेजें कोड में `BufferBlock<BaseEmailModel>` _डाक संदेश और पृष्ठभूमि कार्य इसे उठा लेता है और ई- मेल भेज सकता है.

```csharp
   private async Task DeliverAsync(CancellationToken token)
        {
          ...

            while (!token.IsCancellationRequested)
            {
                BaseEmailModel? message = null;
                try
                {if(_mailMessages.Count == 0) continue;
                    message = await _mailMessages.ReceiveAsync(token);
                    switch (message)
                    {
                        case ContactEmailModel contactEmailModel:
                            await emailService.SendContactEmail(contactEmailModel);
                            break;
                        case CommentEmailModel commentEmailModel:
                            await emailService.SendCommentEmail(commentEmailModel);
                            break;
                    }
                    logger.LogInformation("Email from {SenderEmail} sent", message.SenderEmail);
           ...
            }

            logger.LogInformation("E-mail background delivery stopped");
        }
```

यह तब तक लूप हो जाएगा जब तक हम सेवा को रोक नहीं कर नए ईमेल भेजने के लिए बफर अवरोधित करने के लिए जारी रखें.