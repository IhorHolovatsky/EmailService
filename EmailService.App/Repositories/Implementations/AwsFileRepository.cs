using System.Configuration;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Util;

namespace EmailService.App.Repositories.Implementations
{
    public class AwsFileRepository : IAwsFileRepository
    {
        private readonly AmazonS3Config _config;
        private readonly AWSCredentials _credentials;
        private readonly string _bucketName;

        public AwsFileRepository()
        {
            var accessKey = ConfigurationManager.AppSettings.GetAppConfigValue<string>("AmazonS3.AccessKey");
            var secretKey = ConfigurationManager.AppSettings.GetAppConfigValue<string>("AmazonS3.SecretKey");
            var serviceUrl = ConfigurationManager.AppSettings.GetAppConfigValue<string>("AmazonS3.ServiceUrl");
            _bucketName = ConfigurationManager.AppSettings.GetAppConfigValue<string>("AmazonS3.BucketName");

            _credentials = new BasicAWSCredentials(accessKey, secretKey);
            _config = new AmazonS3Config
            {
                ServiceURL = serviceUrl
            };
        }

        public Stream DownloadFile(string fileName)
        {
            using (var client = new AmazonS3Client(_credentials, _config))
            {
                var response = client.GetObject(_bucketName, fileName);
                return response.ResponseStream;
            }
        }


        public bool RemoveFile(string fileName)
        {
            using (var client = new AmazonS3Client(_credentials, _config))
            {
                var response = client.DeleteObject(_bucketName, fileName);
                return response.HttpStatusCode == HttpStatusCode.NoContent;
            }
        }
    }
}