using Microsoft.AspNetCore.Mvc;

namespace DocSenseV1.Dtos
{
    public class UploadRequest
    {
        [FromForm(Name = "file")]
        public IFormFile File { get; set; } = default!;
    }
}
