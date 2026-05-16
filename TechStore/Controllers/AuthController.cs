using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TechStore.DTOs;
using TechStore.Entities; 
using TechStore.Services;

namespace TechStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IS3Service _s3Service;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IS3Service s3Service)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _s3Service = s3Service;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) Проверка Роли
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }

            // 2) Логика загрузки аватарки в AWS S3
            string? avatarUrl = null;
            if (model.Avatar != null && model.Avatar.Length > 0)
            {
                // Загрузка файла в папку "avatars" внутри бакета
                avatarUrl = await _s3Service.UploadFileAsync(model.Avatar, "avatars");
            }

            // 3) Создание пользователя (используется ApplicationUser)
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                AvatarUrl = avatarUrl // Запись полученной от S3 ссылки в БД
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // 4) Назначение Роли
            await _userManager.AddToRoleAsync(user, model.Role);

            return Ok(new { Message = "Пользователь успешно зарегистрирован" });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model) // Логин остается по JSON [FromBody]
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized("Неправильный логин или пароль");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid) return Unauthorized("Неправильный логин или пароль");

            var roles = await _userManager.GetRolesAsync(user);

            var token = GenerateJwtToken(user, roles);
            return Ok(new { Token = token });
        }

        // МЕТОД ГЕНЕРАЦИИ ТОКЕНА
        private string GenerateJwtToken(ApplicationUser user, IList<string> roles) // Принимаем ApplicationUser
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                
                // ВАЖНО: Добавление ссылки на аватарку в клеймы токена
                // Если аватарки нет, запись пустой строки
                new Claim("AvatarUrl", user.AvatarUrl ?? string.Empty)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["DurationInMinutes"])),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}