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
    public class QueueServiceTests
    {
        private SmartQueueDbContext dbContext = null!;
        private QueueService queueService = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<SmartQueueDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            dbContext = new SmartQueueDbContext(options);
            queueService = new QueueService(dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            dbContext.Dispose();
        }

        [Test]
        public async Task CreateAsync_ShouldCreateQueueSuccessfully()
        {
            var model = new CreateQueueRequestDto
            {
                Name = "Test Queue",
                Description = "Test Description"
            };

            var result = await queueService.CreateAsync(model);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Test Queue"));
            Assert.That(result.Description, Is.EqualTo("Test Description"));
            Assert.That(await dbContext.Queues.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task JoinQueueAsync_ShouldCreateTicketWithNumberOne_WhenFirstTicket()
        {
            var queue = new Queue
            {
                Name = "Queue 1",
                Description = "Test Queue",
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            var model = new JoinQueueRequestDto
            {
                CustomerName = "Maria",
                Priority = "Normal"
            };

            var result = await queueService.JoinQueueAsync(queue.Id, model);

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
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            await dbContext.QueueTickets.AddAsync(new QueueTicket
            {
                CustomerName = "First",
                Number = 1,
                Status = QueueStatus.Waiting,
                Priority = QueuePriority.Normal,
                QueueId = queue.Id,
                CreatedOn = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            var model = new JoinQueueRequestDto
            {
                CustomerName = "Second",
                Priority = "VIP"
            };

            var result = await queueService.JoinQueueAsync(queue.Id, model);

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
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            await dbContext.QueueTickets.AddRangeAsync(
                new QueueTicket
                {
                    CustomerName = "Maria",
                    Number = 1,
                    Status = QueueStatus.Waiting,
                    Priority = QueuePriority.Normal,
                    QueueId = queue.Id,
                    CreatedOn = DateTime.UtcNow
                },
                new QueueTicket
                {
                    CustomerName = "Ivan",
                    Number = 2,
                    Status = QueueStatus.Waiting,
                    Priority = QueuePriority.VIP,
                    QueueId = queue.Id,
                    CreatedOn = DateTime.UtcNow
                });

            await dbContext.SaveChangesAsync();

            var result = await queueService.CallNextAsync(queue.Id);

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
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            var result = await queueService.CallNextAsync(queue.Id);

            Assert.That(result, Is.Null);
        }
        [Test]
        public async Task GetByIdAsync_ShouldReturnQueue_WhenQueueExists()
        {
            var queue = new Queue
            {
                Name = "Existing Queue",
                Description = "Test Description",
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            var result = await queueService.GetByIdAsync(queue.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Existing Queue"));
            Assert.That(result.Description, Is.EqualTo("Test Description"));
        }

        [Test]
        public async Task GetByIdAsync_ShouldReturnNull_WhenQueueDoesNotExist()
        {
            var result = await queueService.GetByIdAsync(999);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetTicketsAsync_ShouldReturnTicketsOrderedByNumber()
        {
            var queue = new Queue
            {
                Name = "Queue 1",
                Description = "Test Queue",
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            await dbContext.QueueTickets.AddRangeAsync(
                new QueueTicket
                {
                    CustomerName = "Third",
                    Number = 3,
                    Status = QueueStatus.Waiting,
                    Priority = QueuePriority.Normal,
                    QueueId = queue.Id,
                    CreatedOn = DateTime.UtcNow
                },
                new QueueTicket
                {
                    CustomerName = "First",
                    Number = 1,
                    Status = QueueStatus.Waiting,
                    Priority = QueuePriority.VIP,
                    QueueId = queue.Id,
                    CreatedOn = DateTime.UtcNow
                },
                new QueueTicket
                {
                    CustomerName = "Second",
                    Number = 2,
                    Status = QueueStatus.Waiting,
                    Priority = QueuePriority.Normal,
                    QueueId = queue.Id,
                    CreatedOn = DateTime.UtcNow
                });

            await dbContext.SaveChangesAsync();

            var result = (await queueService.GetTicketsAsync(queue.Id)).ToList();

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].Number, Is.EqualTo(1));
            Assert.That(result[1].Number, Is.EqualTo(2));
            Assert.That(result[2].Number, Is.EqualTo(3));
        }
    }
}