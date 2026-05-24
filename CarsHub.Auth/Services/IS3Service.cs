using Microsoft.AspNetCore.Http;

namespace CarsHub.Auth.Services
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(IFormFile file, string folderName = "cars");
    }
}
