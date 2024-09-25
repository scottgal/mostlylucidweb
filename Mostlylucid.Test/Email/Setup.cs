using System.Globalization;
using System.Reflection;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;

namespace Mostlylucid.Test.Email;

public class Setup
{
   
}

public class  FluentEmailFake  : IFluentEmail
{
    public IFluentEmail To(string emailAddress, string name = null)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail SetFrom(string emailAddress, string name = null)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail To(string emailAddress)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail To(IEnumerable<Address> mailAddresses)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail CC(string emailAddress, string name = "")
    {
        throw new NotImplementedException();
    }

    public IFluentEmail CC(IEnumerable<Address> mailAddresses)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail BCC(string emailAddress, string name = "")
    {
        throw new NotImplementedException();
    }

    public IFluentEmail BCC(IEnumerable<Address> mailAddresses)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail ReplyTo(string address)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail ReplyTo(string address, string name)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail Subject(string subject)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail Body(string body, bool isHtml = false)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail HighPriority()
    {
        throw new NotImplementedException();
    }

    public IFluentEmail LowPriority()
    {
        throw new NotImplementedException();
    }

    public IFluentEmail UsingTemplateEngine(ITemplateRenderer renderer)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail UsingTemplateFromEmbedded<T>(string path, T model, Assembly assembly, bool isHtml = true)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail UsingTemplateFromFile<T>(string filename, T model, bool isHtml = true)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail UsingCultureTemplateFromFile<T>(string filename, T model, CultureInfo culture, bool isHtml = true)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail UsingTemplate<T>(string template, T model, bool isHtml = true)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail Attach(Attachment attachment)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail Attach(IEnumerable<Attachment> attachments)
    {
        throw new NotImplementedException();
    }

    public SendResponse Send(CancellationToken? token = null)
    {
        throw new NotImplementedException();
    }

    public Task<SendResponse> SendAsync(CancellationToken? token = null)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail AttachFromFilename(string filename, string contentType = null, string attachmentName = null)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail PlaintextAlternativeBody(string body)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail PlaintextAlternativeUsingTemplateFromEmbedded<T>(string path, T model, Assembly assembly)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail PlaintextAlternativeUsingTemplateFromFile<T>(string filename, T model)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail PlaintextAlternativeUsingCultureTemplateFromFile<T>(string filename, T model, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail PlaintextAlternativeUsingTemplate<T>(string template, T model)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail Tag(string tag)
    {
        throw new NotImplementedException();
    }

    public IFluentEmail Header(string header, string body)
    {
        throw new NotImplementedException();
    }

    public EmailData Data { get; set; }
    public ITemplateRenderer Renderer { get; set; }
    public ISender Sender { get; set; }
}