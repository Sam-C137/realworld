using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;

namespace RealWorldApi.Infrastructure.ObjectStorage;

public class ObjectStorageService(IAmazonS3 s3, IConfiguration config): IObjectStorageService
{
    private readonly string _bucket = config["R2:BucketName"] ?? throw new Exception("R2:BucketName not set");

    public async Task<string?> UploadAsync(string key, Stream data, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = data,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            Headers = { CacheControl = "public, max-age=31536000" },
            DisablePayloadSigning = true 
        };
        var response = await s3.PutObjectAsync(request);
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK ? $"{config["R2:Domain"]}/{key}" : null;
    }

    public async Task<Stream> DownloadAsync(string key)
    {
        var response = await s3.GetObjectAsync(_bucket, key);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key) =>
        await s3.DeleteObjectAsync(_bucket, key);

    public async Task<List<string>> ListKeysAsync(string prefix = "")
    {
        var response = await s3.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = _bucket,
            Prefix = prefix
        });
        return response.S3Objects.Select(o => o.Key).ToList();
    }

    public string GetPresignedUrl(string key, TimeSpan expiry, HttpVerb verb = HttpVerb.GET)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = verb,
        };
        return s3.GetPreSignedURL(request);
    }
    
    public async Task MultipartUploadAsync(string key, Stream largeStream)
    {
        var transferUtility = new TransferUtility(s3);
        await transferUtility.UploadAsync(new TransferUtilityUploadRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = largeStream,
            PartSize = 6_291_456,  // 6MB per part
            AutoCloseStream = true,
            DisablePayloadSigning = true 
        });
    }
}