using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Commands;
using SixLabors.ImageSharp.Web.DependencyInjection;
using SixLabors.ImageSharp.Web.Processors;
using SixLabors.ImageSharp.Web.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddImageSharp()
            .SetRequestParser<QueryCollectionRequestParser>()
            .Configure<PhysicalFileSystemCacheOptions>(options =>
            {
                options.CacheRootPath = null;
                options.CacheFolder = "is-cache";
                options.CacheFolderDepth = 8;
            })
            .SetCache<PhysicalFileSystemCache>()
            .SetCacheKey<UriRelativeLowerInvariantCacheKey>()
            .SetCacheHash<SHA256CacheHash>()
            .Configure<PhysicalFileSystemProviderOptions>(options =>
            {
                options.ProviderRootPath = null;
            })
            .AddProvider<PhysicalFileSystemProvider>()
            .AddProcessor<ResizeWebProcessor>()
            .AddProcessor<FormatWebProcessor>()
            .AddProcessor<BackgroundColorWebProcessor>()
            .AddProcessor<QualityWebProcessor>()
            .AddProcessor<AutoOrientWebProcessor>();

// Add the default service and options.
//
// builder.Services.AddImageSharp();

// Or add the default service and custom options.
//
//builder.Services.AddImageSharp(options =>
//{
//    options.Configuration = Configuration.Default;
//    options.BrowserMaxAge = TimeSpan.FromDays(7);
//    options.CacheMaxAge = TimeSpan.FromDays(365);
//    options.CacheHashLength = 8;
//    options.OnParseCommandsAsync = _ => Task.CompletedTask;
//    options.OnBeforeSaveAsync = _ => Task.CompletedTask;
//    options.OnProcessedAsync = _ => Task.CompletedTask;
//    options.OnPrepareResponseAsync = _ => Task.CompletedTask;
//});

// Or we can fine-grain control adding the default options and configure all other services.
//
//builder.Services.AddImageSharp()
//        .RemoveProcessor<FormatWebProcessor>()
//        .RemoveProcessor<BackgroundColorWebProcessor>();
// Or we can fine-grain control adding custom options and configure all other services
// There are also factory methods for each builder that will allow building from configuration files.
//
//builder.Services.AddImageSharp(options =>
//{
//    options.Configuration = Configuration.Default;
//    options.BrowserMaxAge = TimeSpan.FromDays(7);
//    options.CacheMaxAge = TimeSpan.FromDays(365);
//    options.CacheHashLength = 8;
//    options.OnParseCommandsAsync = _ => Task.CompletedTask;
//    options.OnBeforeSaveAsync = _ => Task.CompletedTask;
//    options.OnProcessedAsync = _ => Task.CompletedTask;
//    options.OnPrepareResponseAsync = _ => Task.CompletedTask;
//})
//.SetRequestParser<QueryCollectionRequestParser>()
//.Configure<PhysicalFileSystemCacheOptions>(options =>
//{
//    options.CacheFolder = "different-cache";
//})
//.SetCache<PhysicalFileSystemCache>()
//.SetCacheKey<UriRelativeLowerInvariantCacheKey>()
//.SetCacheHash<SHA256CacheHash>()
//.ClearProviders()
//.AddProvider<PhysicalFileSystemProvider>()
//.ClearProcessors()
//.AddProcessor<ResizeWebProcessor>()
//.AddProcessor<FormatWebProcessor>()
//.AddProcessor<BackgroundColorWebProcessor>()
//.AddProcessor<QualityWebProcessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Home/Error");
    //// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseImageSharp();
app.UseStaticFiles();

app.Run();
