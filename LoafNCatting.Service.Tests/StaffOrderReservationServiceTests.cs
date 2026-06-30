using System.Data;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoafNCatting.Service.Tests;

public class StaffOrderReservationServiceTests
{
    [Fact]
    public async Task GetStaffReservationsAsync_PassesStatusAndDateFilters()
    {
        var reservation = SampleReservation("Đang chờ", statusId: 1);
        var reservations = new FakeReservationRepository(reservation);
        var service = CreateReservationService(
            reservations,
            new FakeReservationStatusRepository(reservation.Status));
        var date = new DateOnly(2026, 6, 30);

        var result = await service.GetStaffReservationsAsync(statusId: 1, date);

        var item = Assert.Single(result);
        Assert.Equal(reservation.GuestName, item.GuestName);
        Assert.Equal(1, reservations.LastStatusId);
        Assert.Equal(date, reservations.LastDate);
    }

    [Theory]
    [InlineData("Đang chờ", "Đã xác nhận", true)]
    [InlineData("Đang chờ", "Đã hủy", true)]
    [InlineData("Đã xác nhận", "Hoàn thành", true)]
    [InlineData("Đã xác nhận", "Đã hủy", true)]
    [InlineData("Đã xác nhận", "Không đến", true)]
    [InlineData("Đang chờ", "Hoàn thành", false)]
    [InlineData("Hoàn thành", "Đã xác nhận", false)]
    [InlineData("Đã hủy", "Đang chờ", false)]
    [InlineData("Không đến", "Đã xác nhận", false)]
    public async Task UpdateReservationStatusAsync_EnforcesReservationWorkflow(
        string currentStatusName,
        string targetStatusName,
        bool expectedSuccess)
    {
        var reservation = SampleReservation(currentStatusName, statusId: 1);
        var reservations = new FakeReservationRepository(reservation);
        var service = CreateReservationService(
            reservations,
            new FakeReservationStatusRepository(new ReservationStatus
            {
                StatusId = 2,
                StatusName = targetStatusName
            }));
        var startedAt = DateTime.UtcNow;

        var result = await service.UpdateReservationStatusAsync(
            reservation.ReservationId,
            new StaffReservationStatusDto(StatusId: 2));

        Assert.Equal(expectedSuccess, result is not null);
        Assert.Equal(expectedSuccess ? 1 : 0, reservations.SaveCount);
        if (expectedSuccess)
        {
            Assert.NotNull(reservation.UpdatedAt);
            Assert.True(reservation.UpdatedAt >= startedAt);
            Assert.Equal(targetStatusName, result!.StatusName);
        }
    }

    [Fact]
    public async Task GetStaffOrdersAsync_ReturnsCustomerName_AndPassesFilters()
    {
        var order = SampleOrder("Đang chờ", statusId: 1);
        var orders = new FakeOrderRepository(order);
        var service = CreateOrderService(
            orders,
            new FakeOrderStatusRepository(order.OrderStatus));
        var date = new DateOnly(2026, 6, 30);

        var result = await service.GetStaffOrdersAsync(statusId: 1, date);

        var item = Assert.Single(result);
        Assert.Equal("Customer", item.CustomerName);
        Assert.Equal(1, orders.LastStatusId);
        Assert.Equal(date, orders.LastDate);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_AssignsActingStaffAndTimestamp_WhenTransitionIsAllowed()
    {
        var order = SampleOrder("Đang chờ", statusId: 1);
        var orders = new FakeOrderRepository(order);
        var service = CreateOrderService(
            orders,
            new FakeOrderStatusRepository(new OrderStatus
            {
                OrderStatusId = 2,
                OrderStatusName = "Đang chuẩn bị"
            }));
        var startedAt = DateTime.UtcNow;

        var result = await service.UpdateOrderStatusAsync(
            order.OrderId,
            actingUserId: 77,
            new StaffOrderStatusDto(StatusId: 2));

        Assert.NotNull(result);
        Assert.Equal(77, order.StaffUserId);
        Assert.Equal(2, order.OrderStatusId);
        Assert.Equal("Đang chuẩn bị", result.StatusName);
        Assert.NotNull(order.UpdatedAt);
        Assert.True(order.UpdatedAt >= startedAt);
        Assert.Equal(1, orders.SaveCount);
    }

    [Theory]
    [InlineData("Đang chờ", "Đã hủy", true)]
    [InlineData("Đang chuẩn bị", "Hoàn thành", true)]
    [InlineData("Đang chuẩn bị", "Đã hủy", true)]
    [InlineData("Đang chờ", "Hoàn thành", false)]
    [InlineData("Hoàn thành", "Đang chuẩn bị", false)]
    [InlineData("Đã hủy", "Đang chờ", false)]
    public async Task UpdateOrderStatusAsync_EnforcesOrderWorkflow(
        string currentStatusName,
        string targetStatusName,
        bool expectedSuccess)
    {
        var order = SampleOrder(currentStatusName, statusId: 1);
        var orders = new FakeOrderRepository(order);
        var service = CreateOrderService(
            orders,
            new FakeOrderStatusRepository(new OrderStatus
            {
                OrderStatusId = 2,
                OrderStatusName = targetStatusName
            }));

        var result = await service.UpdateOrderStatusAsync(
            order.OrderId,
            actingUserId: 77,
            new StaffOrderStatusDto(StatusId: 2));

        Assert.Equal(expectedSuccess, result is not null);
        Assert.Equal(expectedSuccess ? 1 : 0, orders.SaveCount);
    }

    private static OrderService CreateOrderService(
        IOrderRepository orders,
        IOrderStatusRepository statuses)
    {
        return new OrderService(
            orders,
            new FakeProductRepository(),
            new FakeNotificationRepository(),
            statuses,
            new FakePaymentMethodRepository());
    }

    private static ReservationService CreateReservationService(
        IReservationRepository reservations,
        IReservationStatusRepository statuses)
    {
        return new ReservationService(
            reservations,
            statuses,
            new FakeNotificationRepository(),
            new FakeTableService());
    }

    private static Order SampleOrder(string statusName, int statusId) => new()
    {
        OrderId = 10,
        OrderDate = DateTime.UtcNow,
        TotalPrice = 45000m,
        CustomerUserId = 5,
        CustomerUser = new User { UserId = 5, Name = "Customer" },
        OrderStatusId = statusId,
        OrderStatus = new OrderStatus
        {
            OrderStatusId = statusId,
            OrderStatusName = statusName
        }
    };

    private static Reservation SampleReservation(string statusName, int statusId) => new()
    {
        ReservationId = 20,
        UserId = 5,
        Date = new DateOnly(2026, 6, 30),
        Time = new TimeOnly(18, 30),
        GuestName = "Customer",
        GuestPhoneNumber = "0900000000",
        NumberOfGuests = 2,
        StatusId = statusId,
        Status = new ReservationStatus
        {
            StatusId = statusId,
            StatusName = statusName
        },
        TableId = 3,
        Table = new RestaurantTable { TableId = 3, TableName = "A3" }
    };

    private sealed class FakeOrderRepository(Order order) : FakeRepository<Order>, IOrderRepository
    {
        public int? LastStatusId { get; private set; }
        public DateOnly? LastDate { get; private set; }

        public Task<IEnumerable<Order>> GetUserOrdersAsync(int userId) =>
            Task.FromResult<IEnumerable<Order>>([]);

        public Task<IEnumerable<Order>> GetStaffOrdersAsync(int? statusId, DateOnly? date)
        {
            LastStatusId = statusId;
            LastDate = date;
            return Task.FromResult<IEnumerable<Order>>([order]);
        }

        public Task<Order?> GetByIdWithDetailsAsync(int orderId) =>
            Task.FromResult<Order?>(order.OrderId == orderId ? order : null);
    }

    private sealed class FakeOrderStatusRepository(OrderStatus status)
        : FakeRepository<OrderStatus>, IOrderStatusRepository
    {
        public override Task<OrderStatus?> GetByIdAsync(int id) =>
            Task.FromResult<OrderStatus?>(status.OrderStatusId == id ? status : null);

        public Task<OrderStatus> GetByNameAsync(string name) =>
            Task.FromResult(status);
    }

    private sealed class FakeReservationRepository(Reservation reservation)
        : FakeRepository<Reservation>, IReservationRepository
    {
        public int? LastStatusId { get; private set; }
        public DateOnly? LastDate { get; private set; }

        public Task<IEnumerable<Reservation>> GetUserReservationsAsync(int userId) =>
            Task.FromResult<IEnumerable<Reservation>>([]);

        public Task<IEnumerable<Reservation>> GetStaffReservationsAsync(
            int? statusId,
            DateOnly? date)
        {
            LastStatusId = statusId;
            LastDate = date;
            return Task.FromResult<IEnumerable<Reservation>>([reservation]);
        }

        public Task<Reservation?> GetByIdWithDetailsAsync(int reservationId) =>
            Task.FromResult<Reservation?>(
                reservation.ReservationId == reservationId ? reservation : null);

        public Task<List<int>> GetUnavailableTableIdsAsync(DateOnly date, TimeOnly time) =>
            Task.FromResult<List<int>>([]);
    }

    private sealed class FakeReservationStatusRepository(ReservationStatus status)
        : FakeRepository<ReservationStatus>, IReservationStatusRepository
    {
        public override Task<ReservationStatus?> GetByIdAsync(int id) =>
            Task.FromResult<ReservationStatus?>(status.StatusId == id ? status : null);

        public Task<ReservationStatus> GetByNameAsync(string name) =>
            Task.FromResult(status);
    }

    private sealed class FakeProductRepository : FakeRepository<Product>, IProductRepository
    {
        public Task<IEnumerable<Product>> GetProductsAsync(int? categoryId, string? search) =>
            Task.FromResult<IEnumerable<Product>>([]);

        public Task<Product?> GetByIdWithCategoryAsync(int id) =>
            Task.FromResult<Product?>(null);

        public Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids) =>
            Task.FromResult<List<Product>>([]);

        public Task<bool> TryReserveStockAsync(IReadOnlyDictionary<int, int> quantitiesByProductId) =>
            Task.FromResult(false);
    }

    private sealed class FakeNotificationRepository
        : FakeRepository<Notification>, INotificationRepository
    {
        public Task<IEnumerable<Notification>> GetByUserIdAsync(int userId) =>
            Task.FromResult<IEnumerable<Notification>>([]);
    }

    private sealed class FakePaymentMethodRepository
        : FakeRepository<PaymentMethod>, IPaymentMethodRepository
    {
        public Task<PaymentMethod> GetByNameOrDefaultAsync(string name) =>
            Task.FromResult(new PaymentMethod { MethodId = 1, MethodName = name });
    }

    private sealed class FakeTableService : LoafNCatting.Service.Interfaces.ITableService
    {
        public Task<List<TableDto>> GetAvailableTablesAsync(
            DateOnly date,
            TimeOnly time,
            int guestCount) => Task.FromResult<List<TableDto>>([]);

        public Task<List<TableDto>> GetTablesAsync() => Task.FromResult<List<TableDto>>([]);

        public Task<TableDto?> GetTableAsync(int id) => Task.FromResult<TableDto?>(null);

        public Task<TableDto?> CreateTableAsync(AdminTableRequestDto request) =>
            Task.FromResult<TableDto?>(null);

        public Task<TableDto?> UpdateTableAsync(int id, AdminTableRequestDto request) =>
            Task.FromResult<TableDto?>(null);

        public Task<TableDto?> UpdateTableStatusAsync(int id, StaffTableStatusDto request) =>
            Task.FromResult<TableDto?>(null);

        public Task<bool> DeleteTableAsync(int id) => Task.FromResult(false);
    }

    private abstract class FakeRepository<T> : IGenericRepository<T> where T : class
    {
        public int SaveCount { get; private set; }

        public virtual Task<T?> GetByIdAsync(int id) => Task.FromResult<T?>(null);

        public virtual Task<IEnumerable<T>> GetAllAsync() =>
            Task.FromResult<IEnumerable<T>>([]);

        public virtual Task AddAsync(T entity) => Task.CompletedTask;

        public virtual void Update(T entity) { }

        public virtual void Delete(T entity) { }

        public Task<int> SaveChangesAsync()
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync() =>
            Task.FromResult<IDbContextTransaction>(new FakeTransaction());

        public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel) =>
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
