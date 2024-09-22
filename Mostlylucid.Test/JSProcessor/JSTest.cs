using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Mostlylucid.EmailSubscription.Services;

namespace Mostlylucid.EmailSubscription
{
   public class JSTest()
   {


      [Fact]
      public async Task Test()
      {
          var services = new ServiceCollection();
          services.AddTransient<TemplateProcessorService>();
            var serviceProvider = services.BuildServiceProvider();
            var templateProcessorService = serviceProvider.GetService<TemplateProcessorService>();
            var result = await templateProcessorService.ProcessTemplate("test", new Dictionary<string, string>());
      }
   }
}