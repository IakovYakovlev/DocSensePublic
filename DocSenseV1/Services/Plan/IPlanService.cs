using DocSenseV1.Dtos;

namespace DocSenseV1.Services.Plan
{
    public interface IPlanService
    {
        Task<UploadResponseDto> ExecutePlan(IFormFile file, string user, string subscription);
    }
}
