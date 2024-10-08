using GoogleDriveApp.Interfaces;

namespace GoogleDriveApp.Factories
{
    public class GoogleDriveServiceFactory : IGoogleDriveServiceFactory
    {
        public GoogleDriveService Create()
        {
            // GoogleDriveService'i token olmadan oluşturuyoruz.
            return new GoogleDriveService();
        }
    }
}
