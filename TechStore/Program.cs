using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using TechStore.Data;
using TechStore.Entities;
using TechStore.Services;

var builder = WebApplication.CreateBuilder(args);

// 1) Подкл. к БД
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Identity + Roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Регистрация своих сервисов
builder.Services.AddScoped<TechStore.Services.ActionLogService>();

// 3) JWT-Tokens
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddControllers()
    .AddJsonOptions(options => {options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TechStore API", Version = "v1" });

    // Опред. схемы безопасности (JWT)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

// ипс. bearer для всех запросов
c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
            }
        });
});

// 4) CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // Разрешает запросы с любых доменов (React localhost, Somee и т.д.)
              .AllowAnyHeader()  // Разрешает любые заголовки (важно для JWT токена)
              .AllowAnyMethod(); // Разрешает любые методы (GET, POST, PUT, DELETE)
    });
});

// 5) AWS S3
// Подключаем настройки AWS из appsettings.json
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());

// Регистрируем клиент Amazon S3
builder.Services.AddAWSService<Amazon.S3.IAmazonS3>();

// Регистрируем кастомный сервис
builder.Services.AddScoped<IS3Service, S3Service>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// middleware (обработчик ошибок)
app.UseMiddleware<TechStore.Middleware.ExceptionMiddleware>();

app.UseDefaultFiles(); // Ищет index.html по умолчанию
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Cross-Origin-Resource-Policy", "cross-origin");
    await next();
});
app.UseStaticFiles();  // Разрешает доступ к папке wwwroot

app.MapControllers();

// Seeder activate
//await DbSeeder.SeedCarsAsync(app);
try
{
    Console.WriteLine("Попытка запуска DbSeeder...");
    await DbSeeder.SeedCarsAsync(app);
    Console.WriteLine("DbSeeder отработал успешно.");
}
catch (Exception ex)
{
    Console.WriteLine("=================================");
    Console.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА ПРИ ЗАПУСКЕ БАЗЫ: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"ДЕТАЛИ: {ex.InnerException.Message}");
    }
    Console.WriteLine("=================================");
}

app.Run();
