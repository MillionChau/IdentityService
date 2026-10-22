namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Các hằng số bucket lưu trữ trong MinIO / S3
/// </summary>
public static class StorageBuckets
{
    public const string Avatars = "devradar-avatars";
    public const string Covers = "devradar-covers";
    public const string Models = "devradar-models";
}

/// <summary>
/// Dịch vụ lưu trữ tệp tin và hình ảnh (MinIO / S3 / Cloudflare R2).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Xử lý nén, chuẩn hóa kích thước sang WebP và tải ảnh lên Object Storage.
    /// </summary>
    /// <param name="inputStream">Stream dữ liệu ảnh gốc từ client</param>
    /// <param name="fileName">Tên file mong muốn</param>
    /// <param name="bucketName">Tên bucket (ví dụ: devradar-avatars, devradar-covers)</param>
    /// <param name="maxWidth">Chiều rộng tối đa (mặc định 256px cho avatar)</param>
    /// <param name="maxHeight">Chiều cao tối đa (mặc định 256px cho avatar)</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Public URL truy cập ảnh</returns>
    Task<string> UploadImageAsync(
        Stream inputStream,
        string fileName,
        string bucketName,
        int maxWidth = 256,
        int maxHeight = 256,
        CancellationToken ct = default);

    /// <summary>
    /// Xóa tệp khỏi bucket lưu trữ
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl, string bucketName, CancellationToken ct = default);

    /// <summary>
    /// Đảm bảo Bucket đã tồn tại và cấu hình public-read policy nếu chưa có
    /// </summary>
    Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct = default);
}
