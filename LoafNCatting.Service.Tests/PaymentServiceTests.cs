using System.Data;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using LoafNCatting.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Net.payOS.Types;

namespace LoafNCatting.Service.Tests;

public class PaymentServiceTests
{
    [Fact]
    public async Task GetPaymentStatusAsync_MarksPaymentPaid_AndKeepsOrderPending()
    {
        var order = SampleOrder(paymentStatus: "Đang chờ thanh toán");
        order.Payments.First().TransactionCode = "123";
        var orders = new FakeOrderRepository(order);
        var notifications = new FakeNotificationWriter();
        var service = CreateService("PAID", orders, notifications);

        var result = await service.GetPaymentStatusAsync(order.OrderId, userId: 7);

        Assert.NotNull(result);
        Assert.True(result.IsPaid);
        Assert.Equal("Đã thanh toán", order.Payments.First().PaymentStatus);
        Assert.NotNull(order.Payments.First().PaidAt);
        Assert.Equal("Đang chờ", order.OrderStatus.OrderStatusName);
        Assert.Equal(1, orders.SaveCount);
        Assert.Contains(notifications.Items, notification => notification.Type == "payment");
    }

    [Theory]
    [InlineData("CANCELLED")]
    [InlineData("EXPIRED")]
    public async Task GetPaymentStatusAsync_CancelsPendingOrder_WhenPaymentIsCancelledOrExpired(
        string payOsStatus)
    {
        var order = SampleOrder(paymentStatus: "Đang chờ thanh toán");
        order.Payments.First().TransactionCode = "123";
        var orders = new FakeOrderRepository(order);
        var notifications = new FakeNotificationWriter();
        var service = CreateService(payOsStatus, orders, notifications);

        var result = await service.GetPaymentStatusAsync(order.OrderId, userId: 7);

        Assert.NotNull(result);
        Assert.False(result.IsPaid);
        Assert.Equal("Đã hủy", order.Payments.First().PaymentStatus);
        Assert.Equal("Đã hủy", order.OrderStatus.OrderStatusName);
        Assert.NotNull(order.UpdatedAt);
        Assert.Equal(1, orders.SaveCount);
        Assert.Contains(notifications.Items, notification => notification.Type == "payment");
    }

    private static PaymentService CreateService(
        string payOsStatus,
        IOrderRepository orders,
        INotificationWriter notifications)
    {
        return new PaymentService(
            new FakePayOsClient(payOsStatus),
            orders,
            new FakeOrderStatusRepository(),
            notifications,
            new ConfigurationBuilder().Build());
    }

    private static Order SampleOrder(string paymentStatus) => new()
    {
        OrderId = 42,
        CustomerUserId = 7,
        TotalPrice = 50000m,
        OrderStatusId = 1,
        OrderStatus = new OrderStatus { OrderStatusId = 1, OrderStatusName = "Đang chờ" },
        Payments =
        {
            new Payment
            {
                PaymentId = 9,
                PaymentAmount = 50000m,
                PaymentStatus = paymentStatus
            }
        }
    };

    private sealed class FakePayOsClient(string status) : IPayOsClient
    {
        public Task<CreatePaymentResult> CreatePaymentLinkAsync(PaymentData paymentData)
        {
            return Task.FromResult(new CreatePaymentResult(
                "970422",
                "123456789",
                paymentData.amount,
                paymentData.description,
                paymentData.orderCode,
                "VND",
                "link-id",
                "PENDING",
                null,
                "https://example.com/checkout",
                "qr"));
        }

        public Task<PaymentLinkInformation> GetPaymentLinkInformationAsync(long orderCode)
        {
            return Task.FromResult(new PaymentLinkInformation(
                "link-id",
                orderCode,
                50000,
                status == "PAID" ? 50000 : 0,
                status == "PAID" ? 0 : 50000,
                status,
                DateTime.UtcNow.ToString("O"),
                [],
                status == "PAID" ? null : DateTime.UtcNow.ToString("O"),
                status == "PAID" ? null : "test"));
        }
    }

    private sealed class FakeOrderRepository(Order order) : FakeRepository<Order>, IOrderRepository
    {
        public int SaveCount { get; private set; }

        public Task<IEnumerable<Order>> GetUserOrdersAsync(int userId) =>
            Task.FromResult<IEnumerable<Order>>([]);

        public Task<IEnumerable<Order>> GetStaffOrdersAsync(int? statusId, DateOnly? date) =>
            Task.FromResult<IEnumerable<Order>>([]);

        public Task<Order?> GetByIdWithDetailsAsync(int orderId) =>
            Task.FromResult<Order?>(order.OrderId == orderId ? order : null);

        public Task<Order?> GetLatestPendingPaymentOrderAsync(int userId) =>
            Task.FromResult<Order?>(null);

        public override Task<int> SaveChangesAsync()
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeOrderStatusRepository : FakeRepository<OrderStatus>, IOrderStatusRepository
    {
        public Task<OrderStatus> GetByNameAsync(string name) =>
            Task.FromResult(new OrderStatus
            {
                OrderStatusId = name == "Đã hủy" ? 4 : 1,
                OrderStatusName = name
            });
    }

    private sealed class FakeNotificationWriter : INotificationWriter
    {
        public List<NotificationDto> Items { get; } = [];

        public Task<NotificationDto?> CreateAsync(int? userId, string title, string content, string type)
        {
            if (!userId.HasValue)
            {
                return Task.FromResult<NotificationDto?>(null);
            }

            var notification = new NotificationDto(
                Items.Count + 1,
                userId,
                title,
                content,
                type,
                false,
                DateTime.UtcNow);
            Items.Add(notification);
            return Task.FromResult<NotificationDto?>(notification);
        }
    }

    private abstract class FakeRepository<T> : IGenericRepository<T> where T : class
    {
        public virtual Task<T?> GetByIdAsync(int id) => Task.FromResult<T?>(null);
        public virtual Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>([]);
        public virtual Task AddAsync(T entity) => Task.CompletedTask;
        public virtual void Update(T entity) { }
        public virtual void Delete(T entity) { }
        public virtual Task<int> SaveChangesAsync() => Task.FromResult(0);
        public virtual Task<IDbContextTransaction> BeginTransactionAsync() =>
            Task.FromResult<IDbContextTransaction>(new FakeTransaction());
        public virtual Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel) =>
            Task.FromResult<IDbContextTransaction>(new FakeTransaction());
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
