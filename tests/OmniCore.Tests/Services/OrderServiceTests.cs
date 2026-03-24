using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using OmniCore.Application.DTOs.Orders;
using OmniCore.Domain.Entities;
using OmniCore.Infrastructure.Services;
using OmniCore.Persistence.Contexts;

namespace OmniCore.Tests.Services
{
    public class OrderServiceTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ApplicationDbContext(options);
        }

        private OrderService GetService(ApplicationDbContext context)
        {
            var loggerMock = new Mock<ILogger<OrderService>>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            return new OrderService(context, loggerMock.Object, memoryCache);
        }

        [Fact]
        public async Task CreateOrderAsync_Should_Create_Order_Successfully()
        {
            // Arrange
            var context = GetDbContext();

            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            context.Users.Add(new User
            {
                Id = customerId,
                Email = "test@test.com"
            });

            context.Products.Add(new Product
            {
                Id = productId,
                Name = "Test Product",
                Price = 100,
                Stock = 10,
                IsActive = true
            });

            await context.SaveChangesAsync();

            var service = GetService(context);

            var request = new CreateOrderRequest
            {
                CustomerId = customerId,
                Items = new List<CreateOrderItemRequest>
                {
                    new CreateOrderItemRequest
                    {
                        ProductId = productId,
                        Quantity = 2
                    }
                }
            };

            // Act
            var result = await service.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.CustomerId.Should().Be(customerId);
            result.TotalPrice.Should().Be(200);
            result.Items.Should().HaveCount(1);

            var product = await context.Products.FirstAsync();
            product.Stock.Should().Be(8); // stock reduced
        }

        [Fact]
        public async Task CreateOrderAsync_Should_Throw_When_Product_Not_Found()
        {
            // Arrange
            var context = GetDbContext();

            var customerId = Guid.NewGuid();

            context.Users.Add(new User
            {
                Id = customerId,
                Email = "test@test.com"
            });

            await context.SaveChangesAsync();

            var service = GetService(context);

            var request = new CreateOrderRequest
            {
                CustomerId = customerId,
                Items = new List<CreateOrderItemRequest>
        {
            new CreateOrderItemRequest
            {
                ProductId = Guid.NewGuid(), // NOT in DB
                Quantity = 1
            }
        }
            };

            // Act
            Func<Task> act = async () => await service.CreateOrderAsync(request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task CreateOrderAsync_Should_Throw_When_Stock_Is_Insufficient()
        {
            // Arrange
            var context = GetDbContext();

            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            context.Users.Add(new User
            {
                Id = customerId,
                Email = "test@test.com"
            });

            context.Products.Add(new Product
            {
                Id = productId,
                Name = "Test Product",
                Price = 100,
                Stock = 1, // LOW STOCK
                IsActive = true
            });

            await context.SaveChangesAsync();

            var service = GetService(context);

            var request = new CreateOrderRequest
            {
                CustomerId = customerId,
                Items = new List<CreateOrderItemRequest>
        {
            new CreateOrderItemRequest
            {
                ProductId = productId,
                Quantity = 5 // MORE THAN STOCK
            }
        }
            };

            // Act
            Func<Task> act = async () => await service.CreateOrderAsync(request);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("*Insufficient stock*");
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_From_Cache_On_Second_Call()
        {
            // Arrange
            var context = GetDbContext();

            var orderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            context.Products.Add(new Product
            {
                Id = productId,
                Name = "Test Product",
                Price = 100,
                Stock = 10,
                IsActive = true
            });

            context.Orders.Add(new Order
            {
                Id = orderId,
                CustomerId = customerId,
                TotalPrice = 100,
                Status = OmniCore.Domain.Enums.OrderStatus.Pending,
                Items = new List<OrderItem>
        {
            new OrderItem
            {
                ProductId = productId,
                Quantity = 1,
                UnitPrice = 100
            }
        }
            });

            await context.SaveChangesAsync();

            var service = GetService(context);

            // Act 1 → first call (DB hit)
            var firstResult = await service.GetByIdAsync(orderId);

            // Act 2 → second call (should be cached)
            var secondResult = await service.GetByIdAsync(orderId);

            // Assert
            secondResult.Should().NotBeNull();
            secondResult.Id.Should().Be(orderId);

            // Bonus: same reference means it came from cache
            secondResult.Should().BeSameAs(firstResult);
        }
    }
}