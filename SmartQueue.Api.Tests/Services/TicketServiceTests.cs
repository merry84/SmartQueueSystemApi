using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private IMemoryCache cache = null!;
        private TicketService ticketService = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<SmartQueueDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            dbContext = new SmartQueueDbContext(options);
            cache = new MemoryCache(new MemoryCacheOptions());

            ticketService = new TicketService(dbContext, cache);
        }

        [TearDown]
        public void TearDown()
        {
            cache.Dispose();
            dbContext.Dispose();
        }

        [Test]
        public async Task ServeAsync_ShouldMarkCalledTicketAsServed()
        {
            var ticket = new QueueTicket
            {
                CustomerName = "Maria",
                Number = 1,
                Status = TicketStatus.Called,
                Priority = QueuePriority.Normal,
                JoinedAt = DateTime.UtcNow
            };

            await dbContext.QueueTickets.AddAsync(ticket);
            await dbContext.SaveChangesAsync();

            var result = await ticketService.ServeAsync(ticket.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Status, Is.EqualTo("Served"));

            var dbTicket = await dbContext.QueueTickets.FindAsync(ticket.Id);

            Assert.That(dbTicket!.Status, Is.EqualTo(TicketStatus.Served));
            Assert.That(dbTicket.ServedAt, Is.Not.Null);
        }

        [Test]
        public async Task ServeAsync_ShouldReturnNull_WhenTicketDoesNotExist()
        {
            var result = await ticketService.ServeAsync(999);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ServeAsync_ShouldThrow_WhenTicketIsWaiting()
        {
            var ticket = new QueueTicket
            {
                CustomerName = "Ivan",
                Number = 5,
                Status = TicketStatus.Waiting,
                Priority = QueuePriority.Normal,
                JoinedAt = DateTime.UtcNow
            };

            dbContext.QueueTickets.Add(ticket);
            dbContext.SaveChanges();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ticketService.ServeAsync(ticket.Id);
            });
        }

        [Test]
        public async Task CallNextAsync_ShouldSetCalledAt()
        {
            var queue = new Queue
            {
                Name = "Support",
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.Queues.AddAsync(queue);

            await dbContext.QueueTickets.AddAsync(new QueueTicket
            {
                CustomerName = "Peter",
                Number = 1,
                Status = TicketStatus.Waiting,
                Priority = QueuePriority.Normal,
                QueueId = queue.Id,
                JoinedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            var result = await ticketService.CallNextAsync(queue.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Status, Is.EqualTo("Called"));
            Assert.That(result.CalledOn, Is.Not.Null);
        }
    }
}