using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using ShYCalculator.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
// builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var host = builder.Build();

var js = host.Services.GetRequiredService<IJSRuntime>();
var calculator = new ShYCalculatorInterop();
await js.InvokeVoidAsync("registerShYCalculator", DotNetObjectReference.Create(calculator));

await host.RunAsync();
