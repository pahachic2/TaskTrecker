using GRAFF.TaskTracker.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization; // Required for AddAuthorizationCore
using Blazored.LocalStorage; // Required for AddBlazoredLocalStorage
using GRAFF.TaskTracker.Web.Services; // Your AuthService location

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register Authentication services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Configure HttpClient. 
// The default HttpClient is configured to use the app's base address.
// If your API is hosted separately, you'll need to change this.
// For example, if API is at https://localhost:7151 (check your API's launchSettings.json)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7151/") }); 
// If API is hosted at the same base address as the Blazor app (e.g. served by ASP.NET Core backend)
// then the default new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) } is fine.

// Register application-specific services
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IColumnService, ColumnService>();
builder.Services.AddScoped<ITaskService, TaskService>();

await builder.Build().RunAsync();
