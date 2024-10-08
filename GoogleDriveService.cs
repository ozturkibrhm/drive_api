using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using System;
using System.IO;
using System.Threading.Tasks;
using Google;
using System.Net;

public class GoogleDriveService
{
    private static readonly string[] Scopes = { Google.Apis.Drive.v3.DriveService.Scope.Drive };
    private readonly DriveService _driveService;

    public GoogleDriveService()
    {
        var credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "credentials.json");

        GoogleCredential credential;
        using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(DriveService.Scope.Drive);
        }

        _driveService = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "GoogleDriveFolderCopy",
        });
    }

    public async Task<string> CreateFolderAsync(string name, string parentFolderId = null)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = name,
            MimeType = "application/vnd.google-apps.folder",
            Parents = parentFolderId != null ? new[] { parentFolderId } : null
        };

        var request = _driveService.Files.Create(fileMetadata);
        request.Fields = "id";

        try
        {
            var file = await request.ExecuteAsync();
            return file.Id;
        }
        catch (GoogleApiException ex)
        {
            Console.WriteLine($"Error creating folder: {ex.Message}");
            throw;
        }
    }

    public async Task CopyFolderAsync(string sourceFolderId, string destinationFolderId)
    {
        var request = _driveService.Files.List();
        request.Q = $"'{sourceFolderId}' in parents";
        request.Fields = "files(id, name, mimeType)";

        try
        {
            var files = await request.ExecuteAsync();

            foreach (var file in files.Files)
            {
                if (file.MimeType == "application/vnd.google-apps.folder")
                {
                    string newFolderId = await CreateFolderAsync(file.Name, destinationFolderId);
                    await CopyFolderAsync(file.Id, newFolderId);
                }
                else
                {
                    var copyRequest = _driveService.Files.Copy(new Google.Apis.Drive.v3.Data.File(), file.Id);
                    copyRequest.Fields = "id, parents";
                    var copiedFile = await copyRequest.ExecuteAsync();

                    var updateRequest = _driveService.Files.Update(new Google.Apis.Drive.v3.Data.File(), copiedFile.Id);
                    updateRequest.AddParents = destinationFolderId;
                    updateRequest.RemoveParents = sourceFolderId;
                    updateRequest.Fields = "id, parents";
                    await updateRequest.ExecuteAsync();
                }
            }
        }
        catch (GoogleApiException ex)
        {
            Console.WriteLine($"Error copying folder: {ex.Message}");
            throw;
        }
    }

    public async Task ShareFileAsync(string fileId, string emailAddress)
    {
        try
        {
            // Paylaşım izinlerini oluştur
            var permission = new Google.Apis.Drive.v3.Data.Permission
            {
                Type = "user",
                Role = "reader", // Okuyucu rolü, 'writer' veya 'commenter' gibi diğer roller de kullanılabilir
                EmailAddress = emailAddress
            };

            // İzni dosyaya ekle
            var request = _driveService.Permissions.Create(permission, fileId);
            await request.ExecuteAsync();

            Console.WriteLine("File shared successfully.");
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.BadRequest)
        {
            Console.WriteLine($"Error sharing file: {ex.Message}");
            foreach (var error in ex.Error.Errors)
            {
                Console.WriteLine($"Error: {error.Message}");
            }
        }
    }

    public async Task<string> GetFileInfoAsync(string fileId)
    {
        var request = _driveService.Files.Get(fileId);
        request.Fields = "name, webViewLink";

        try
        {
            var file = await request.ExecuteAsync();
            return file.Name;
        }
        catch (GoogleApiException ex)
        {
            Console.WriteLine($"Error retrieving file info: {ex.Message}");
            foreach (var error in ex.Error.Errors)
            {
                Console.WriteLine($"Error: {error.Message}");
            }
            throw;
        }
    }
}
