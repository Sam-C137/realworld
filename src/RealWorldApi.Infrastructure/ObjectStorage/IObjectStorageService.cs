using Amazon.S3;

namespace RealWorldApi.Infrastructure.ObjectStorage;

public interface IObjectStorageService
{
    public Task<string?> UploadAsync(string key, Stream data, string contentType);
    public Task<Stream> DownloadAsync(string key);
    public Task DeleteAsync(string key);
    public string GetPresignedUrl(string key, TimeSpan expiry, HttpVerb verb = HttpVerb.GET);
    public Task<List<string>> ListKeysAsync(string prefix = "");
    public Task MultipartUploadAsync(string key, Stream largeStream);
}