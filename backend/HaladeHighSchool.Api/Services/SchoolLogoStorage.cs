namespace HaladeHighSchool.Api.Services;

public interface ISchoolLogoStorage
{
    Task<string> SaveAsync(
        IFormFile file,
        Guid schoolId,
        CancellationToken cancellationToken = default);

    bool IsStoredForSchool(string logoUrl, Guid schoolId);

    void Delete(string logoUrl, Guid schoolId);
}

public sealed class SchoolLogoStorageException(string message) : Exception(message);

public sealed class SchoolLogoStorage(
    IWebHostEnvironment environment,
    ILogger<SchoolLogoStorage> logger) : ISchoolLogoStorage
{
    public const long MaximumFileSize = 2 * 1024 * 1024;
    private const string UrlRoot = "/uploads/school-logos/";

    private static readonly IReadOnlyDictionary<string, byte[]> Signatures =
        new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = [0xFF, 0xD8, 0xFF],
            [".jpeg"] = [0xFF, 0xD8, 0xFF],
            [".png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            [".webp"] = "RIFF"u8.ToArray()
        };

    private readonly string _root = Path.GetFullPath(Path.Combine(
        environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
        "uploads",
        "school-logos"));

    public async Task<string> SaveAsync(
        IFormFile file,
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            throw new SchoolLogoStorageException("Choose a non-empty logo image.");
        }

        if (file.Length > MaximumFileSize)
        {
            throw new SchoolLogoStorageException("The school logo must be 2 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!Signatures.TryGetValue(extension, out var signature))
        {
            throw new SchoolLogoStorageException("Only JPEG, PNG and WebP logos are allowed.");
        }

        await using var input = file.OpenReadStream();
        var header = new byte[Math.Max(signature.Length, 12)];
        var read = await input.ReadAsync(header.AsMemory(), cancellationToken);
        if (read < signature.Length || !header.AsSpan(0, signature.Length).SequenceEqual(signature))
        {
            throw new SchoolLogoStorageException("The selected file is not a valid logo image.");
        }

        if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            && (read < 12 || !header.AsSpan(8, 4).SequenceEqual("WEBP"u8)))
        {
            throw new SchoolLogoStorageException("The selected file is not a valid WebP image.");
        }

        var schoolDirectory = schoolId.ToString("N");
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var directory = Path.Combine(_root, schoolDirectory);
        Directory.CreateDirectory(directory);

        input.Position = 0;
        await using (var output = new FileStream(
            Path.Combine(directory, fileName),
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous))
        {
            await input.CopyToAsync(output, cancellationToken);
        }

        return $"{UrlRoot}{schoolDirectory}/{fileName}";
    }

    public bool IsStoredForSchool(string logoUrl, Guid schoolId)
    {
        var prefix = $"{UrlRoot}{schoolId:N}/";
        if (!logoUrl.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var fileName = logoUrl[prefix.Length..];
        if (fileName.Length == 0 || Path.GetFileName(fileName) != fileName)
        {
            return false;
        }

        return File.Exists(Path.Combine(_root, schoolId.ToString("N"), fileName));
    }

    public void Delete(string logoUrl, Guid schoolId)
    {
        if (!IsStoredForSchool(logoUrl, schoolId))
        {
            logger.LogWarning(
                "Rejected invalid logo path {LogoUrl} for school {SchoolId}",
                logoUrl,
                schoolId);
            return;
        }

        var prefix = $"{UrlRoot}{schoolId:N}/";
        var fileName = logoUrl[prefix.Length..];
        try
        {
            File.Delete(Path.Combine(_root, schoolId.ToString("N"), fileName));
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Could not delete school logo {LogoUrl}", logoUrl);
        }
    }
}
