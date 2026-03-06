using DocSenseV1.Dtos;
using DocSenseV1.Infrastructure;
using DocSenseV1.Services.Job;
using DocSenseV1.Services.Plan;
using DocSenseV1.Services.RateLimiter;
using Microsoft.AspNetCore.Mvc;

namespace DocSenseV1.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IPlanService _planService;
        private readonly IJobService _jobService;
        private readonly IRateLimiterService _rateLimiterService;

        public UploadController(
            IPlanService planService,
            IJobService jobService,
            IRateLimiterService rateLimiterService)
        {
            _planService = planService;
            _jobService = jobService;
            _rateLimiterService = rateLimiterService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [UploadException]
        public async Task<ActionResult<UploadResponseDto>> Upload(
            [FromForm] UploadRequest request,
            [FromHeader(Name = "X-RapidAPI-User")] string user,
            [FromHeader(Name = "X-RapidAPI-Subscription")] string subscription)
        {
            // Проверяем Rate Limiter
            var isAllowed = await _rateLimiterService.IsAllowedAsync(user, subscription);
            if (!isAllowed)
            {
                return StatusCode(429,
                new { message = $"Rate limit exceeded for plan: {subscription}. Max requests per minute exceeded." });
            }

            return Ok(await _planService.ExecutePlan(request.File, user, subscription));
        }

        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetJobResult(
            [FromRoute] string jobId,
            [FromHeader(Name = "X-RapidAPI-User")] string userId,
            [FromHeader(Name = "X-RapidAPI-Subscription")] string subscription)
        {
            // Проверяем Rate Limiter
            var isAllowed = await _rateLimiterService.IsAllowedAsync(userId, subscription);
            if (!isAllowed)
            {
                return StatusCode(429,
                    new { message = $"Rate limit exceeded for plan: {subscription}. Max requests per minute exceeded." });
            }

            var job = await _jobService.GetJobResultAsync(jobId, userId);

            if (job == null)
            {
                return NotFound(new { message = "Job not found or access denied" });
            }

            return Ok(job);
        }
    }
}
