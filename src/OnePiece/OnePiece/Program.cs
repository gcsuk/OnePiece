using OnePiece.Client.Pages;
using OnePiece.Components;
using OnePiece.Services;
using OnePiece.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Configure options
builder.Services.Configure<AzureVisionOptions>(
    builder.Configuration.GetSection(AzureVisionOptions.SectionName));
builder.Services.Configure<AzureTranslateOptions>(
    builder.Configuration.GetSection(AzureTranslateOptions.SectionName));

// Add Azure Vision service
builder.Services.AddScoped<IAzureVisionService, AzureVisionService>();

// Add Azure Translate service
builder.Services.AddScoped<IAzureTranslateService, AzureTranslateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(OnePiece.Client._Imports).Assembly);

app.Run();
