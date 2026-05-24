using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using CarsHub.Auth.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using TechStore.Entities;
using CarsHub.Auth.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Подключение AWS Secrets ---
var region = RegionEndpoint.EUCentral1;
var client = new AmazonSecretsManagerClient(region);
var request = new GetSecretValueRequest { SecretId = "CourseProject/Dev" };
try
{
    var response = await client.GetSecretValueAsync(request);
    if (!string.IsNullOrEmpty(response.SecretString))
    {
        var secretStream = new MemoryStream(Encoding.UTF8.GetBytes(response.SecretString));
        builder.Configuration.AddJsonStream(secretStream);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка при получении секретов из AWS: {ex.Message}");
}

// --- 2. Подключение к БД (Только контекст авторизации) ---
// Использование одной и той жеБД, что и каталог, EF Core сам разделит таблицы
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- 3. Identity + Roles ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

// --- 4. JWT-Tokens (Настройка генерации) ---
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

// --- 5. Контроллеры и Swagger ---
builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            new string[] {}
        }
    });
});

// --- 6. CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Подключение настроек AWS из appsettings.json
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
// Регистрация клиента Amazon S3
builder.Services.AddAWSService<Amazon.S3.IAmazonS3>();
// Регистрация сервиса
builder.Services.AddScoped<IS3Service, S3Service>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();