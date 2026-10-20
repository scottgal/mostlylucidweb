using lucidRESUME.Web;
using LucidResumeHost;

if (args is ["--probe", var site]) return await ReleaseProbe.RunAsync(new Uri(site));

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddLucidResumeCompiler(builder.Configuration);
builder.Services.Configure<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>(options =>
    options.Cookie.Name = "lucidresume.csrf");
builder.Services.AddHostedService<CareerRecordPublisher>();
var app = builder.Build();
app.UseAntiforgery();
app.MapLucidResumeCompiler();
app.Run();
return 0;
