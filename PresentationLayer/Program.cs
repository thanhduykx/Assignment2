using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using PresentationLayer.Hubs;
using PresentationLayer.Security;

namespace PresentationLayer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            var contentRootPath = Directory.GetCurrentDirectory();
            var bootstrapConfiguration = new ConfigurationBuilder()
                .SetBasePath(contentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
            var configuredEnvironment = bootstrapConfiguration["Hosting:Environment"];

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = contentRootPath,
                EnvironmentName = string.IsNullOrWhiteSpace(configuredEnvironment)
                    ? Environments.Production
                    : configuredEnvironment.Trim()
            });

            builder.Configuration.Sources.Clear();
            builder.Configuration
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AddPageRoute("/Home/Index", "");
            });
            builder.Services
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(
                    builder.Environment.ContentRootPath,
                    "App_Data",
                    "DataProtection-Keys")));
            builder.Services.AddSignalR();

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
                options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
                {
                    OnValidatePrincipal = async context =>
                    {
                        var userIdValue = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!Guid.TryParse(userIdValue, out var userId))
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(
                                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                            return;
                        }

                        var users = context.HttpContext.RequestServices.GetRequiredService<PresentationLayer.Services.IUserAccountStore>();
                        var user = await users.FindByIdAsync(userId, context.HttpContext.RequestAborted);
                        if (user is null)
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(
                                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                            return;
                        }

                        var normalizedRole = AppRoles.Normalize(user.Role);
                        if (context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == user.Id.ToString()
                            && context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value == user.FullName
                            && context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value == normalizedRole)
                        {
                            return;
                        }

                        var claims = new[]
                        {
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.FullName),
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, normalizedRole)
                        };
                        var identity = new System.Security.Claims.ClaimsIdentity(
                            claims,
                            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                        context.ReplacePrincipal(new System.Security.Claims.ClaimsPrincipal(identity));
                        context.ShouldRenew = true;
                    }
                };
            });
            authenticationBuilder.AddCookie("External", options =>
            {
                options.Cookie.Name = "CourseAssistant.External";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            });

            var googleClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
            var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
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
            var geminiApiKey = geminiSection["ApiKey"] ?? string.Empty;
            var geminiEnabled = !bool.TryParse(geminiSection["Enabled"], out var parsedGeminiEnabled)
                || parsedGeminiEnabled;
            var geminiTimeoutSeconds = int.TryParse(geminiSection["TimeoutSeconds"], out var parsedGeminiTimeout)
                ? parsedGeminiTimeout
                : 60;
            var geminiEmbeddingDimensions = int.TryParse(geminiSection["EmbeddingDimensions"], out var parsedGeminiEmbeddingDimensions)
                ? parsedGeminiEmbeddingDimensions
                : int.TryParse(builder.Configuration["Embedding:OutputDimensionality"], out var parsedEmbeddingDimensions)
                    ? parsedEmbeddingDimensions
                    : 768;
            var geminiOptions = new ServicesLayer.GeminiOptions(
                geminiEnabled,
                geminiApiKey,
                geminiSection["ChatModel"] ?? "gemini-3.5-flash",
                geminiSection["EmbeddingModel"] ?? "gemini-embedding-2",
                geminiEmbeddingDimensions,
                geminiTimeoutSeconds,
                geminiSection["ChatBaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
                geminiSection["EmbeddingBaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta");

            var smtpSection = builder.Configuration.GetSection("Smtp");
            var smtpOptions = new PresentationLayer.Services.SmtpOptions(
                smtpSection["Host"] ?? string.Empty,
                int.TryParse(smtpSection["Port"], out var smtpPort) ? smtpPort : 587,
                !bool.TryParse(smtpSection["EnableSsl"], out var smtpEnableSsl) || smtpEnableSsl,
                smtpSection["FromEmail"] ?? string.Empty,
                smtpSection["FromName"] ?? "CPMS",
                smtpSection["UserName"] ?? string.Empty,
                smtpSection["Password"] ?? string.Empty);

            builder.Services.AddSingleton(geminiOptions);
            builder.Services.AddSingleton(smtpOptions);

            builder.Services.AddSingleton<DataAccessLayer.IKnowledgeRepository>(_ =>
                new DataAccessLayer.Repositories.SqlKnowledgeRepository(
                    builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty));
            builder.Services.AddSingleton<PresentationLayer.Services.IUserAccountStore>(_ =>
            {
                var seedAdminSection = builder.Configuration.GetSection("SeedAdmin");
                var seedAdminEnabled = !bool.TryParse(seedAdminSection["Enabled"], out var parsedSeedAdminEnabled)
                    || parsedSeedAdminEnabled;

                return new PresentationLayer.Services.UserAccountStore(
                    builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
                    new PresentationLayer.Services.SeedAdminOptions(
                        seedAdminEnabled,
                        seedAdminSection["FullName"] ?? "System Admin",
                        seedAdminSection["Email"] ?? "admin@eduvietrag.local",
                        seedAdminSection["Password"] ?? "Admin@12345"));
            });
            builder.Services.AddSingleton<ServicesLayer.IEmbeddingService>(_ =>
            {
                var embeddingProvider = builder.Configuration["Embedding:Provider"] ?? "Hashing";
                if (embeddingProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
                {
                    return new ServicesLayer.GeminiEmbeddingService(
                        new HttpClient
                        {
                            Timeout = TimeSpan.FromSeconds(Math.Max(5, geminiOptions.TimeoutSeconds))
                        },
                        geminiOptions);
                }

                return new ServicesLayer.HashingEmbeddingService();
            });
            builder.Services.AddSingleton<ServicesLayer.ILocalChatCompletionService>(_ =>
            {
                var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(Math.Max(5, geminiOptions.TimeoutSeconds))
                };

                return new ServicesLayer.GeminiChatCompletionService(httpClient, geminiOptions);
            });
            builder.Services.AddSingleton<ServicesLayer.IDocumentTextExtractor, ServicesLayer.DocumentTextExtractor>();
            builder.Services.AddSingleton<ServicesLayer.ITextChunker, ServicesLayer.FlmSyllabusAwareTextChunker>();
            builder.Services.AddSingleton<ServicesLayer.IChunkRetrievalEnrichmentService, ServicesLayer.AiChunkRetrievalEnrichmentService>();
            builder.Services.AddSingleton<ServicesLayer.IDocumentIndexJobQueue, ServicesLayer.DocumentIndexJobQueue>();
            builder.Services.AddSingleton<PresentationLayer.Services.IAccountEmailSender, PresentationLayer.Services.SmtpAccountEmailSender>();
            builder.Services.AddSingleton<PresentationLayer.Services.IDocumentStatusNotifier, PresentationLayer.Services.SignalRDocumentStatusNotifier>();
            builder.Services.AddSingleton<PresentationLayer.Services.IOnlineUserPresenceTracker, PresentationLayer.Services.InMemoryOnlineUserPresenceTracker>();
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

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapHub<DocumentStatusHub>("/hubs/documents");
            app.MapRazorPages()
                .WithStaticAssets();

            app.Run();
        }
    }
}
