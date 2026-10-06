using MediatR;
using SafeBid.Domain;

namespace SafeBid.Application;

public class UploadMediaCommandHandler : IRequestHandler<UploadMediaCommand, Result<string>>
{
    private readonly IStorageService _storageService;

    // 5MB max
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    
    private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "video/mp4" };
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".mp4" };

    public UploadMediaCommandHandler(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<Result<string>> Handle(UploadMediaCommand request, CancellationToken cancellationToken)
    {
        if (request.Length > MaxFileSizeBytes)
        {
            return Result<string>.Failure(new Error("Media.TooLarge", "File size exceeds 5MB limit."));
        }

        if (!_allowedMimeTypes.Contains(request.ContentType.ToLower()))
        {
            return Result<string>.Failure(new Error("Media.InvalidFormat", "Invalid file format."));
        }

        var ext = Path.GetExtension(request.FileName).ToLower();
        if (!_allowedExtensions.Contains(ext))
        {
            return Result<string>.Failure(new Error("Media.InvalidExtension", "Invalid file extension."));
        }

        var uniqueFileName = $"{Guid.NewGuid()}{ext}";

        var url = await _storageService.UploadFileAsync(request.FileStream, uniqueFileName, request.ContentType, cancellationToken);
        
        return Result<string>.Success(url);
    }
}
