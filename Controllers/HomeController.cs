using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace GoogleDriveApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly GoogleDriveService _googleDriveService;
        private readonly EmailService _emailService;

        public HomeController(GoogleDriveService googleDriveService, EmailService emailService)
        {
            _googleDriveService = googleDriveService;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            try
            {
                var fileId = "***************************"; // Payla��lacak dosyan�n ID'si
                var emailAddress = "ibrahim.ozturk@demircode.com"; // Al�c�n�n e-posta adresi

                // Dosyay� payla�
                await _googleDriveService.ShareFileAsync(fileId, emailAddress);
                Console.WriteLine($"File with ID {fileId} shared with {emailAddress}");

                // Payla��m linkini al ve e-posta g�nder
                await _emailService.SendEmailWithShareLinkAsync(fileId, emailAddress, "Google Drive Dosya Payla��m�", "A�a��daki dosya sizinle payla��lm��t�r:");
                Console.WriteLine($"E-posta g�nderildi: {emailAddress}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }

            return View();
        }
    }
}
