using Imaginator;
using Imaginator.Components;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IImageProcessingQueue, ImageProcessingQueue>();
builder.Services.AddSingleton<IProcessingStatusService, ProcessingStatusService>();
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();
builder.Services.AddSingleton<IBackgroundProcessingQueue, BackgroundProcessingQueue>();
builder.Services.AddHostedService<BackgroundArchiveWorker>();

// HttpClient один раз, з Timeout
builder.Services.AddHttpClient("serverAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:5214/");
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddControllers();

// Встановлюємо обмеження Kestrel — 2 ГБ
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2L * 1024L * 1024L * 1024L; // 2 GB
    Console.WriteLine($"MaxRequestBodySize: {options.Limits.MaxRequestBodySize}");
});

// Конфігурація для форми, щоб дозволити великі файли — 2 ГБ
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2L * 1024L * 1024L * 1024L; // 2 GB
});

builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
        options.HandshakeTimeout = TimeSpan.FromMinutes(2);
        options.KeepAliveInterval = TimeSpan.FromMinutes(2);
        options.MaximumReceiveMessageSize = 200 * 1024 * 1024; // до 200 MB
    });

builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
{
    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
});

var app = builder.Build();

app.MapControllers();

// Exception handler
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        Console.WriteLine($"❌ GLOBAL ERROR: {exception?.Message}");
        await context.Response.WriteAsync("Something went wrong");
    });
});

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
