
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.Services;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddResponseCaching();
builder.Services.AddScoped<BlogService>();

builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseImageSharp();
app.UseStaticFiles();


app.UseRouting();

app.UseAuthorization();


app.UseResponseCaching();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();