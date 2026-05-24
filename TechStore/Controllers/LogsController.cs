using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.Services;
using Microsoft.EntityFrameworkCore;
using TechStore.Data;

namespace TechStore.Controllers
{
    [Route("api/actionlogs")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Только администраторы могут видеть логи
    public class LogsController : ControllerBase
    {
        private readonly CatalogDbContext _context;
        public LogsController(CatalogDbContext context) { _context = context; }


        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _context.ActionLogs
                                     .OrderByDescending(l => l.Timestamp)
                                     .Take(50)
                                     .ToListAsync();
            return Ok(logs);
        }
    }
}
