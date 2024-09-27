using Microsoft.AspNetCore.Mvc;
using Mostlylucid.SchedulerService.Services;
using Mostlylucid.Services.Email;
using Mostlylucid.Shared.Models.Email;

namespace Mostlylucid.SchedulerService.API;

public static class EmailEndPointsExtension
{
    public static RouteGroupBuilder MapTodosApi(this RouteGroupBuilder group)
    {
        group.MapGet("/sendfortoken", ([FromServices] NewsletterSendingService newsletterSendingService, [FromBody] string token) => newsletterSendingService.SendImmediateEmailForSubscription(token)).WithName("Email Trigger API for Token");

        group.MapGet("/send", Send).WithName("Email Send API");
        return group;
    }

    
    private static Task Send([FromServices] IEmailSenderHostedService emailSenderHostedService,[FromBody] BaseEmailModel? message)
    {
        return message == null ? Task.CompletedTask : emailSenderHostedService.SendEmailAsync(message);
    }
}