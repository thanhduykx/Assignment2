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
            var smtpSection = builder.Configuration.GetSection("Smtp");
            var smtpOptions = new PresentationLayer.Services.SmtpOptions(
                smtpSection["Host"] ?? string.Empty,
                int.TryParse(smtpSection["Port"], out var smtpPort) ? smtpPort : 587,
                !bool.TryParse(smtpSection["EnableSsl"], out var smtpEnableSsl) || smtpEnableSsl,
                FirstConfigured(smtpSection["FromEmail"], builder.Configuration["SMTP_FROM_EMAIL"], Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL")),
                smtpSection["FromName"] ?? "CPMS",
                FirstConfigured(smtpSection["UserName"], builder.Configuration["SMTP_USERNAME"], Environment.GetEnvironmentVariable("SMTP_USERNAME")),
                FirstConfigured(smtpSection["Password"], builder.Configuration["SMTP_PASSWORD"], Environment.GetEnvironmentVariable("SMTP_PASSWORD")));
            builder.Services.AddSingleton(huggingFaceOptions);
            builder.Services.AddSingleton(smtpOptions);
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
                return new ServicesLayer.HuggingFaceEmbeddingService(
                    new HttpClient
                    {
                        Timeout = TimeSpan.FromSeconds(Math.Max(5, huggingFaceOptions.TimeoutSeconds))
                    },
                    huggingFaceOptions);
            });
            builder.Services.AddSingleton<ServicesLayer.ILocalChatCompletionService>(_ =>
            {
                return new ServicesLayer.HuggingFaceChatCompletionService(
                    new HttpClient
                    {
                        Timeout = TimeSpan.FromSeconds(Math.Max(5, huggingFaceOptions.TimeoutSeconds))
                    },
                    huggingFaceOptions);
            });
            builder.Services.AddSingleton<ServicesLayer.IDocumentTextExtractor, ServicesLayer.DocumentTextExtractor>();
            builder.Services.AddSingleton<ServicesLayer.ITextChunker, ServicesLayer.FlmSyllabusAwareTextChunker>();
            builder.Services.AddSingleton<ServicesLayer.IDocumentIndexJobQueue, ServicesLayer.DocumentIndexJobQueue>();
            builder.Services.AddSingleton<PresentationLayer.Services.IAccountEmailSender, PresentationLayer.Services.SmtpAccountEmailSender>();
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
