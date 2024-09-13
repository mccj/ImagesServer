using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using RoadFlow.Utility;

using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Resolvers;

/// <summary>
/// Returns images from an <see cref="IFileProvider"/> abstraction.
/// </summary>
public abstract class CustomFileProviderImageProvider : IImageProvider//FileProviderImageProvider
{
    /// <summary>
    /// The file provider abstraction.
    /// </summary>
    private readonly IFileProvider fileProvider;

    /// <summary>
    /// Contains various format helper methods based on the current configuration.
    /// </summary>
    private readonly FormatUtilities formatUtilities;
    private readonly ILogger<CustomFileProviderImageProvider> _logger;
    private readonly IOptions<CustomPhysicalFileSystemProviderOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileProviderImageProvider"/> class.
    /// </summary>
    /// <param name="fileProvider">The file provider.</param>
    /// <param name="processingBehavior">The processing behavior.</param>
    /// <param name="formatUtilities">Contains various format helper methods based on the current configuration.</param>
    protected CustomFileProviderImageProvider(IFileProvider fileProvider, ProcessingBehavior processingBehavior, FormatUtilities formatUtilities, IServiceProvider serviceProvider)
    {
        if (fileProvider == null) throw new ArgumentNullException(nameof(fileProvider));
        if (formatUtilities == null) throw new ArgumentNullException(nameof(formatUtilities));

        this.fileProvider = fileProvider;
        this.formatUtilities = formatUtilities;
        this.ProcessingBehavior = processingBehavior;

        Match = this.IsValidRequest;

        _logger = serviceProvider.GetRequiredService<ILogger<CustomFileProviderImageProvider>>();
        _options = serviceProvider.GetRequiredService<IOptions<CustomPhysicalFileSystemProviderOptions>>();
    }

    /// <inheritdoc/>
    public ProcessingBehavior ProcessingBehavior { get; }

    /// <inheritdoc/>
    public virtual Func<HttpContext, bool> Match { get; set; }

    /// <inheritdoc/>
    public virtual bool IsValidRequest(HttpContext context)
    {
        //var sss = Encrypt("/dome/sixlabors.imagesharp.web.png");
        //var test = this.formatUtilities.TryGetExtensionFromUri(context.Request.GetDisplayUrl(), out _);
        if (context.Request.Path.StartsWithSegments(_options.Value.PathMatch ?? "/Img", StringComparison.OrdinalIgnoreCase, out var key) && key.HasValue)
        {
            var pathDecrypt = key.Value.TrimStart('/').Split(new[] { "/", ".", "?" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(pathDecrypt))
            {
                _logger.LogDebug("路径 {0} 无法通过密匙解析", key.Value);
                return false;
            }
            var path = Decrypt(pathDecrypt);//context.Request.GetDisplayUrl()
            //context.Items.Add("", path);
            var ss = this.formatUtilities.TryGetExtensionFromUri(path, out var ddd_);
            if (!ss)
            {
                _logger.LogDebug("文件 {0} 不是图片格式，或者格式不支持", path);
            }
            return ss;
        }
        return false;
    }

    /// <inheritdoc/>
    public async Task<IImageResolver?> GetAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(_options.Value.PathMatch ?? "/Img", StringComparison.OrdinalIgnoreCase, out var key) && key.HasValue)
        {
            var pathDecrypt = key.Value.TrimStart('/').Split(new[] { "/", ".", "?" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(pathDecrypt))
            {
                _logger.LogDebug("路径 {0} 无法通过密匙解析", key.Value);
                return null;
            }
            var path = Decrypt(pathDecrypt);//context.Request.Path.Value
            var fileInfo = this.fileProvider.GetFileInfo(path);
            if (fileInfo.Exists)
            {
                return await Task.FromResult<IImageResolver?>(new FileProviderImageResolver(fileInfo));
            }
            else
            {
                _logger.LogDebug("路径 {0} 不存在文件", path);
            }
        }
        return null;
    }
    protected abstract string Decrypt(string pToDecrypt);
    //protected abstract string Encrypt(string pToEncrypt);
}

/// <summary>
/// Returns images stored in the local physical file system.
/// </summary>
public sealed class CustomPhysicalFileSystemProvider : CustomFileProviderImageProvider
{
    private readonly IOptions<CustomPhysicalFileSystemProviderOptions> _options;
    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalFileSystemProvider"/> class.
    /// </summary>
    /// <param name="options">The provider configuration options.</param>
    /// <param name="environment">The environment used by this middleware.</param>
    /// <param name="formatUtilities">Contains various format helper methods based on the current configuration.</param>
    public CustomPhysicalFileSystemProvider(
        IOptions<CustomPhysicalFileSystemProviderOptions> options,
#if NETCOREAPP2_1
            IHostingEnvironment environment,
#else
        IWebHostEnvironment environment,
#endif
        FormatUtilities formatUtilities, IServiceProvider serviceProvider)
        : base(GetProvider(options, environment), options.Value.ProcessingBehavior, formatUtilities, serviceProvider)
    {
        _options = options;
    }

    /// <summary>
    /// Determine the provider root path
    /// </summary>
    /// <param name="options">The provider options.</param>
    /// <param name="webRootPath">The web root path.</param>
    /// <param name="contentRootPath">The content root path.</param>
    /// <returns><see cref="string"/> representing the fully qualified provider root path.</returns>
    internal static string GetProviderRoot(CustomPhysicalFileSystemProviderOptions options, string webRootPath, string contentRootPath)
    {
        string providerRootPath = options.ProviderRootPath ?? webRootPath;
        if (string.IsNullOrEmpty(providerRootPath))
        {
            throw new InvalidOperationException("The provider root path cannot be determined, make sure it's explicitly configured or the webroot is set.");
        }

        if (!Path.IsPathFullyQualified(providerRootPath))
        {
            // Ensure this is an absolute path (resolved to the content root path)
            providerRootPath = Path.GetFullPath(providerRootPath, contentRootPath);
        }

        return EnsureTrailingSlash(providerRootPath);
    }
    /// <summary>
    /// Ensures the path ends with a trailing slash (directory separator).
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>
    /// The path with a trailing slash.
    /// </returns>
    internal static string EnsureTrailingSlash(string path)
    {
        if (!string.IsNullOrEmpty(path) &&
            path[path.Length - 1] != Path.DirectorySeparatorChar)
        {
            return path + Path.DirectorySeparatorChar;
        }

        return path;
    }
    private static PhysicalFileProvider GetProvider(
        IOptions<CustomPhysicalFileSystemProviderOptions> options,
#if NETCOREAPP2_1
            IHostingEnvironment environment)
#else
        IWebHostEnvironment environment)
#endif
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (environment == null) throw new ArgumentNullException(nameof(environment));

        return new(GetProviderRoot(options.Value, environment.WebRootPath, environment.ContentRootPath));
    }
    protected override string Decrypt(string pToDecrypt)
    {
        var key = _options.Value.DESKey;
        return EncryptionDes.Decrypt(pToDecrypt, key);
    }
}

public class CustomPhysicalFileSystemProviderOptions : PhysicalFileSystemProviderOptions
{
    public string DESKey { get; set; }
    public string? PathMatch { get; set; }
}