using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TechStore.DTOs
{
    public class CreateCarDto
    {
        [Required]
        public string MarkName { get; set; } = string.Empty;

        [Required]
        public string ModelName { get; set; } = string.Empty;

        public int Year { get; set; }

        public decimal PriceUSD { get; set; }
        public decimal PriceUAH { get; set; }

        public int Mileage { get; set; }
        public string City { get; set; } = string.Empty;
        public string Transmission { get; set; } = string.Empty;
        public string FuelAndVolume { get; set; } = string.Empty;
        public string Drive { get; set; } = string.Empty;

        public bool VinVerified { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}