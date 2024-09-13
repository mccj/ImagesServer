using LogDashboard;
using LogDashboard.Extensions;

using NLog.Web;

using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Commands;
using SixLabors.ImageSharp.Web.DependencyInjection;
using SixLabors.ImageSharp.Web.Processors;
using SixLabors.ImageSharp.Web.Providers;

//var sss = EncryptionDes.Encrypt("dome/sixlabors.imagesharp.web.png");
var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("App_Data/appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"App_Data/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    ;

//builder.Services.AddHttpReports().AddHttpTransport();

builder.Services.AddImageSharp()
            .SetRequestParser<QueryCollectionRequestParser>()
            .Configure<PhysicalFileSystemCacheOptions>(builder.Configuration.GetSection("PhysicalFileSystemCache")
            //options =>
            //{
            //    options.CacheRootPath = null;
            //    options.CacheFolder = "is-cache";
            //    options.CacheFolderDepth = 8;
            //}
            )
            .SetCache<PhysicalFileSystemCache>()
            .SetCacheKey<UriRelativeLowerInvariantCacheKey>()
            .SetCacheHash<SHA256CacheHash>()
            //.Configure<PhysicalFileSystemProviderOptions>(builder.Configuration.GetSection("PhysicalFileSystemProvider")
            .Configure<CustomPhysicalFileSystemProviderOptions>(builder.Configuration.GetSection("PhysicalFileSystemProvider")
            //options =>
            //{
            //  options.ProcessingBehavior = ProcessingBehavior.All;
            //  options.ProviderRootPath = "E:\\Users\\mccj\\source\\repos\\ThreeLive\\HouseSafety\\src\\WebMvc\\App_Data\\FileUploads";
            //}
            )
            .ClearProviders()
            .AddProvider<CustomPhysicalFileSystemProvider>()
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

builder.Services.AddCors();
builder.Services.AddHealthChecks()
//.AddSqlServer(builder.Configuration["ConnectionStrings:connectionString"]);
;

builder.Services.AddLogDashboard(opt =>
{
    var username = builder.Configuration.GetValue<string>("LogDashboard:UserName");
    var password = builder.Configuration.GetValue<string>("LogDashboard:PassWord") ?? "";
    var pathMatch = builder.Configuration.GetValue<string>("LogDashboard:PathMatch");
    var brand = builder.Configuration.GetValue<string>("LogDashboard:Brand");

    if (!string.IsNullOrWhiteSpace(username))
        opt.AddAuthorizationFilter(new LogDashboardBasicAuthorizationFilter(username, password));
    if (!string.IsNullOrWhiteSpace(pathMatch)) opt.PathMatch = pathMatch;
    if (!string.IsNullOrWhiteSpace(brand)) opt.Brand = brand;

    opt.SetRootPath(System.IO.Path.Combine(AppContext.BaseDirectory + "App_Data", "logs"));
    opt.CustomLogModel<LogDashboard.Models.RequestTraceLogModel>();
});

builder.Host.UseNLog();

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

//app.UseHttpsRedirection();

app.UseCors(builder => builder
  //.WithOrigins("*")
  //.WithMethods("*")
  //.WithHeaders("*")
  //.AllowAnyOrigin()
  .SetIsOriginAllowed(origin => true)
  .AllowAnyMethod()
  .AllowAnyHeader()
  .AllowCredentials()
//.WithExposedHeaders("content-disposition", "content-type")
);

app.UseDefaultFiles();
app.UseImageSharp();
app.UseStaticFiles();

//app.UseHttpReports();
app.UseLogDashboard();
app.UseHealthChecks("/health");

app.Run();
