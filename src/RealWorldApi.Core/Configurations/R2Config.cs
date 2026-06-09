using Amazon.S3;

namespace RealWorldApi.Core.Configurations;

public static class R2Config
{
    public static void AddR2Config(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = builder.Configuration["R2:Endpoint"],
                ForcePathStyle = true,
            };
            return new AmazonS3Client(builder.Configuration["R2:AccessKey"], builder.Configuration["R2:SecretKey"], config);
        });
    }
}