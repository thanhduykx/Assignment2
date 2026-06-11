using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using PresentationLayer.Models;

namespace PresentationLayer.Services;

public sealed record SmtpOptions(
    string Host,
    int Port,
    bool EnableSsl,
    string FromEmail,
    string FromName,
    string UserName,
    string Password);

public interface IAccountEmailSender
{
    Task SendWelcomeEmailAsync(
        UserAccount account,
        string temporaryPassword,
        string? loginUrl,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetEmailAsync(
        UserAccount account,
        string resetUrl,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
}

public sealed class SmtpAccountEmailSender : IAccountEmailSender
{
    private const string LogoContentId = "fpt-logo";

    private readonly SmtpOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SmtpAccountEmailSender> _logger;

    public SmtpAccountEmailSender(
        SmtpOptions options,
        IWebHostEnvironment environment,
        ILogger<SmtpAccountEmailSender> logger)
    {
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(
        UserAccount account,
        string temporaryPassword,
        string? loginUrl,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName, Encoding.UTF8),
            Subject = "Chào mừng bạn tới Ứng dụng Chat Bot - Quản Lý Tài Liệu",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };
        message.To.Add(new MailAddress(account.Email, account.FullName, Encoding.UTF8));

        var plainText = BuildPlainText(account, temporaryPassword, loginUrl);
        var html = BuildHtml(account, temporaryPassword, loginUrl);
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainText, Encoding.UTF8, MediaTypeNames.Text.Plain));

        var htmlView = AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html);
        var logoPath = Path.Combine(_environment.WebRootPath, "img", "fptlogo.png");
        if (File.Exists(logoPath))
        {
            var logo = new LinkedResource(logoPath, "image/png")
            {
                ContentId = LogoContentId,
                TransferEncoding = TransferEncoding.Base64
            };
            logo.ContentType.Name = "fptlogo.png";
            htmlView.LinkedResources.Add(logo);
        }
        else
        {
            _logger.LogWarning("Welcome email logo was not found at {LogoPath}", logoPath);
        }

        message.AlternateViews.Add(htmlView);

        await SendAsync(message, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(
        UserAccount account,
        string resetUrl,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName, Encoding.UTF8),
            Subject = "Đặt lại mật khẩu Course Assistant",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };
        message.To.Add(new MailAddress(account.Email, account.FullName, Encoding.UTF8));

        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            BuildPasswordResetPlainText(account, resetUrl, expiresAt),
            Encoding.UTF8,
            MediaTypeNames.Text.Plain));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            BuildPasswordResetHtml(account, resetUrl, expiresAt),
            Encoding.UTF8,
            MediaTypeNames.Text.Html));

        await SendAsync(message, cancellationToken);
    }

    private async Task SendAsync(MailMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.UserName, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.FromEmail)
            || string.IsNullOrWhiteSpace(_options.UserName)
            || string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException("SMTP configuration is incomplete.");
        }
    }

    private static string BuildPlainText(UserAccount account, string temporaryPassword, string? loginUrl)
    {
        var loginLine = string.IsNullOrWhiteSpace(loginUrl)
            ? "Đăng nhập tại địa chỉ ứng dụng được nhà trường cung cấp."
            : $"Đăng nhập: {loginUrl}";

        return $"""
            Chào mừng bạn tới Ứng dụng Chat Bot - Quản Lý Tài Liệu

            Xin chào {account.FullName},

            Tài khoản của bạn đã được tạo thành công.

            Email đăng nhập: {account.Email}
            Vai trò: {account.Role}
            Mật khẩu tạm thời: {temporaryPassword}

            {loginLine}

            Lưu ý bảo mật tài khoản:
            - Không chia sẻ mật khẩu cho bất kỳ ai.
            - Không chuyển tiếp email này cho người khác.
            - Nếu nghi ngờ tài khoản bị lộ, liên hệ admin/bộ phận phụ trách để đổi hoặc khóa tài khoản.
            - Đăng xuất khỏi thiết bị dùng chung sau khi sử dụng.

            Trân trọng,
            CPMS
            """;
    }

    private static string BuildPasswordResetPlainText(UserAccount account, string resetUrl, DateTimeOffset expiresAt)
    {
        var expiryText = expiresAt.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
        return $"""
            Đặt lại mật khẩu Course Assistant

            Xin chào {account.FullName},

            Hệ thống nhận được yêu cầu đặt lại mật khẩu cho tài khoản {account.Email}.

            Link đặt lại mật khẩu:
            {resetUrl}

            Link này hết hạn lúc {expiryText}. Nếu bạn không yêu cầu thao tác này, hãy bỏ qua email.

            Trân trọng,
            CPMS
            """;
    }

    private static string BuildPasswordResetHtml(UserAccount account, string resetUrl, DateTimeOffset expiresAt)
    {
        var fullName = WebUtility.HtmlEncode(account.FullName);
        var email = WebUtility.HtmlEncode(account.Email);
        var resetLink = WebUtility.HtmlEncode(resetUrl);
        var expiryText = WebUtility.HtmlEncode(expiresAt.ToLocalTime().ToString("HH:mm dd/MM/yyyy"));

        return $$"""
            <!doctype html>
            <html lang="vi">
            <body style="margin:0;background:#f4f7fb;font-family:Arial,Helvetica,sans-serif;color:#102033;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f4f7fb;padding:28px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:620px;background:#ffffff;border:1px solid #dbe4ef;border-radius:12px;overflow:hidden;">
                      <tr>
                        <td style="padding:28px;">
                          <h1 style="margin:0 0 14px;color:#0f4c81;font-size:24px;line-height:1.3;">Đặt lại mật khẩu</h1>
                          <p style="font-size:16px;line-height:1.6;margin:0 0 16px;">Xin chào <strong>{{fullName}}</strong>,</p>
                          <p style="font-size:15px;line-height:1.6;margin:0 0 18px;">Hệ thống nhận được yêu cầu đặt lại mật khẩu cho tài khoản <strong>{{email}}</strong>.</p>
                          <p style="margin:22px 0;">
                            <a href="{{resetLink}}" style="display:inline-block;background:#0f4c81;color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:700;">Đặt lại mật khẩu</a>
                          </p>
                          <p style="font-size:14px;color:#64748b;line-height:1.6;margin:0 0 12px;">Link hết hạn lúc <strong>{{expiryText}}</strong>.</p>
                          <p style="font-size:13px;color:#64748b;line-height:1.6;margin:0;">Nếu bạn không yêu cầu thao tác này, hãy bỏ qua email.</p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string BuildHtml(UserAccount account, string temporaryPassword, string? loginUrl)
    {
        var fullName = WebUtility.HtmlEncode(account.FullName);
        var email = WebUtility.HtmlEncode(account.Email);
        var role = WebUtility.HtmlEncode(account.Role);
        var password = WebUtility.HtmlEncode(temporaryPassword);
        var loginLink = string.IsNullOrWhiteSpace(loginUrl)
            ? "<p style=\"margin:0;color:#475569;\">Đăng nhập tại địa chỉ ứng dụng được nhà trường cung cấp.</p>"
            : $"""
              <a href="{WebUtility.HtmlEncode(loginUrl)}" style="display:inline-block;background:#0f4c81;color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:700;">Đăng nhập ứng dụng</a>
              """;

        return $$"""
            <!doctype html>
            <html lang="vi">
            <body style="margin:0;background:#f4f7fb;font-family:Arial,Helvetica,sans-serif;color:#102033;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f4f7fb;padding:28px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:680px;background:#ffffff;border:1px solid #dbe4ef;border-radius:12px;overflow:hidden;">
                      <tr>
                        <td style="padding:24px 28px 12px;text-align:center;">
                          <img src="cid:{{LogoContentId}}" alt="FPT Education" style="max-width:360px;width:80%;height:auto;margin:0 auto 18px;display:block;" />
                          <h1 style="margin:0;color:#0f4c81;font-size:24px;line-height:1.3;">Chào mừng bạn tới Ứng dụng Chat Bot - Quản Lý Tài Liệu</h1>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:10px 28px 28px;">
                          <p style="font-size:16px;line-height:1.6;margin:0 0 16px;">Xin chào <strong>{{fullName}}</strong>,</p>
                          <p style="font-size:15px;line-height:1.6;margin:0 0 18px;">Tài khoản của bạn đã được tạo thành công. Vui lòng sử dụng thông tin bên dưới để đăng nhập hệ thống.</p>

                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;margin:18px 0;background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;overflow:hidden;">
                            <tr>
                              <td style="padding:12px 14px;color:#64748b;width:38%;font-size:14px;">Email đăng nhập</td>
                              <td style="padding:12px 14px;font-weight:700;font-size:14px;">{{email}}</td>
                            </tr>
                            <tr>
                              <td style="padding:12px 14px;color:#64748b;font-size:14px;">Vai trò</td>
                              <td style="padding:12px 14px;font-weight:700;font-size:14px;">{{role}}</td>
                            </tr>
                            <tr>
                              <td style="padding:12px 14px;color:#64748b;font-size:14px;">Mật khẩu tạm thời</td>
                              <td style="padding:12px 14px;font-weight:800;font-size:14px;color:#b42318;">{{password}}</td>
                            </tr>
                          </table>

                          <div style="margin:20px 0;">{{loginLink}}</div>

                          <div style="border-left:4px solid #f97316;background:#fff7ed;padding:14px 16px;border-radius:8px;margin-top:22px;">
                            <h2 style="margin:0 0 8px;font-size:16px;color:#9a3412;">Nhắc nhở bảo mật tài khoản</h2>
                            <ul style="margin:0;padding-left:18px;color:#7c2d12;font-size:14px;line-height:1.6;">
                              <li>Không chia sẻ mật khẩu cho bất kỳ ai.</li>
                              <li>Không chuyển tiếp email này cho người khác.</li>
                              <li>Nếu nghi ngờ tài khoản bị lộ, liên hệ admin/bộ phận phụ trách để đổi hoặc khóa tài khoản.</li>
                              <li>Đăng xuất khỏi thiết bị dùng chung sau khi sử dụng.</li>
                            </ul>
                          </div>

                          <p style="margin:22px 0 0;color:#64748b;font-size:13px;line-height:1.5;">Email này được gửi từ hệ thống CPMS. Vui lòng không trả lời trực tiếp email này.</p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
