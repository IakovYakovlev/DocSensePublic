namespace DocSenseV1.Dtos
{
    public class ErrorResponseDto
    {
        public string? Error { get; set; }
        public string? Message { get; set; }
        public UsageLimitsStats? Details { get; set; }
    }
}
