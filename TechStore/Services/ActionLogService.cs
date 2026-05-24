using Microsoft.EntityFrameworkCore;
using TechStore.Data;
using TechStore.Entities;

namespace TechStore.Services
{
    public class ActionLogService
    {
        private readonly CatalogDbContext _context;
        public ActionLogService(CatalogDbContext context)
        {
            _context = context;
        }
        // Запись действия администратора в лог
        public virtual async Task LogActionAsync(string adminEmail, string action, string entityName, string entityId, string details)
        {
            var log = new ActionLog
            {
                AdminEmail = adminEmail,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
            _context.ActionLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        // Просмотр логов (для бизнес-овнера)
        public virtual async Task<List<ActionLog>> GetLogsAsync()
        {
            return await _context.ActionLogs
                .OrderByDescending(log => log.Timestamp)
                .Take(100)
                .ToListAsync();
        }
    }
}
