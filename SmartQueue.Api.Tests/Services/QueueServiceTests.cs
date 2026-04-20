using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SmartQueue.Api.Data;
using SmartQueue.Api.DTOs;
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
    }
}