using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;

namespace TechStore.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3Service(IAmazonS3 s3Client, IConfiguration config)
        {
            _s3Client = s3Client;
            _bucketName = config["S3Settings:BucketName"] ?? throw new ArgumentNullException("BucketName is missing");
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderName = "cars")
        {
            // 1. Генерируем уникальное имя файла, чтобы картинки не перезаписывали друг друга
            var fileName = $"{Guid.NewGuid()}_{file.FileName.Replace(" ", "_")}";
            var fileKey = $"{folderName}/{fileName}"; // Папка внутри бакета

            // 2. Читаем файл в память
            using var newMemoryStream = new MemoryStream();
            await file.CopyToAsync(newMemoryStream);

            // 3. Формируем запрос на загрузку
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = newMemoryStream,
                Key = fileKey,
                BucketName = _bucketName,
                ContentType = file.ContentType,
                CannedACL = S3CannedACL.PublicRead // Делаем файл доступным для чтения всем в интернете
            };

            // 4. Отправляем в AWS S3
            using var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            // 5. Формируем и возвращаем публичную ссылку на загруженный файл
            var region = _s3Client.Config.RegionEndpoint.SystemName;
            return $"https://{_bucketName}.s3.{region}.amazonaws.com/{fileKey}";
        }
    }
}
