namespace ServicesLayer;

public sealed class DocumentTextExtractor : IDocumentTextExtractor
{
    public async Task<string> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;

        if (extension == ".txt")
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        // For unsupported formats, attempt to read as text
        try
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch
        {
            return string.Empty;
        }
    }
}