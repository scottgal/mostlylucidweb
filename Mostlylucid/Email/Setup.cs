using System.Net;
using System.Net.Mail;
using FluentEmail.Core.Interfaces;
using FluentEmail.Smtp;
using Mostlylucid.Config;

namespace Mostlylucid.Email;

public static class Setup
{
    public static void SetupEmail(this IServiceCollection services, IConfiguration config)
    {
        var smtpSettings = config.GetSection(SmtpSettings.Section).Get<SmtpSettings>();
        services.AddSingleton<SmtpSettings>(smtpSettings);

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
        
    }

}