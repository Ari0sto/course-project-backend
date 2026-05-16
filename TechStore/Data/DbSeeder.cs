using System.Text.Json;
using TechStore.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Data
{
    public static class DbSeeder
    {
        public static async Task SeedCarsAsync(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Если машин в базе нет - загружаем их из файла
                if (!context.Cars.Any())
                {
                    Console.WriteLine("База пуста. Загружаем машины из JSON...");

                    // Указываем путь к файлу. 
                    // cars.json в папке Data бэкенда
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "cars.json");

                    if (File.Exists(filePath))
                    {
                        var jsonData = await File.ReadAllTextAsync(filePath);

                        // Эта настройка нужна, чтобы JSON (markName) правильно смапился на C# класс (MarkName)
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        // Десериализация JSON в список объектов Car
                        var cars = JsonSerializer.Deserialize<List<Car>>(jsonData, options);

                        if (cars != null && cars.Any())
                        {
                            foreach (var car in cars)
                            {
                                car.Id = 0;
                            }

                            await context.Cars.AddRangeAsync(cars);
                            await context.SaveChangesAsync();
                            Console.WriteLine($"--- Успешно добавлено {cars.Count} машин из файла ---");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ОШИБКА: Файл с данными не найден по пути: {filePath}");
                    }
                }
            }
        }
    }
}