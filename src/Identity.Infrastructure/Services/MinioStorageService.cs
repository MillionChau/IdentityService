using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Identity.Application.Common.Interfaces;
using Identity.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Identity.Infrastructure.Services;

public class MinioStorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly MinioStorageSettings _settings;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(
        IOptions<MinioStorageSettings> settings,
        ILogger<MinioStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var endpoint = _settings.Endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? _settings.Endpoint
            : (_settings.UseSsl ? $"https://{_settings.Endpoint}" : $"http://{_settings.Endpoint}");

        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true, // Bắt buộc đối với MinIO
            UseHttp = !_settings.UseSsl
        };

        _s3Client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, config);
    }

    public async Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct = default)
    {
        try
        {
            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
            if (!exists)
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                };
                await _s3Client.PutBucketAsync(putBucketRequest, ct);
                _logger.LogInformation("Created MinIO bucket: {BucketName}", bucketName);

                // Thiết lập quyền Public-Read cho bucket ảnh
                var policy = $$"""
                {
                    "Version": "2012-10-17",
                    "Statement": [
                        {
                            "Sid": "PublicReadGetObject",
                            "Effect": "Allow",
                            "Principal": "*",
                            "Action": ["s3:GetObject"],
                            "Resource": ["arn:aws:s3:::{{bucketName}}/*"]
                        }
                    ]
                }
                """;

                await _s3Client.PutBucketPolicyAsync(new PutBucketPolicyRequest
                {
                    BucketName = bucketName,
                    Policy = policy
                }, ct);
                _logger.LogInformation("Set public read policy for MinIO bucket: {BucketName}", bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure or configure MinIO bucket {BucketName}. Continuing...", bucketName);
        }
    }

    public async Task<string> UploadImageAsync(
        Stream inputStream,
        string fileName,
        string bucketName,
        int maxWidth = 256,
        int maxHeight = 256,
        CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(bucketName, ct);

        // Chuẩn hóa và nén ảnh sang WebP bằng ImageSharp
        using var image = await Image.LoadAsync(inputStream, ct);

        // Crop vuông hoặc resize co giãn theo tỷ lệ
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(maxWidth, maxHeight),
            Mode = ResizeMode.Crop
        }));

        using var memoryStream = new MemoryStream();
        var encoder = new WebpEncoder
        {
            Quality = 85,
            FileFormat = WebpFileFormatType.Lossy
        };

        await image.SaveAsync(memoryStream, encoder, ct);
        memoryStream.Position = 0;

        // Tạo object key duy nhất tránh trùng lặp
        var extension = ".webp";
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var safeName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars()));
        var objectKey = $"{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}_{safeName}{extension}";

        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = memoryStream,
            ContentType = "image/webp",
            AutoCloseStream = false
        };

        await _s3Client.PutObjectAsync(putRequest, ct);

        var publicBaseUrl = _settings.PublicUrl.TrimEnd('/');
        var publicUrl = $"{publicBaseUrl}/{bucketName}/{objectKey}";

        _logger.LogInformation("Uploaded image to MinIO: {PublicUrl} (Size: {Size} bytes)", publicUrl, memoryStream.Length);
        return publicUrl;
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, string bucketName, CancellationToken ct = default)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var path = uri.AbsolutePath.TrimStart('/');
            // Định dạng path: {bucketName}/{objectKey}
            if (path.StartsWith(bucketName, StringComparison.OrdinalIgnoreCase))
            {
                var objectKey = path.Substring(bucketName.Length).TrimStart('/');
                await _s3Client.DeleteObjectAsync(bucketName, objectKey, ct);
                _logger.LogInformation("Deleted object from MinIO: {BucketName}/{ObjectKey}", bucketName, objectKey);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileUrl} from MinIO bucket {BucketName}", fileUrl, bucketName);
            return false;
        }
    }
}
