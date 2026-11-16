using Crm.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#root");
builder.Services.AddScoped(_ => new CustomersClient(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }));

await builder.Build().RunAsync();
