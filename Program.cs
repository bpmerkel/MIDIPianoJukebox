using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Pages services to the DI container
builder.Services.AddRazorPages();

// Add Server Side Blazor services to the DI container
builder.Services.AddServerSideBlazor();

// Add HttpClient to the DI container
builder.Services.AddScoped<HttpClient>();

// Add JukeboxService to the DI container
builder.Services.AddSingleton<JukeboxService>();

// Add MudBlazor services to the DI container
builder.Services.AddMudServices();

// Configure IISServerOptions
builder.Services.Configure<IISServerOptions>(options =>
{
    // Disable automatic authentication
    options.AutomaticAuthentication = false;
});

var app = builder.Build();

// Use developer exception page middleware
app.UseDeveloperExceptionPage();

// Use static files middleware
app.UseStaticFiles();

// Use routing middleware
app.UseRouting();

// Map Blazor SignalR hub
app.MapBlazorHub();

// Map fallback to the page "/_Host"
app.MapFallbackToPage("/_Host");

// Run the application
app.Run();
