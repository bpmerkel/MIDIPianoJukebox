var builder = WebApplication.CreateBuilder(args);

// Add Razor Pages services to the DI container
builder.Services.AddRazorPages();

// Add Server Side Blazor services to the DI container
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // Adjust timeout
        options.HandshakeTimeout = TimeSpan.FromSeconds(15); // Adjust handshake timeout
        options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Adjust keep-alive interval
        options.EnableDetailedErrors = true; // Enable detailed errors
    });

// Add JukeboxService to the DI container
builder.Services.AddSingleton<JukeboxService>();

// Add MudBlazor services to the DI container
builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();

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