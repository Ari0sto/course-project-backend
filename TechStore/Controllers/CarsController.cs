using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechStore.Data;
using TechStore.DTOs;
using TechStore.Entities;
using TechStore.Services;

namespace TechStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ActionLogService _actionLogService;
        private readonly IS3Service _s3Service;

        public CarsController(ApplicationDbContext context, ActionLogService actionLogService, IS3Service s3Service)
        {
            _context = context;
            _actionLogService = actionLogService;
            _s3Service = s3Service;
        }

        // GET: api/cars
        [HttpGet]
        public async Task<ActionResult> GetCars([FromQuery] CarFilterParams carParams)
        {
            // 1. запрос к БД
            var query = _context.Cars.Where(c => !c.IsDeleted).AsQueryable();

            // 2. Применяем фильтры динамически
            if (!string.IsNullOrWhiteSpace(carParams.SearchTerm))
            {
                var lowerSearch = carParams.SearchTerm.ToLower();
                query = query.Where(c => c.MarkName.ToLower().Contains(lowerSearch) ||
                                         c.ModelName.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrWhiteSpace(carParams.Mark))
                query = query.Where(c => c.MarkName == carParams.Mark);

            if (!string.IsNullOrWhiteSpace(carParams.Model))
                query = query.Where(c => c.ModelName == carParams.Model);

            // Поиск по коробке передач (строгое совпадение)
            if (!string.IsNullOrWhiteSpace(carParams.Transmission))
                query = query.Where(c => c.Transmission == carParams.Transmission);

            // Поиск по топливу
            if (!string.IsNullOrWhiteSpace(carParams.FuelType))
                query = query.Where(c => c.FuelAndVolume.Contains(carParams.FuelType));

            if (carParams.MinPrice.HasValue)
                query = query.Where(c => c.PriceUSD >= carParams.MinPrice.Value);

            if (carParams.MaxPrice.HasValue)
                query = query.Where(c => c.PriceUSD <= carParams.MaxPrice.Value);

            if (carParams.MinYear.HasValue)
                query = query.Where(c => c.Year >= carParams.MinYear.Value);

            if (carParams.MaxYear.HasValue)
                query = query.Where(c => c.Year <= carParams.MaxYear.Value);

            // 3. Сортировка
            query = carParams.SortBy switch
            {
                "price_asc" => query.OrderBy(c => c.PriceUSD),
                "price_desc" => query.OrderByDescending(c => c.PriceUSD),
                "year_desc" => query.OrderByDescending(c => c.Year),
                "year_asc" => query.OrderBy(c => c.Year),
                _ => query.OrderByDescending(c => c.Year) // По умолчанию: сначала новые
            };

            // 4. Пагинация
            var totalItems = await query.CountAsync(); // Узнаем общее количество подходящих машин
            var cars = await query
                .Skip((carParams.PageNumber - 1) * carParams.PageSize)
                .Take(carParams.PageSize)
                .ToListAsync(); // Только здесь запрос улетает в базу данных!

            // 5. Формируем ответ (Фронтенду нужно знать общее количество для отрисовки страниц)
            return Ok(new
            {
                TotalItems = totalItems,
                PageNumber = carParams.PageNumber,
                PageSize = carParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)carParams.PageSize),
                Items = cars
            });
        }

        // GET: api/cars/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Car>> GetCar(int id)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (car == null)
            {
                return NotFound();
            }

            return car;
        }

        // POST: api/cars
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Car>> CreateCar([FromForm] CreateCarDto carDto)
        {
            // 1. Создание новой сущности Car на основе пришедших данных
            var car = new Car
            {
                MarkName = carDto.MarkName,
                ModelName = carDto.ModelName,
                Year = carDto.Year,
                PriceUSD = carDto.PriceUSD,
                PriceUAH = carDto.PriceUAH,
                Mileage = carDto.Mileage,
                City = carDto.City,
                Transmission = carDto.Transmission,
                FuelAndVolume = carDto.FuelAndVolume,
                Drive = carDto.Drive,
                VinVerified = carDto.VinVerified,
                IsDeleted = false
            };

            // 2. ЛОГИКА AWS S3
            if (carDto.ImageFile != null && carDto.ImageFile.Length > 0)
            {
                // Вызов сервиса
                car.PhotoUrl = await _s3Service.UploadFileAsync(carDto.ImageFile, "cars");
            }
            else
            {
                return BadRequest("Будь ласка, завантажте фото автомобіля.");
            }

            // 3. Сохранение в базу
            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

            // 4. Логирование действия
            var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown Admin";
            await _actionLogService.LogActionAsync(adminEmail, "Create", "Car", car.Id.ToString(), $"Додано нове авто: {car.MarkName} {car.ModelName}");

            return CreatedAtAction(nameof(GetCar), new { id = car.Id }, car);
        }

        // PUT: api/cars/5
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCar(int id, Car car)
        {
            if (id != car.Id) return BadRequest();

            _context.Entry(car).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CarExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/cars/5
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCar(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();

            string carInfo = $"{car.MarkName} {car.ModelName} (ID: {car.Id})";

            // Вместо физического удаления — Soft Delete
            car.IsDeleted = true;

            await _context.SaveChangesAsync();

            // ЗАПИСЫВАЕМ ДЕЙСТВИЕ В ЛОГ
            var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown Admin";
            await _actionLogService.LogActionAsync(
                adminEmail,
                "Delete",
                "Car",
                id.ToString(),
                $"Видалено (Soft Delete) авто: {carInfo}"
            );

            return NoContent();
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.Id == id && !e.IsDeleted);
        }
    }
}