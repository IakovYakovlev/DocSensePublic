using DocSenseV1.Dtos;
using DocSenseV1.Exceptions;
using DocSenseV1.Models;
using DocSenseV1.Repositories.Plan;
using DocSenseV1.Services.FileReader;
using DocSenseV1.Services.Job;
using DocSenseV1.Services.TextProcessing;
using DocSenseV1.Services.Usage;

namespace DocSenseV1.Services.Plan.Strategies
{
    public abstract class BasePlanStrategy : IPlanStrategy
    {
        protected abstract string PlanType { get; }

        private readonly IFileReaderService _fileReader;
        private readonly IPlanRepository _planRepo;
        private readonly IUsageService _usage;
        private readonly IJobService _jobService;

        protected BasePlanStrategy(
            IFileReaderService fileReader, 
            IPlanRepository planRepo,
            IUsageService usage,
            IJobService jobService)
        {
            _fileReader = fileReader;
            _planRepo = planRepo;
            _usage = usage;
            _jobService = jobService;
        }

        public bool CanHandle(string planType)
        {
            return PlanType.Equals(planType, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<UploadResponseDto> ExecuteAsync(IFormFile file, string user)
        {
            // 1. Прочитать файл (ReadService -> Read())
            // 2. Посчитать количество символов (простым методом)
            var content = await _fileReader.ReadFileAsync(file);
            var symbols = content.Length;

            // 3. Проверить лимиты для free плана по данному юзеру (UsageService -> CheckLimits(user, plan, symbols))
            // 4. Если лимиты 
            //  4.1. Превышены - тправить ошибку с детальной информацией по лимитам
                    /* {
                          message: `Usage limit reached for your ${this.getPlanName()} plan.`,
                          limits: usageCheck.stats,
                     * }
            //  4.2. Не превышены, отправить текст на обработку в LLM сервис, (TextProcessingService -> ProcessAsync())
             */
            var planModel = await GetPlanModelAsync();
            var usageCheck = await _usage.CheckLimitsAsync(user, planModel, symbols);
            if(!usageCheck.Allowed)
            {
                throw new UsageLimitException(
                    $"Usage limit reached for your {planModel.Type} plan.", usageCheck.Stats);
            }

            // Обновляем использование
            UsageModel? usage = await _usage.IncrementUsageAsync(user, planModel.Type, symbols);

            // 5. Получаем резутат от LLM сервиса (json)
            var jobId = Guid.NewGuid();
            await _jobService.CreateJobAsync(content, user, planModel.Id, jobId.ToString());

            // Вот
            return new UploadResponseDto
            {
                Status = "pending",
                Plan = PlanType,
                Result = null,
                JobId = jobId,
                Stats = new UsageLimitsStats
                {
                    Symbols = new SymbolsMetric
                    {
                        Used = usage?.TotalSymbols ?? 0,
                        Limit = planModel.LimitSymbols,
                        Remaining = Math.Max(planModel.LimitSymbols - (usage?.TotalSymbols ?? 0), 0),
                        RequestedSymbols = symbols,
                    },
                    Requests = new UsageLimitMetric
                    {
                        Used = usage?.TotalRequests ?? 0,
                        Limit = planModel.LimitRequests,
                        Remaining = Math.Max(planModel.LimitRequests - (usage?.TotalRequests ?? 0), 0),
                    }
                }
            };
        }

        protected async Task<PlanModel> GetPlanModelAsync()
        {
            var plan = await _planRepo.GetPlanByTypeAsync(PlanType);

            if (plan == null)
            {
                throw new Exception($"Configuration Error: Plan model for type '{PlanType}' not found in datase.");
            }

            return plan;
        }
    }
}
