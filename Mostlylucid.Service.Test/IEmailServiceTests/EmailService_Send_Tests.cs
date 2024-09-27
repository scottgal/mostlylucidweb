using System.Reflection;
using FakeItEasy;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging.Testing;
using Mostlylucid.Services.Email;
using Mostlylucid.Shared.Config;
using Mostlylucid.Shared.Models.Email;

namespace Mostlylucid.Service.Test.IEmailServiceTests;

public class EmailService_Send_Tests
{
    private readonly string _toEmail = "test@test.com";
    private string _senderEmail = "test1@test.com";
    private readonly CancellationToken ct = new CancellationToken();
    private readonly string _senderName = "Test";
    private SendResponse _sendResponse;
    private Fake<IFluentEmail> GetEmailService()
    {
        var smtpSettings = new SmtpSettings();
        smtpSettings.ToMail =  _toEmail;
        var fluentEmail = new FakeItEasy.Fake<IFluentEmail>();
        fluentEmail.CallsTo(x => 
            x.UsingTemplateFromEmbedded(A<string>.Ignored, A<object>.Ignored, A<Assembly>.Ignored, true)).Returns(fluentEmail.FakedObject);
        fluentEmail.CallsTo(x => x.To(A<string>.Ignored)).Returns(fluentEmail.FakedObject);
        fluentEmail.CallsTo(x => x.SetFrom(A<string>.Ignored, A<string>.Ignored)).Returns(fluentEmail.FakedObject);
        fluentEmail.CallsTo(x => x.Subject(A<string>.Ignored)).Returns(fluentEmail.FakedObject);
        fluentEmail.CallsTo(x => x.SendAsync(ct)).Returns(_sendResponse);
        
        return fluentEmail;
    }

    [Fact]
    public async Task Test_Send()
    {
        var fakeLogger =new FakeLogger<EmailService>();
        var fluentMail = GetEmailService();
        
        var emailService = new EmailService(new SmtpSettings(), fluentMail.FakedObject,fakeLogger);
        var emailModel = new BaseEmailModel();
        
        
    }
}