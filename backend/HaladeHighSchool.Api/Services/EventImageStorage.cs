namespace HaladeHighSchool.Api.Services;

public interface IEventImageStorage
{
    Task<IReadOnlyList<string>> SaveAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken = default);

    void Delete(string imageUrl);
}

public sealed class EventImageStorageException(string message) : Exception(message)
{
}

public sealed class EventImageStorage(
    IWebHostEnvironment environment,
    ILogger<EventImageStorage> logger) : IEventImageStorage
{
    public const int MaximumFileCount = 5;
    public const long MaximumFileSize = 5 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, byte[]> Signatures =
        new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = [0xFF, 0xD8, 0xFF],
            [".jpeg"] = [0xFF, 0xD8, 0xFF],
            [".png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            [".gif"] = "GIF8"u8.ToArray(),
            [".webp"] = "RIFF"u8.ToArray()
        };

    private readonly string _root = Path.GetFullPath(Path.Combine(
        environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
        "uploads",
        "events"));

    public async Task<IReadOnlyList<string>> SaveAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        if (files.Count > MaximumFileCount)
        {
            throw new EventImageStorageException($"Choose no more than {MaximumFileCount} images.");
        }
        if (files.Any(file => file.Length == 0))
        {
            throw new EventImageStorageException("Selected image files must not be empty.");
        }

        var selectedFiles = files.ToArray();

        var saved = new List<string>(selectedFiles.Length);
        try
        {
            Directory.CreateDirectory(_root);
            foreach (var file in selectedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var extension = Path.GetExtension(file.FileName);
                if (!Signatures.TryGetValue(extension, out var signature))
                {
                    throw new EventImageStorageException("Only JPEG, PNG, GIF and WebP images are allowed.");
                }

                if (file.Length > MaximumFileSize)
                {
                    throw new EventImageStorageException("Each event image must be 5 MB or smaller.");
                }

                await using var input = file.OpenReadStream();
                var header = new byte[Math.Max(signature.Length, 12)];
                var read = await input.ReadAsync(header.AsMemory(), cancellationToken);
                if (read < signature.Length || !header.AsSpan(0, signature.Length).SequenceEqual(signature))
                {
                    throw new EventImageStorageException($"The file '{Path.GetFileName(file.FileName)}' is not a valid image.");
                }

                if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
                    && (read < 12 || !header.AsSpan(8, 4).SequenceEqual("WEBP"u8)))
                {
                    throw new EventImageStorageException($"The file '{Path.GetFileName(file.FileName)}' is not a valid WebP image.");
                }

                if (extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
                    && read >= 6
                    && !header.AsSpan(0, 6).SequenceEqual("GIF87a"u8)
                    && !header.AsSpan(0, 6).SequenceEqual("GIF89a"u8))
                {
                    throw new EventImageStorageException($"The file '{Path.GetFileName(file.FileName)}' is not a valid GIF image.");
                }

                var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                var fullPath = Path.Combine(_root, storedName);
                input.Position = 0;
                await using (var output = new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous))
                {
                    await input.CopyToAsync(output, cancellationToken);
                }

                var url = $"/uploads/events/{storedName}";
                saved.Add(url);
                logger.LogInformation("Stored event image {StoredName} ({Bytes} bytes)", storedName, file.Length);
            }

            return saved;
        }
        catch
        {
            foreach (var url in saved)
            {
                Delete(url);
            }

            throw;
        }
    }

    public void Delete(string imageUrl)
    {
        const string prefix = "/uploads/events/";
        if (!imageUrl.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        var fileName = imageUrl[prefix.Length..];
        if (fileName.Length == 0 || Path.GetFileName(fileName) != fileName)
        {
            logger.LogWarning("Rejected invalid event image path {ImageUrl}", imageUrl);
            return;
        }

        var path = Path.GetFullPath(Path.Combine(_root, fileName));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Rejected event image path outside storage root {ImageUrl}", imageUrl);
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Could not delete event image {ImageUrl}", imageUrl);
        }
    }
}
