using System.IO;

namespace SafeBid.Application;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct);
    Task<List<string>> MoveFilesToPublicAsync(List<string> oldUrls, CancellationToken ct);
}
