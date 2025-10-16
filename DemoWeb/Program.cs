using Autofac.Core;
using DemoWeb.Services;
using DemoWeb.Services.DomainEndpoints;
using DemoWeb.Services.SupportedLocals;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection.PortableExecutable;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IViewRenderService, ViewRenderService>();
builder.Services.AddScoped<StaticSiteGenerator>();

builder.Services.TryAddSingleton<DomainAppEndpointDataSource>(); // Mvc默认的DefaultEndpointDataSource是单例的
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, DomainAppEndpointMatcherPolicy>());
builder.Services.Configure<DomainApps>(builder.Configuration.GetSection("DomainApps"));

builder.Services.AddScoped<PageCacheService>();
builder.Services.Configure<RouteOptions>(options =>
{
    options.ConstraintMap.Add("SupportedLocals", typeof(LangRouteConstraint));
});
builder.Services.AddRouting(options => { options.ConstraintMap.Add("host", typeof(HostRouteConstraint)); });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<RequestLocalizationMiddleware>();
app.UseMiddleware<PageCachesMiddleware>();

app.UseRouting();

app.UseAuthorization();

app.MapAppleAppEndpoints();
//("CatchAll", "{**catchall}", new { controller = "Error", action = "CatchAll" });
app.MapControllerRoute(
        name: "default",
        pattern: "/{controller=Home}/{action=Index}/{id?}")
    .RequireHost(builder.Configuration.GetSection("HostingDomains")?.Get<string[]>() ?? ["localhost"]);
app.MapFallbackToController("Fallback", "Error");
app.Run();