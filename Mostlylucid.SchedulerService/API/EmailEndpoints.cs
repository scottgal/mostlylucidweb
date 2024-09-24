using Microsoft.AspNetCore.Mvc;
using Mostlylucid.SchedulerService.Services;
using Mostlylucid.Services.Email;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Models.Email;

namespace Mostlylucid.SchedulerService.API;

public static class EmailEndPointsExtension
{
    public static RouteGroupBuilder MapTodosApi(this RouteGroupBuilder group)
    {
        group.MapGet("/triggeremail", ([FromServices] NewsletterSendingService newsletterSendingService) => newsletterSendingService.SendNewsletter(SubscriptionType.EveryPost)).WithName("Email Trigger API");
        group.MapGet("/handler2", () => "Hello").WithName("Email Handler2 API");
        group.MapGet("/send", Send).WithName("Email Send API");
        return group;
    }

    
    private static Task Send([FromServices] IEmailSenderHostedService emailSenderHostedService,[FromBody] BaseEmailModel? message)
    {
        return message == null ? Task.CompletedTask : emailSenderHostedService.SendEmailAsync(message);
    }
}