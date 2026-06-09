using System.Collections.Concurrent;
using Amazon.S3;
using RealWorldApi.Infrastructure.ObjectStorage;

namespace RealWorldApi.IntegrationTests;

public sealed class TestObjectStorageService : IObjectStorageService
{
    public const string BaseUrl = "https://object-storage.integration.test";
    private readonly ConcurrentDictionary<string, StoredObject> _objects = [];

    public IReadOnlyCollection<StoredObject> Objects => _objects.Values.ToArray();

    public async Task<string?> UploadAsync(string key, Stream data, string contentType)
    {
        using var output = new MemoryStream();
        await data.CopyToAsync(output);
        _objects[key] = new StoredObject(key, output.ToArray(), contentType);
        return $"{BaseUrl}/{key}";
    }

    public Task<Stream> DownloadAsync(string key)
    {
        if (!_objects.TryGetValue(key, out var stored))
        {
            throw new FileNotFoundException($"No test object exists for key '{key}'.", key);
        }

        return Task.FromResult<Stream>(new MemoryStream(stored.Bytes, writable: false));
    }

    public Task DeleteAsync(string key)
    {
        _objects.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public string GetPresignedUrl(string key, TimeSpan expiry, HttpVerb verb = HttpVerb.GET) =>
        $"{BaseUrl}/presigned/{key}?verb={verb}&expiresInSeconds={(int)expiry.TotalSeconds}";

    public Task<List<string>> ListKeysAsync(string prefix = "")
    {
        var keys = _objects.Keys
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        return Task.FromResult(keys);
    }

    public async Task MultipartUploadAsync(string key, Stream largeStream)
    {
        await UploadAsync(key, largeStream, "application/octet-stream");
    }

    public sealed record StoredObject(string Key, byte[] Bytes, string ContentType);
}
