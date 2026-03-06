namespace DocSenseV1.Dtos
{
    public class UploadResponseDto
    {
        public string Status { get; set; } = "done";
        public string Plan { get; set; } = String.Empty;
        public UsageLimitsStats Stats { get; set; } = new UsageLimitsStats();
        public string? Result { get; set; }
        public Guid? JobId { get; set; }
    }
}
