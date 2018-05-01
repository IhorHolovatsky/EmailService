using System.IO;

namespace EmailService.App.Repositories
{
    public interface IAwsFileRepository
    {
        Stream DownloadFile(string fileName);

        bool RemoveFile(string fileName);
    }
}