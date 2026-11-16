using Crm.Api;

if (args.FirstOrDefault() == "seed")
{
    Seeder.Run(CrmConfig.DatabasePath);
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.WebHost.UseUrls($"http://localhost:{CrmConfig.Port}");
builder.Services.AddSingleton(_ => new CustomerStore(CrmConfig.DatabasePath));

var app = builder.Build();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapCustomerEndpoints();
app.MapFallbackToFile("index.html");

app.Logger.LogInformation("CRM listening on http://localhost:{Port} (database: {DatabasePath})", CrmConfig.Port, Path.GetFullPath(CrmConfig.DatabasePath));
app.Run();

public partial class Program;
