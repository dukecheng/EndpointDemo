using DemoWeb.Services;
using DemoWeb.Services.DomainEndpoints;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IViewRenderService, ViewRenderService>();
builder.Services.AddScoped<StaticSiteGenerator>();

builder.Services.TryAddSingleton<DomainAppEndpointDataSource>();// Mvc默认的DefaultEndpointDataSource是单例的
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, DomainAppEndpointMatcherPolicy>());

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

app.UseRouting();

app.UseAuthorization();

app.MapAppleAppEndpoints();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
