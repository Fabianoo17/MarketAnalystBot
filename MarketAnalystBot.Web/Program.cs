using MarketAnalystBot.Application.Contracts;
using MarketAnalystBot.Application.Services;
using MarketAnalystBot.Infrastructure.Brapi;

var builder = WebApplication.CreateBuilder(args);
string? token = "gE9YjNgLoWdfqSZUHNMtot";

var settings = new BrapiSettings
{
    Token = string.IsNullOrWhiteSpace(token) ? null : token
};
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IBrapiClient>(new BrapiClient(settings));
builder.Services.AddSingleton<IOpportunityEngine, OpportunityEngine>();
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
