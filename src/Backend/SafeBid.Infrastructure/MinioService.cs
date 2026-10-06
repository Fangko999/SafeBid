using Minio;
using Minio.DataModel.Args;
using SafeBid.Application;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SafeBid.Infrastructure;

public class MinioService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName = "temp-media";
    private readonly string _endpoint;

    public MinioService(IMinioClient minioClient, IConfiguration configuration)
    {
        _minioClient = minioClient;
        _endpoint = configuration["MINIO_ENDPOINT"] ?? "localhost:9000";
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct)
    {
        bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName), ct);
        if (!found)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName), ct);
            
            // Set bucket policy to public read
            string policy = $@"{{
                ""Statement"": [
                    {{
                        ""Action"": [""s3:GetObject""],
                        ""Effect"": ""Allow"",
                        ""Principal"": ""*"",
                        ""Resource"": [""arn:aws:s3:::{_bucketName}/*""]
                    }}
                ],
                ""Version"": ""2012-10-17""
            }}";
            await _minioClient.SetPolicyAsync(new SetPolicyArgs().WithBucket(_bucketName).WithPolicy(policy), ct);
        }

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType), ct);

        // We construct the URL. If endpoint doesn't contain http/https, prepend it.
        var scheme = _endpoint.StartsWith("http") ? "" : "http://";
        return $"{scheme}{_endpoint}/{_bucketName}/{fileName}";
    }
}
