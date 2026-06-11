using PresentationLayer.Security;

namespace PresentationLayer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            var builder = WebApplication.CreateBuilder(args);

            static string FirstConfigured(params string?[] values)
            {
                return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
            }

            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AddPageRoute("/Home/Index", "");
            });
            var authenticationBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
            });
            authenticationBuilder.AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.Name = "CourseAssistant.Auth";
            });
            authenticationBuilder.AddCookie("External", options =>
            {
                options.Cookie.Name = "CourseAssistant.External";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            });

            var googleClientId = FirstConfigured(
                builder.Configuration["Authentication:Google:ClientId"],
                builder.Configuration["GOOGLE_CLIENT_ID"],
                Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID"));
            var googleClientSecret = FirstConfigured(
                builder.Configuration["Authentication:Google:ClientSecret"],
                builder.Configuration["GOOGLE_CLIENT_SECRET"],
                Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET"));
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                authenticationBuilder.AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    options.CallbackPath = "/signin-google";
                    options.SignInScheme = "External";
                    options.SaveTokens = false;
                });
            }
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationPolicies.ChatAccess, policy =>
                    policy.RequireRole(AppRoles.Student, AppRoles.Lecturer, AppRoles.Admin));
                options.AddPolicy(AuthorizationPolicies.DocumentRead, policy =>
                    policy.RequireRole(AppRoles.Lecturer, AppRoles.Admin));
                options.AddPolicy(AuthorizationPolicies.DocumentManagement, policy =>
                    policy.RequireRole(AppRoles.Lecturer, AppRoles.Admin));
                options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
                    policy.RequireRole(AppRoles.Admin));
            });
            var geminiSection = builder.Configuration.GetSection("Gemini");
            var geminiApiKey = FirstConfigured(
                geminiSection["ApiKey"],
                builder.Configuration["GEMINI_API_KEY"],
                Environment.GetEnvironmentVariable("GEMINI_API_KEY"));
            var geminiEnabled = !bool.TryParse(geminiSection["Enabled"], out var parsedGeminiEnabled) || parsedGeminiEnabled;
            var geminiChatModel = geminiSection["Model"] ?? "gemini-3.5-flash";
            var geminiEmbeddingModel = builder.Configuration["Embedding:Model"]
                ?? geminiSection["EmbeddingModel"]
                ?? "gemini-embedding-001";
            var geminiEmbeddingDimensions = int.TryParse(
                    builder.Configuration["Embedding:OutputDimensionality"] ?? geminiSection["EmbeddingOutputDimensionality"],
                    out var parsedEmbeddingDimensions)
                ? parsedEmbeddingDimensions
                : 768;
            var geminiTimeoutSeconds = int.TryParse(geminiSection["TimeoutSeconds"], out var parsedGeminiTimeout)
                ? parsedGeminiTimeout
                : 120;
            var huggingFaceSection = builder.Configuration.GetSection("HuggingFace");
            var huggingFaceToken = FirstConfigured(
                Environment.GetEnvironmentVariable("HF_TOKEN"),
                builder.Configuration["HF_TOKEN"],
                huggingFaceSection["Token"]);
            var huggingFaceEnabled = !bool.TryParse(huggingFaceSection["Enabled"], out var parsedHuggingFaceEnabled)
                                     || parsedHuggingFaceEnabled;
            var huggingFaceTimeoutSeconds = int.TryParse(huggingFaceSection["TimeoutSeconds"], out var parsedHuggingFaceTimeout)
                ? parsedHuggingFaceTimeout
                : 60;
            var huggingFaceOptions = new ServicesLayer.HuggingFaceOptions(
                huggingFaceEnabled,
                huggingFaceToken,
                huggingFaceSection["ChatModel"] ?? "Qwen/Qwen2.5-7B-Instruct:fastest",
                huggingFaceSection["EmbeddingModel"] ?? "Qwen/Qwen3-Embedding-0.6B",
                huggingFaceTimeoutSeconds,
                huggingFaceSection["ChatBaseUrl"] ?? "https://router.huggingface.co/v1/chat/completions",
                huggingFaceSection["EmbeddingBaseUrl"] ?? "https://router.huggingface.co/hf-inference/models");
            var useHuggingFace = huggingFaceOptions.Enabled && !string.IsNullOrWhiteSpace(huggingFaceOptions.Token);
            var semanticChunkingEnabled = bool.TryParse(
                    builder.Configuration["SemanticChunking:Enabled"] ?? geminiSection["EnableSemanticChunking"],
                    out var parsedSemanticChunkingEnabled)
                && parsedSemanticChunkingEnabled;
            var semanticChunkingModel = builder.Configuration["SemanticChunking:Model"]
                ?? geminiSection["ChunkingModel"]
                ?? geminiChatModel;
            var semanticChunkingMaxPromptCharacters = int.TryParse(
                    builder.Configuration["SemanticChunking:MaxPromptCharacters"] ?? geminiSection["ChunkingMaxPromptCharacters"],
                    out var parsedMaxPromptCharacters)
                ? parsedMaxPromptCharacters
                : 16000;
            var semanticChunkingMaxParagraphs = int.TryParse(
                    builder.Configuration["SemanticChunking:MaxParagraphs"] ?? geminiSection["ChunkingMaxParagraphs"],
                    out var parsedMaxParagraphs)
                ? parsedMaxParagraphs
                : 180;
            var fineTunedChatSection = builder.Configuration.GetSection("FineTunedChat");
            var fineTunedChatEnabled = !bool.TryParse(fineTunedChatSection["Enabled"], out var parsedFineTunedChatEnabled)
                                       || parsedFineTunedChatEnabled;
            var fineTunedChatMinimumConfidence = double.TryParse(
                    fineTunedChatSection["MinimumConfidence"],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsedFineTunedMinimumConfidence)
                ? parsedFineTunedMinimumConfidence
                : 0.62;
            var configuredFineTunedExamplesPath = fineTunedChatSection["ExamplesPath"];
            var fineTunedChatExamplesPath = string.IsNullOrWhiteSpace(configuredFineTunedExamplesPath)
                ? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "fine-tuned-chat-examples.json")
                : Path.IsPathRooted(configuredFineTunedExamplesPath)
                    ? configuredFineTunedExamplesPath
                    : Path.Combine(builder.Environment.ContentRootPath, configuredFineTunedExamplesPath);
            builder.Services.AddSingleton(new ServicesLayer.GeminiApiOptions(
                geminiApiKey,
                geminiChatModel,
                geminiEmbeddingModel,
                geminiEmbeddingDimensions,
                geminiEnabled));
            builder.Services.AddSingleton(huggingFaceOptions);
            builder.Services.AddSingleton(new ServicesLayer.FineTunedChatOptions(
                fineTunedChatEnabled,
                fineTunedChatSection["Provider"] ?? "local://supervised-qa",
                fineTunedChatSection["Endpoint"] ?? "local://supervised-qa",
                fineTunedChatMinimumConfidence,
                fineTunedChatExamplesPath));
            builder.Services.AddSingleton<DataAccessLayer.IKnowledgeRepository>(_ =>
            {
                var repository = new DataAccessLayer.Repositories.SqlKnowledgeRepository(
                    builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty);
                try
                {
                    repository.ImportFromJsonIfEmptyAsync(
                        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "rag-store.json")).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Knowledge repository import skipped: {ex.Message}");
                }

                return repository;
            });
            builder.Services.AddSingleton<PresentationLayer.Services.IUserAccountStore>(_ =>
            {
                var seedAdminSection = builder.Configuration.GetSection("SeedAdmin");
                var seedAdminEnabled = !bool.TryParse(seedAdminSection["Enabled"], out var parsedSeedAdminEnabled)
                                       || parsedSeedAdminEnabled;

                return new PresentationLayer.Services.UserAccountStore(
                    Path.Combine(builder.Environment.ContentRootPath, "App_Data", "users.json"),
                    new PresentationLayer.Services.SeedAdminOptions(
                        seedAdminEnabled,
                        seedAdminSection["FullName"] ?? "System Admin",
                        seedAdminSection["Email"] ?? "admin@eduvietrag.local",
                        seedAdminSection["Password"] ?? "Admin@12345"));
            });
            builder.Services.AddSingleton<ServicesLayer.IEmbeddingService>(_ =>
            {
                var embedding = builder.Configuration.GetSection("Embedding");
                var timeoutSeconds = int.TryParse(embedding["TimeoutSeconds"], out var parsedTimeout)
                    ? parsedTimeout
                    : 120;

                if (useHuggingFace)
                {
                    return new ServicesLayer.HuggingFaceEmbeddingService(
                        new HttpClient
                        {
                            Timeout = TimeSpan.FromSeconds(Math.Max(5, huggingFaceOptions.TimeoutSeconds))
                        },
                        huggingFaceOptions);
                }

                var geminiEmbeddingService = new ServicesLayer.GeminiEmbeddingService(
                    new HttpClient
                    {
                        BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
                        Timeout = TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds))
                    },
                    new ServicesLayer.GeminiApiOptions(
                        geminiApiKey,
                        geminiChatModel,
                        geminiEmbeddingModel,
                        geminiEmbeddingDimensions,
                        geminiEnabled));

                return new ServicesLayer.FallbackEmbeddingService(
                    geminiEmbeddingService,
                    new ServicesLayer.HashingEmbeddingService());
            });
            builder.Services.AddSingleton<ServicesLayer.ILocalChatCompletionService>(_ =>
            {
                if (useHuggingFace)
                {
                    return new ServicesLayer.HuggingFaceChatCompletionService(
                        new HttpClient
                        {
                            Timeout = TimeSpan.FromSeconds(Math.Max(5, huggingFaceOptions.TimeoutSeconds))
                        },
                        huggingFaceOptions);
                }

                return new ServicesLayer.GeminiChatCompletionService(
                    new HttpClient
                    {
                        BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
                        Timeout = TimeSpan.FromSeconds(Math.Max(5, geminiTimeoutSeconds))
                    },
                    geminiChatModel,
                    geminiApiKey,
                    geminiEnabled);
            });
            builder.Services.AddSingleton<ServicesLayer.IFineTunedChatService>(serviceProvider =>
                new ServicesLayer.FineTunedChatService(
                    new HttpClient
                    {
                        Timeout = TimeSpan.FromSeconds(Math.Max(5, geminiTimeoutSeconds))
                    },
                    serviceProvider.GetRequiredService<ServicesLayer.FineTunedChatOptions>()));
            builder.Services.AddSingleton<ServicesLayer.IDocumentTextExtractor, ServicesLayer.DocumentTextExtractor>();
            builder.Services.AddSingleton<ServicesLayer.ITextChunker>(_ =>
            {
                var fallbackChunker = new ServicesLayer.ParagraphAwareTextChunker();
                return new ServicesLayer.GeminiSemanticTextChunker(
                    new HttpClient
                    {
                        BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
                        Timeout = TimeSpan.FromSeconds(Math.Max(5, geminiTimeoutSeconds))
                    },
                    new ServicesLayer.GeminiSemanticChunkingOptions(
                        geminiApiKey,
                        semanticChunkingModel,
                        semanticChunkingEnabled && geminiEnabled,
                        semanticChunkingMaxPromptCharacters,
                        semanticChunkingMaxParagraphs),
                    fallbackChunker);
            });
            builder.Services.AddSingleton<ServicesLayer.IDocumentIndexJobQueue, ServicesLayer.DocumentIndexJobQueue>();
            builder.Services.AddSingleton<ServicesLayer.IWebPageTextExtractor>(_ =>
                new ServicesLayer.WebPageTextExtractor(new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(35)
                }));
            builder.Services.AddScoped<ServicesLayer.IDocumentIndexingService, ServicesLayer.DocumentIndexingService>();
            builder.Services.AddScoped<ServicesLayer.IRagChatService, ServicesLayer.RagChatService>();
            builder.Services.AddHostedService<PresentationLayer.Services.DocumentIndexWorker>();

            var app = builder.Build();
            _ = app.Services.GetRequiredService<DataAccessLayer.IKnowledgeRepository>();
            _ = app.Services.GetRequiredService<PresentationLayer.Services.IUserAccountStore>()
                .HasAnyUsersAsync()
                .GetAwaiter()
                .GetResult();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
                .WithStaticAssets();

            app.Run();
        }
    }
}
