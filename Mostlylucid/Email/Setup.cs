using System.Net;
using System.Net.Mail;
using FluentEmail.Core.Interfaces;
using FluentEmail.Smtp;

namespace Mostlylucid.Email;

public static class Setup
{
    public static void SetupEmail(this IServiceCollection services, IConfiguration config)
    {
        var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));

        services.AddFluentEmail(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .AddRazorRenderer();

        services.AddSingleton<ISender>(new SmtpSender( () => new SmtpClient()
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Host = smtpSettings.Server,
            Port = smtpSettings.Port,
            Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password),
            EnableSsl = smtpSettings.EnableSSL,
            UseDefaultCredentials = false
        }));
        // Register your EmailService as a scoped service if it uses scoped dependencies
        services.AddSingleton<EmailService>();
        services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
        services.AddHostedService<IEmailSenderHostedService>(provider => provider.GetRequiredService<IEmailSenderHostedService>());

        //T0 test email service
// await using  var scope = app.Services.CreateAsyncScope();
// var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
// await emailService.SendCommentEmail("test@test.com", "Test", "Test", "test");
    }

}