using MediatR;
using SafeBid.Domain;
using System.IO;

namespace SafeBid.Application;

public record UploadMediaCommand(Stream FileStream, string FileName, string ContentType, long Length) : IRequest<Result<string>>;
