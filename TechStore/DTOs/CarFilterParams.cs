namespace TechStore.DTOs
{
    public class CarFilterParams
    {
        // Пагинация
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Поиск (для строки поиска в хедере)
        public string? SearchTerm { get; set; }

        // Фильтры
        public string? Mark { get; set; }
        public string? Model { get; set; }
        public string? FuelType { get; set; }

        public string? Transmission { get; set; }
        public int? MinYear { get; set; }
        public int? MaxYear { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // Сортировка (например: "price_asc", "price_desc", "year_desc")
        public string? SortBy { get; set; }
    }
}
