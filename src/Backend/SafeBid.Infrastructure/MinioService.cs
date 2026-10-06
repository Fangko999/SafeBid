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

        var scheme = _endpoint.StartsWith("http") ? "" : "http://";
        return $"{scheme}{_endpoint}/{_bucketName}/{fileName}";
    }

    public async Task<List<string>> MoveFilesToPublicAsync(List<string> oldUrls, CancellationToken ct)
    {
        var publicBucket = "auction-media";
        bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(publicBucket), ct);
        if (!found)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(publicBucket), ct);
            string policy = $@"{{
                ""Statement"": [
                    {{
                        ""Action"": [""s3:GetObject""],
                        ""Effect"": ""Allow"",
                        ""Principal"": ""*"",
                        ""Resource"": [""arn:aws:s3:::{publicBucket}/*""]
                    }}
                ],
                ""Version"": ""2012-10-17""
            }}";
            await _minioClient.SetPolicyAsync(new SetPolicyArgs().WithBucket(publicBucket).WithPolicy(policy), ct);
        }

        var newUrls = new List<string>();
        foreach (var oldUrl in oldUrls)
        {
            try
            {
                var uri = new Uri(oldUrl);
                var pathSegments = uri.AbsolutePath.TrimStart('/').Split('/');
                if (pathSegments.Length >= 2 && pathSegments[0] == _bucketName)
                {
                    var objectName = string.Join("/", pathSegments.Skip(1));
                    
                    // Copy object to public bucket
                    await _minioClient.CopyObjectAsync(new CopyObjectArgs()
                        .WithBucket(publicBucket)
                        .WithObject(objectName)
                        .WithCopyObjectSource(new CopySourceObjectArgs().WithBucket(_bucketName).WithObject(objectName)), ct);

                    // Remove from temp bucket
                    await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                        .WithBucket(_bucketName)
                        .WithObject(objectName), ct);

                    var scheme = _endpoint.StartsWith("http") ? "" : "http://";
                    newUrls.Add($"{scheme}{_endpoint}/{publicBucket}/{objectName}");
                }
                else
                {
                    newUrls.Add(oldUrl);
                }
            }
            catch
            {
                // If it fails, keep the old URL or handle error. We will just throw for now.
                throw;
            }
        }
        return newUrls;
    }
}
