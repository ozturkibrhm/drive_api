using Google.Apis.Auth.OAuth2;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService
{
    private readonly string _smtpServer = "smtp.gmail.com";
    private readonly int _smtpPort = 587;
    private readonly string _smtpUser = "***********";
    private readonly string _smtpPass = "***********;

    // Google Drive'daki dosyanın adını ve paylaşım linkini al
    private async Task<(string fileName, string shareLink)> GetFileInfoAsync(string fileId)
    {
        try
        {
            var credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "credentials.json");
            var credential = GoogleCredential.FromFile(credentialsPath)
                .CreateScoped(Google.Apis.Drive.v3.DriveService.Scope.Drive);

            var service = new Google.Apis.Drive.v3.DriveService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "GoogleDriveApp",
            });

            var request = service.Files.Get(fileId);
            request.Fields = "name, webViewLink";
            var file = await request.ExecuteAsync();

            if (!string.IsNullOrEmpty(file.WebViewLink))
            {
                return (file.Name, file.WebViewLink);
            }
            else
            {
                Console.WriteLine("Paylaşım linki alınamadı.");
                return (null, null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Google Drive'dan dosya bilgisi alma hatası: {ex.Message}");
            return (null, null);
        }
    }

    // E-posta ile paylaşım linkini gönder
    public async Task SendEmailWithShareLinkAsync(string fileId, string toAddress, string subject, string body)
    {
        var (fileName, shareLink) = await GetFileInfoAsync(fileId);
        if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(shareLink))
        {
            var emailBody = $"Şu dosya sizinle paylaşıldı: \"{fileName}\".\n\nPaylaşım Linki: {shareLink}";
            await SendEmailAsync(toAddress, subject, emailBody);
        }
        else
        {
            Console.WriteLine("Paylaşım linki alınamadığı için e-posta gönderilemedi.");
        }
    }

    // E-posta gönderme
    private async Task SendEmailAsync(string toAddress, string subject, string body)
    {
        try
        {
            using (var smtpClient = new SmtpClient(_smtpServer)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                EnableSsl = true,
            })
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUser),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toAddress);

                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine("E-posta başarıyla gönderildi.");
            }
        }
        catch (SmtpException smtpEx)
        {
            Console.WriteLine($"SMTP hatası: {smtpEx.Message}");
            Console.WriteLine($"Durum Kodu: {smtpEx.StatusCode}");
            Console.WriteLine($"Hata Ayrıntıları: {smtpEx.StackTrace}");
            if (smtpEx.InnerException != null)
            {
                Console.WriteLine($"İç Hata: {smtpEx.InnerException.Message}");
                Console.WriteLine($"İç Hata Ayrıntıları: {smtpEx.InnerException.StackTrace}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Genel hata: {ex.Message}");
            Console.WriteLine($"Hata Ayrıntıları: {ex.StackTrace}");
        }
    }
}
