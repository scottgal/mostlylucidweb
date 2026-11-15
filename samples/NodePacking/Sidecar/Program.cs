using System.Net;
using Grpc.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using PackingTutorial;
using PackingTutorial.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.ConfigureKestrel(options =>
    options.Listen(IPAddress.Loopback, 0, endpoint => endpoint.Protocols = HttpProtocols.Http2));
builder.Services.AddGrpc();
builder.Services.AddSingleton(_ => new Salon(":memory:"));
var app = builder.Build();
app.Lifetime.ApplicationStopped.Register(() => app.Services.GetRequiredService<Salon>().Dispose());
app.MapGrpcService<PackingBenchService>();
await app.StartAsync();
var server = app.Services.GetRequiredService<IServer>();
Console.WriteLine("READY " + server.Features.Get<IServerAddressesFeature>()!.Addresses.Single());
await app.WaitForShutdownAsync();

public sealed class PackingBenchService(Salon salon) : PackingBench.PackingBenchBase
{
    public override Task<JsonReply> FindTogether(SearchRequest request, ServerCallContext context) =>
        Task.FromResult(new JsonReply { Json = salon.FindTogetherJson(new SearchWindow { Start = request.From, End = request.To }) });

    public override Task<JsonReply> Echo(JsonReply request, ServerCallContext context) => Task.FromResult(request);

    public override Task<JsonReply> Memory(Empty request, ServerCallContext context) =>
        Task.FromResult(new JsonReply { Json = BenchmarkProbe.Memory() });
}
