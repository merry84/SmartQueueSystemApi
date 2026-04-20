using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SmartQueue.Api.Data;
using SmartQueue.Api.DTOs;
using SmartQueue.Api.Enums;
using SmartQueue.Api.Models;
using SmartQueue.Api.Services;

namespace SmartQueue.Api.Tests.Services
{
    [TestFixture]
    public class TicketServiceTests
    {
        private SmartQueueDbContext dbContext = null!;
        private TicketService ticketService = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<SmartQueueDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            dbContext = new SmartQueueDbContext(options);
            ticketService = new TicketService(dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            dbContext.Dispose();
        }

        [Test]
        public async Task JoinQueueAsync_ShouldCreateTicketWithNumberOne_WhenFirstTicket()
        {
            var queue = new Queue
            {
                Name = "Queue 1",
                Description = "Test Queue",
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
                AverageServiceTimeMinutes = 5
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            var model = new JoinQueueRequestDto
            {
                CustomerName = "Maria",
                Priority = "Normal"
            };

            var result = await ticketService.JoinQueueAsync(queue.Id, model);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.CustomerName, Is.EqualTo("Maria"));
            Assert.That(result.Number, Is.EqualTo(1));
            Assert.That(result.Status, Is.EqualTo("Waiting"));
            Assert.That(result.Priority, Is.EqualTo("Normal"));
        }

        [Test]
        public async Task JoinQueueAsync_ShouldAssignNextTicketNumber()
        {
            var queue = new Queue
            {
                Name = "Queue 1",
                Description = "Test Queue",
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
                AverageServiceTimeMinutes = 5
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            await dbContext.QueueTickets.AddAsync(new QueueTicket
            {
                CustomerName = "First",
                Number = 1,
                Status = TicketStatus.Waiting,
                Priority = QueuePriority.Normal,
                QueueId = queue.Id,
                JoinedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            var model = new JoinQueueRequestDto
            {
                CustomerName = "Second",
                Priority = "VIP"
            };

            var result = await ticketService.JoinQueueAsync(queue.Id, model);

            Assert.That(result.Number, Is.EqualTo(2));
            Assert.That(result.Priority, Is.EqualTo("VIP"));
        }

        [Test]
        public async Task CallNextAsync_ShouldReturnVipTicketFirst()
        {
            var queue = new Queue
            {
                Name = "Queue 1",
                Description = "Test Queue",
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
                AverageServiceTimeMinutes = 5
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            await dbContext.QueueTickets.AddRangeAsync(
                new QueueTicket
                {
                    CustomerName = "Maria",
                    Number = 1,
                    Status = TicketStatus.Waiting,
                    Priority = QueuePriority.Normal,
                    QueueId = queue.Id,
                    JoinedAt = DateTime.UtcNow
                },
                new QueueTicket
                {
                    CustomerName = "Ivan",
                    Number = 2,
                    Status = TicketStatus.Waiting,
                    Priority = QueuePriority.VIP,
                    QueueId = queue.Id,
                    JoinedAt = DateTime.UtcNow
                });

            await dbContext.SaveChangesAsync();

            var result = await ticketService.CallNextAsync(queue.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.CustomerName, Is.EqualTo("Ivan"));
            Assert.That(result.Status, Is.EqualTo("Called"));
            Assert.That(result.Priority, Is.EqualTo("VIP"));
        }

        [Test]
        public async Task CallNextAsync_ShouldReturnNull_WhenNoWaitingTickets()
        {
            var queue = new Queue
            {
                Name = "Queue 1",
                Description = "Test Queue",
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
                AverageServiceTimeMinutes = 5
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            var result = await ticketService.CallNextAsync(queue.Id);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetTicketsAsync_ShouldReturnTicketsOrderedByPriorityThenNumber()
        {
            var queue = new Queue
            {
                Name = "Queue 1",
                Description = "Test Queue",
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
                AverageServiceTimeMinutes = 5
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            await dbContext.QueueTickets.AddRangeAsync(
                new QueueTicket
                {
                    CustomerName = "Third",
                    Number = 3,
                    Status = TicketStatus.Waiting,
                    Priority = QueuePriority.Normal,
                    QueueId = queue.Id,
                    JoinedAt = DateTime.UtcNow
                },
                new QueueTicket
                {
                    CustomerName = "First",
                    Number = 1,
                    Status = TicketStatus.Waiting,
                    Priority = QueuePriority.VIP,
                    QueueId = queue.Id,
                    JoinedAt = DateTime.UtcNow
                },
                new QueueTicket
                {
                    CustomerName = "Second",
                    Number = 2,
                    Status = TicketStatus.Waiting,
                    Priority = QueuePriority.Normal,
                    QueueId = queue.Id,
                    JoinedAt = DateTime.UtcNow
                });

            await dbContext.SaveChangesAsync();

            var result = (await ticketService.GetTicketsAsync(queue.Id)).ToList();

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].Number, Is.EqualTo(1));
            Assert.That(result[1].Number, Is.EqualTo(2));
            Assert.That(result[2].Number, Is.EqualTo(3));
        }
    }
}