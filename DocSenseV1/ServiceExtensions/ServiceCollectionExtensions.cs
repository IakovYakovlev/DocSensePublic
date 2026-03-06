using DocSenseV1.Repositories.Job;
using DocSenseV1.Repositories.Plan;
using DocSenseV1.Repositories.Usage;
using DocSenseV1.Services.AiProvider;
using DocSenseV1.Services.AiProvider.Strategies;
using DocSenseV1.Services.FileReader;
using DocSenseV1.Services.FileReader.Strategies;
using DocSenseV1.Services.Job;
using DocSenseV1.Services.Plan;
using DocSenseV1.Services.Plan.Strategies;
using DocSenseV1.Services.RateLimiter;
using DocSenseV1.Services.TextProcessing;
using DocSenseV1.Services.Usage;

namespace DocSenseV1.ServiceExtensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Регистрируем реализацию IDistributedCache
            services.AddDistributedMemoryCache();

            // Hosted Service для фоновой обработки текста
            services.AddHostedService<TextProcessingWorker>();

            // --- РЕГИСТРАЦИЯ РЕПОЗИТОРИЕВ ---
            services.AddScoped<IUsageRepository, UsageRepository>();
            services.AddScoped<IPlanRepository, PlanRepository>();
            services.AddScoped<IJobRepository, JobRepository>();

            // --- РЕГИСТРАЦИЯ СЕРВИСОВ ---
            services.AddScoped<IFileReaderService, FileReaderService>();
            services.AddScoped<IUsageService, UsageService>();
            services.AddScoped<IPlanService, PlanService>();
            services.AddScoped<ITextProcessingService, TextProcessingService>();
            services.AddScoped<ITextChunk, TextChunk>();
            services.AddScoped<IAiProviderService, AiProviderService>();
            services.AddScoped<IAiProviderFactory, AiProviderFactory>();
            services.AddScoped<IJobService, JobService>();

            // Singleton сервисы
            services.AddSingleton<IJobQueue, JobQueue>();
            services.AddSingleton<IRateLimiterService, RateLimiterService>();

            // --- РЕГИСТРАЦИЯ СТРАТЕГИЙ ФАЙЛОВ ---
            services.AddScoped<IFileReaderStrategy, TextFileReaderStrategy>();
            services.AddScoped<IFileReaderStrategy, DocxFileReaderStrategy>();
            services.AddScoped<IFileReaderStrategy, PdfFileReaderStrategy>();
            services.AddScoped<IFileReaderFactory, FileReaderFactory>();

            // --- РЕГИСТРАЦИЯ СТРАТЕГИЙ ПЛАНОВ ---
            services.AddScoped<IPlanStrategy, BasicPlanStrategy>();
            services.AddScoped<IPlanStrategy, ProPlanStrategy>();
            services.AddScoped<IPlanStrategy, UltraPlanStrategy>();
            services.AddScoped<IPlanStrategyFactory, PlanStrategyFactory>();

            // --- РЕГИСТРАЦИЯ СТРАТЕГИЙ AI ПРОВАЙДЕРОВ ---
            services.AddScoped<IAiProviderStrategy, GeminiProvider>();

            return services;
        }
    }
}
