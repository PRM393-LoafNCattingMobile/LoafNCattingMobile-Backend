using System.Data;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using LoafNCatting.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace LoafNCatting.Service.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_RejectsCumulativeQuantityAboveStock()
    {
        var product = TestProduct(unitInStock: 3);
        var orders = new FakeOrderRepository();
        var service = CreateService(orders, new FakeProductRepository(product));
        var request = new CreateOrderRequestDto(
            7,
            null,
            null,
            "Mang di",
            null,
            "Tien mat",
            [new OrderItemRequestDto(product.ProductId, 2), new OrderItemRequestDto(product.ProductId, 2)]);

        var order = await service.CreateOrderAsync(request);

        Assert.Null(order);
        Assert.Null(orders.AddedOrder);
        Assert.Equal(3, product.UnitInStock);
        Assert.Equal(0, orders.SaveCount);
    }

    [Fact]
    public async Task CreateOrderAsync_DecrementsStockAfterOrderIsCreated()
    {
        var product = TestProduct(unitInStock: 3);
        var orders = new FakeOrderRepository();
        var service = CreateService(orders, new FakeProductRepository(product));
        var request = new CreateOrderRequestDto(
            7,
            null,
            null,
            "Mang di",
            null,
            "Tien mat",
            [new OrderItemRequestDto(product.ProductId, 2)]);

        var order = await service.CreateOrderAsync(request);

        Assert.NotNull(order);
        Assert.Equal(1, product.UnitInStock);
        Assert.Equal(1, orders.SaveCount);
        Assert.Equal(1, orders.AddedOrder?.OrderDetails.Count);
    }

    [Fact]
    public async Task CreateOrderAsync_RejectsNewOrder_WhenUserHasPendingPaymentOrder()
    {
        var product = TestProduct(unitInStock: 3);
        var orders = new FakeOrderRepository
        {
            PendingPaymentOrder = new Order
            {
                OrderId = 99,
                CustomerUserId = 7
            }
        };
        var service = CreateService(orders, new FakeProductRepository(product));
        var request = new CreateOrderRequestDto(
            7,
            null,
            null,
            "Mang di",
            null,
            "Tien mat",
            [new OrderItemRequestDto(product.ProductId, 1)]);

        var order = await service.CreateOrderAsync(request);

        Assert.Null(order);
        Assert.Null(orders.AddedOrder);
        Assert.Equal(3, product.UnitInStock);
        Assert.Equal(0, orders.SaveCount);
    }

    [Fact]
    public async Task GetPendingPaymentOrderAsync_ExpiresOldPendingPaymentOrder_AndRestoresStock()
    {
        var product = TestProduct(unitInStock: 0);
        product.IsAvailable = false;
        var order = new Order
        {
            OrderId = 99,
            CustomerUserId = 7,
            OrderDate = DateTime.UtcNow.AddSeconds(-2),
            CreatedAt = DateTime.UtcNow.AddSeconds(-2),
            OrderStatusId = 1,
            OrderStatus = new OrderStatus { OrderStatusId = 1, OrderStatusName = "Đang chờ" },
            Payments =
            {
                new Payment { PaymentStatus = "Đang chờ thanh toán" }
            },
            OrderDetails =
            {
                new OrderDetail
                {
                    ProductId = product.ProductId,
                    Product = product,
                    Quantity = 2,
                    UnitPrice = product.Price,
                    Subtotal = product.Price * 2
                }
            }
        };
        var orders = new FakeOrderRepository();
        orders.PendingPaymentOrders.Add(order);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:PendingPaymentExpirySeconds"] = "1"
            })
            .Build();
        var service = CreateService(orders, new FakeProductRepository(product), configuration);

        var pending = await service.GetPendingPaymentOrderAsync(7);

        Assert.Null(pending);
        Assert.Equal("Đã hủy", order.Payments.First().PaymentStatus);
        Assert.Equal("Đã hủy", order.OrderStatus.OrderStatusName);
        Assert.Equal(2, product.UnitInStock);
        Assert.True(product.IsAvailable);
        Assert.Equal(1, orders.SaveCount);
    }

    private static OrderService CreateService(
        IOrderRepository orders,
        IProductRepository products,
        IConfiguration? configuration = null)
    {
        return new OrderService(
            orders,
            products,
            new FakeNotificationRepository(),
            new FakeOrderStatusRepository(),
            new FakePaymentMethodRepository(),
            configuration);
    }

    private static Product TestProduct(int unitInStock) => new()
    {
        ProductId = 10,
        Name = "Cappuccino",
        Price = 45000m,
        UnitInStock = unitInStock,
        CategoryId = 3,
        Category = new Category { CategoryId = 3, Name = "Drinks" },
        IsAvailable = true
    };

    private sealed class FakeOrderRepository : FakeRepository<Order>, IOrderRepository
    {
        public Order? AddedOrder { get; private set; }
        public Order? PendingPaymentOrder { get; init; }
        public List<Order> PendingPaymentOrders { get; } = [];

        public override Task AddAsync(Order entity)
        {
            entity.OrderId = 123;
            entity.OrderStatus = new OrderStatus { OrderStatusId = 1, OrderStatusName = "Dang cho" };
            foreach (var detail in entity.OrderDetails)
            {
                detail.Order = entity;
            }
            AddedOrder = entity;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Order>> GetUserOrdersAsync(int userId)
        {
            return Task.FromResult(Enumerable.Empty<Order>());
        }

        public Task<IEnumerable<Order>> GetStaffOrdersAsync(int? statusId, DateOnly? date)
        {
            return Task.FromResult(Enumerable.Empty<Order>());
        }

        public Task<Order?> GetByIdWithDetailsAsync(int orderId)
        {
            return Task.FromResult(AddedOrder?.OrderId == orderId ? AddedOrder : null);
        }

        public Task<Order?> GetLatestPendingPaymentOrderAsync(int userId)
        {
            var pendingOrder = PendingPaymentOrders
                .Where(order =>
                    order.CustomerUserId == userId &&
                    order.Payments.Any(payment => payment.PaymentStatus == "Đang chờ thanh toán") &&
                    order.OrderStatus.OrderStatusName == "Đang chờ")
                .OrderByDescending(order => order.OrderDate)
                .FirstOrDefault();

            return Task.FromResult(pendingOrder ?? (PendingPaymentOrder?.CustomerUserId == userId
                ? PendingPaymentOrder
                : null));
        }

        public Task<List<Order>> GetPendingPaymentOrdersAsync(int userId)
        {
            return Task.FromResult(PendingPaymentOrders
                .Where(order =>
                    order.CustomerUserId == userId &&
                    order.Payments.Any(payment => payment.PaymentStatus == "Đang chờ thanh toán") &&
                    order.OrderStatus.OrderStatusName == "Đang chờ")
                .OrderByDescending(order => order.OrderDate)
                .ToList());
        }
    }

    private sealed class FakeProductRepository(params Product[] products) : FakeRepository<Product>, IProductRepository
    {
        public Task<IEnumerable<Product>> GetProductsAsync(int? categoryId, string? search)
        {
            return Task.FromResult(products.AsEnumerable());
        }

        public Task<Product?> GetByIdWithCategoryAsync(int id)
        {
            return Task.FromResult(products.FirstOrDefault(product => product.ProductId == id));
        }

        public Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var productIds = ids.ToHashSet();
            return Task.FromResult(products.Where(product => productIds.Contains(product.ProductId)).ToList());
        }

        public Task<bool> TryReserveStockAsync(IReadOnlyDictionary<int, int> quantitiesByProductId)
        {
            if (quantitiesByProductId.Any(item =>
                products.FirstOrDefault(product => product.ProductId == item.Key) is not { IsAvailable: true } product ||
                product.UnitInStock < item.Value))
            {
                return Task.FromResult(false);
            }

            foreach (var item in quantitiesByProductId)
            {
                var product = products.First(product => product.ProductId == item.Key);
                product.UnitInStock -= item.Value;
                product.IsAvailable = product.UnitInStock > 0 && product.IsAvailable;
            }

            return Task.FromResult(true);
        }
    }

    private sealed class FakeNotificationRepository : FakeRepository<Notification>, INotificationRepository, INotificationWriter
    {
        public Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
        {
            return Task.FromResult(Enumerable.Empty<Notification>());
        }

        public Task<NotificationDto?> CreateAsync(int? userId, string title, string content, string type)
        {
            return Task.FromResult<NotificationDto?>(userId.HasValue
                ? new NotificationDto(1, userId, title, content, type, false, DateTime.UtcNow)
                : null);
        }
    }

    private sealed class FakeOrderStatusRepository : FakeRepository<OrderStatus>, IOrderStatusRepository
    {
        public Task<OrderStatus> GetByNameAsync(string name)
        {
            return Task.FromResult(new OrderStatus { OrderStatusId = 1, OrderStatusName = name });
        }
    }

    private sealed class FakePaymentMethodRepository : FakeRepository<PaymentMethod>, IPaymentMethodRepository
    {
        public Task<PaymentMethod> GetByNameOrDefaultAsync(string name)
        {
            return Task.FromResult(new PaymentMethod { MethodId = 1, MethodName = name });
        }
    }

    private abstract class FakeRepository<T> : IGenericRepository<T> where T : class
    {
        public int SaveCount { get; private set; }

        public virtual Task<T?> GetByIdAsync(int id) => Task.FromResult<T?>(null);

        public virtual Task<IEnumerable<T>> GetAllAsync() => Task.FromResult(Enumerable.Empty<T>());

        public virtual Task AddAsync(T entity) => Task.CompletedTask;

        public virtual void Update(T entity) { }

        public virtual void Delete(T entity) { }

        public Task<int> SaveChangesAsync()
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return Task.FromResult<IDbContextTransaction>(new FakeTransaction());
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            return Task.FromResult<IDbContextTransaction>(new FakeTransaction());
        }
    }

    private sealed class FakeTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public bool SupportsSavepoints => false;
        public void Commit() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Rollback() { }
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void CreateSavepoint(string name) { }
        public Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void RollbackToSavepoint(string name) { }
        public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void ReleaseSavepoint(string name) { }
        public Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
