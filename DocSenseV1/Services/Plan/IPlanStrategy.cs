using DocSenseV1.Dtos;

namespace DocSenseV1.Services.Plan
{
    public interface IPlanStrategy
    {
        bool CanHandle(string planType);
        Task<UploadResponseDto> ExecuteAsync(IFormFile file, string user);
    }
}
