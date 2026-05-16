using Microsoft.AspNetCore.Http;

namespace TechStore.Services
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(IFormFile file, string folderName = "cars");
    }
}
