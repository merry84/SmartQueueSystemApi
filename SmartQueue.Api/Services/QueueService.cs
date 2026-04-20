using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Data;
using SmartQueue.Api.DTOs;
using SmartQueue.Api.Models;
using SmartQueue.Api.Services.Contracts;

namespace SmartQueue.Api.Services
{
    public class QueueService : IQueueService
    {
        private readonly SmartQueueDbContext dbContext;

        public QueueService(SmartQueueDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<QueueResponseDto>> GetAllAsync()
        {
            return await dbContext.Queues
                .Select(q => new QueueResponseDto
                {
                    Id = q.Id,
                    Name = q.Name,
                    Description = q.Description,
                    IsActive = q.IsActive,
                    AverageServiceTimeMinutes = q.AverageServiceTimeMinutes,
                    CreatedOn = q.CreatedOn
                })
                .ToListAsync();
        }

        public async Task<QueueResponseDto?> GetByIdAsync(int id)
        {
            return await dbContext.Queues
                .Where(q => q.Id == id)
                .Select(q => new QueueResponseDto
                {
                    Id = q.Id,
                    Name = q.Name,
                    Description = q.Description,
                    IsActive = q.IsActive,
                    AverageServiceTimeMinutes = q.AverageServiceTimeMinutes,
                    CreatedOn = q.CreatedOn
                })
                .FirstOrDefaultAsync();
        }

        public async Task<QueueResponseDto> CreateAsync(CreateQueueRequestDto model)
        {
            var queue = new Queue
            {
                Name = model.Name,
                Description = model.Description,
                IsActive = true,
                AverageServiceTimeMinutes = model.AverageServiceTimeMinutes,
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            return new QueueResponseDto
            {
                Id = queue.Id,
                Name = queue.Name,
                Description = queue.Description,
                IsActive = queue.IsActive,
                AverageServiceTimeMinutes = queue.AverageServiceTimeMinutes,
                CreatedOn = queue.CreatedOn
            };
        }
    }
}