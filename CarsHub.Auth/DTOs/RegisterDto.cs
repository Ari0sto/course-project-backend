using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TechStore.DTOs
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "Customer";

        // Новое поле для бинарного файла аватарки из формы
        public IFormFile? Avatar { get; set; }
    }
}
