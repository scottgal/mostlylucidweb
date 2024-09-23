using System.Net.Mail;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mostlylucid.Shared.Models.Email;
using Polly;

namespace Mostlylucid.Services.Email
{
    public interface IEmailSenderHostedService : IHostedService, IDisposable
    {
        Task SendEmailAsync(BaseEmailModel message);
    }

    public class EmailSenderHostedService : IEmailSenderHostedService
    {
        private readonly Channel<BaseEmailModel> _mailMessages = Channel.CreateUnbounded<BaseEmailModel>();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
        private readonly EmailService _emailService;
        private readonly ILogger<EmailSenderHostedService> _logger;
        private readonly IAsyncPolicy _policyWrap;

        public EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        {
            _emailService = emailService;
            _logger = logger;
            
            // Initialize the retry policy
            var retryPolicy = Policy
                .Handle<SmtpException>() 
                .WaitAndRetryAsync(3, 
                    attempt => TimeSpan.FromSeconds(2 * attempt),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        logger.LogWarning(exception, "Retry {RetryCount} for sending email failed", retryCount);
                    });

            // Initialize the circuit breaker policy
            var circuitBreakerPolicy = Policy
                .Handle<SmtpException>()
                .CircuitBreakerAsync(
                    5,
                    TimeSpan.FromMinutes(1), 
                    onBreak: (exception, timespan) =>
                    {
                        logger.LogError("Circuit broken due to too many failures. Breaking for {BreakDuration}", timespan);
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("Circuit reset. Resuming email delivery.");
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogInformation("Circuit in half-open state. Testing connection...");
                    });
            _policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
            
        }

        public async Task SendEmailAsync(BaseEmailModel message)
        {
            await _mailMessages.Writer.WriteAsync(message);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting background e-mail delivery");
            // Start the background task
            _sendTask = DeliverAsync(cancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping background e-mail delivery");

            // Cancel the token to signal the background task to stop
            await cancellationTokenSource.CancelAsync();
            _mailMessages.Writer.Complete();

            // Wait until the background task completes or the cancellation token triggers
            await Task.WhenAny(_sendTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }

       private async Task DeliverAsync(CancellationToken token)
{
    _logger.LogInformation("E-mail background delivery started");



    try
    {
        while (await _mailMessages.Reader.WaitToReadAsync(token))
        {
            BaseEmailModel? message = null;
            try
            {
                message = await _mailMessages.Reader.ReadAsync(token);

                // Execute retry policy and circuit breaker around the email sending logic
                await _policyWrap.ExecuteAsync(async () =>
                {
                    switch (message)
                    {
                        case ContactEmailModel contactEmailModel:
                            await _emailService.SendContactEmail(contactEmailModel);
                            break;
                        case CommentEmailModel commentEmailModel:
                            await _emailService.SendCommentEmail(commentEmailModel);
                            break;
                        case ConfirmEmailModel confirmEmailModel:
                            await _emailService.SendConfirmationEmail(confirmEmailModel);
                            break;
                    }
                });

                _logger.LogInformation("Email from {SenderEmail} sent", message.SenderEmail);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Couldn't send an e-mail from {SenderEmail}", message?.SenderEmail);
            }
        }
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("E-mail background delivery canceled");
    }

    _logger.LogInformation("E-mail background delivery stopped");
}


        public void Dispose()
        {
            cancellationTokenSource.Dispose();
        }
    }
}