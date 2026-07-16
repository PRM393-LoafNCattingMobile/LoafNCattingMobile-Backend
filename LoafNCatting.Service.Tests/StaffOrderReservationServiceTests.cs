using System.Data;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using LoafNCatting.Service.Interfaces;
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
    public async Task GetStaffOrderAsync_ReturnsOrderDetailForCooking_WhenOrderExists()
    {
        var order = SampleOrder("Đang chờ", statusId: 1);
        var orders = new FakeOrderRepository(order);
        var service = CreateOrderService(
            orders,
            new FakeOrderStatusRepository(order.OrderStatus));

        var result = await service.GetStaffOrderAsync(order.OrderId);

        Assert.NotNull(result);
        Assert.Equal(order.OrderId, result.OrderId);
        Assert.Equal("Customer", result.CustomerName);
        var detail = Assert.Single(result.Items);
        Assert.Equal("Latte", detail.ProductName);
        Assert.Equal(2, detail.Quantity);
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

    [Fact]
    public async Task UpdateOrderStatusAsync_RejectsPreparing_WhenPaymentIsPending()
    {
        var order = SampleOrder("Đang chờ", statusId: 1, paymentStatus: "Đang chờ thanh toán");
        var orders = new FakeOrderRepository(order);
        var service = CreateOrderService(
            orders,
            new FakeOrderStatusRepository(new OrderStatus
            {
                OrderStatusId = 2,
                OrderStatusName = "Đang chuẩn bị"
            }));

        var result = await service.UpdateOrderStatusAsync(
            order.OrderId,
            actingUserId: 77,
            new StaffOrderStatusDto(StatusId: 2));

        Assert.Null(result);
        Assert.Equal("Đang chờ", order.OrderStatus.OrderStatusName);
        Assert.Equal(0, orders.SaveCount);
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

    [Fact]
    public async Task CreateReservationAsync_AssignsFirstAvailableTable_WhenTableIdIsMissing()
    {
        var reservations = new FakeReservationRepository();
        var tableService = new FakeTableService([
            new TableDto(2, "A2", 2, "Tầng 1", null, "Trống"),
            new TableDto(3, "A3", 4, "Tầng 1", null, "Trống")
        ]);
        var service = CreateReservationService(
            reservations,
            new FakeReservationStatusRepository(new ReservationStatus
            {
                StatusId = 1,
                StatusName = "Đang chờ"
            }),
            tableService);

        var result = await service.CreateReservationAsync(new CreateReservationDto(
            UserId: 5,
            Date: DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
            Time: new TimeOnly(18, 30),
            GuestName: "Customer",
            GuestPhoneNumber: "0900000000",
            NumberOfGuests: 2,
            Note: null,
            TableId: null));

        Assert.NotNull(result);
        Assert.Equal(2, result.TableId);
        Assert.Equal(2, reservations.AddedReservation?.TableId);
        Assert.Equal(1, reservations.SaveCount);
    }

    [Fact]
    public async Task CreateReservationAsync_BooksEnoughAvailableTables_ForLargeParty()
    {
        var reservations = new FakeReservationRepository();
        var tableService = new FakeTableService([
            new TableDto(2, "A2", 4, "Tầng 1", null, "Trống"),
            new TableDto(3, "A3", 4, "Tầng 1", null, "Trống"),
            new TableDto(4, "A4", 2, "Tầng 1", null, "Trống")
        ]);
        var service = CreateReservationService(
            reservations,
            new FakeReservationStatusRepository(new ReservationStatus
            {
                StatusId = 1,
                StatusName = "Đang chờ"
            }),
            tableService);

        var result = await service.CreateReservationAsync(new CreateReservationDto(
            UserId: 5,
            Date: DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
            Time: new TimeOnly(18, 30),
            GuestName: "Customer",
            GuestPhoneNumber: "0900000000",
            NumberOfGuests: 7,
            Note: null,
            TableId: null));

        Assert.NotNull(result);
        Assert.Equal([2, 3], reservations.AddedReservations.Select(reservation => reservation.TableId));
        Assert.All(reservations.AddedReservations, reservation =>
        {
            Assert.Equal(5, reservation.UserId);
            Assert.Equal(7, reservation.NumberOfGuests);
        });
        Assert.Equal(1, reservations.SaveCount);
    }

    [Fact]
    public async Task CreateReservationAsync_RejectsInvalidGuestPhoneNumber()
    {
        var reservations = new FakeReservationRepository();
        var service = CreateReservationService(
            reservations,
            new FakeReservationStatusRepository(new ReservationStatus
            {
                StatusId = 1,
                StatusName = "Đang chờ"
            }),
            new FakeTableService([
                new TableDto(2, "A2", 2, "Tầng 1", null, "Trống")
            ]));

        var result = await service.CreateReservationAsync(new CreateReservationDto(
            UserId: 5,
            Date: DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
            Time: new TimeOnly(18, 30),
            GuestName: "Customer",
            GuestPhoneNumber: "090000abcd",
            NumberOfGuests: 2,
            Note: null,
            TableId: null));

        Assert.Null(result);
        Assert.Empty(reservations.AddedReservations);
        Assert.Equal(0, reservations.SaveCount);
    }

    [Fact]
    public async Task CreateReservationAsync_NotifiesActiveStaffUsers()
    {
        var notifications = new FakeNotificationRepository();
        var service = CreateReservationService(
            new FakeReservationRepository(),
            new FakeReservationStatusRepository(new ReservationStatus
            {
                StatusId = 1,
                StatusName = "Đang chờ"
            }),
            new FakeTableService([
                new TableDto(2, "A2", 2, "Tầng 1", null, "Trống")
            ]),
            notifications: notifications,
            users: new FakeUserRepository(
                TestUser(20, "Staff"),
                TestUser(21, "Admin"),
                TestUser(22, "Staff", isActive: false)));

        var result = await service.CreateReservationAsync(new CreateReservationDto(
            UserId: 5,
            Date: DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
            Time: new TimeOnly(18, 30),
            GuestName: "Customer",
            GuestPhoneNumber: "0900000000",
            NumberOfGuests: 2,
            Note: null,
            TableId: null));

        Assert.NotNull(result);
        Assert.Contains(notifications.Notifications, item =>
            item.UserId == 20 &&
            item.Type == "reservation" &&
            item.Title.Contains("đặt bàn", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(notifications.Notifications, item => item.UserId == 21);
        Assert.DoesNotContain(notifications.Notifications, item => item.UserId == 22);
    }

    [Fact]
    public async Task UpdateReservationStatusAsync_SetsTableBooked_WhenConfirmed()
    {
        var reservation = SampleReservation("Đang chờ", statusId: 1);
        var tableRepository = new FakeTableRepository();
        var service = CreateReservationService(
            new FakeReservationRepository(reservation),
            new FakeReservationStatusRepository(new ReservationStatus
            {
                StatusId = 2,
                StatusName = "Đã xác nhận"
            }),
            tableRepository: tableRepository);

        var result = await service.UpdateReservationStatusAsync(
            reservation.ReservationId,
            new StaffReservationStatusDto(StatusId: 2));

        Assert.NotNull(result);
        Assert.Equal("Đã đặt", reservation.Table.TableStatus.StatusName);
        Assert.Same(reservation.Table, tableRepository.UpdatedTable);
    }

    [Fact]
    public async Task UpdateReservationStatusAsync_ReleasesTable_WhenCancelledAndNoOtherActiveReservation()
    {
        var reservation = SampleReservation("Đang chờ", statusId: 1);
        reservation.Table.TableStatus = new TableStatus { TableStatusId = 2, StatusName = "Đã đặt" };
        var tableRepository = new FakeTableRepository();
        var service = CreateReservationService(
            new FakeReservationRepository(reservation),
            new FakeReservationStatusRepository(new ReservationStatus
            {
                StatusId = 3,
                StatusName = "Đã hủy"
            }),
            tableRepository: tableRepository);

        var result = await service.UpdateReservationStatusAsync(
            reservation.ReservationId,
            new StaffReservationStatusDto(StatusId: 3));

        Assert.NotNull(result);
        Assert.Equal("Trống", reservation.Table.TableStatus.StatusName);
        Assert.Same(reservation.Table, tableRepository.UpdatedTable);
    }

    private static ReservationService CreateReservationService(
        IReservationRepository reservations,
        IReservationStatusRepository statuses,
        FakeTableService? tableService = null,
        FakeTableRepository? tableRepository = null,
        FakeNotificationRepository? notifications = null,
        IUserRepository? users = null)
    {
        return new ReservationService(
            reservations,
            statuses,
            notifications ?? new FakeNotificationRepository(),
            tableService ?? new FakeTableService(),
            tableRepository ?? new FakeTableRepository(),
            new FakeTableStatusRepository(
                new TableStatus { TableStatusId = 1, StatusName = "Trống" },
                new TableStatus { TableStatusId = 2, StatusName = "Đã đặt" }),
            users);
    }

    private static Order SampleOrder(string statusName, int statusId, string paymentStatus = "Đã thanh toán") => new()
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
        },
        Payments =
        {
            new Payment
            {
                PaymentId = 1,
                PaymentAmount = 45000m,
                PaymentStatus = paymentStatus
            }
        },
        OrderDetails =
        {
            new OrderDetail
            {
                OrderDetailId = 1,
                ProductId = 9,
                Product = new Product { ProductId = 9, Name = "Latte" },
                Quantity = 2,
                UnitPrice = 22500m,
                Subtotal = 45000m
            }
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

    private static User TestUser(int userId, string roleName, bool isActive = true) => new()
    {
        UserId = userId,
        Name = $"{roleName} {userId}",
        Email = $"{roleName.ToLowerInvariant()}{userId}@example.com",
        PhoneNumber = $"0900000{userId}",
        Role = new Role { RoleName = roleName },
        IsActive = isActive
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

        public Task<Order?> GetLatestPendingPaymentOrderAsync(int userId) =>
            Task.FromResult<Order?>(null);

        public Task<List<Order>> GetPendingPaymentOrdersAsync(int userId) =>
            Task.FromResult<List<Order>>([]);
    }

    private sealed class FakeOrderStatusRepository(OrderStatus status)
        : FakeRepository<OrderStatus>, IOrderStatusRepository
    {
        public override Task<OrderStatus?> GetByIdAsync(int id) =>
            Task.FromResult<OrderStatus?>(status.OrderStatusId == id ? status : null);

        public Task<OrderStatus> GetByNameAsync(string name) =>
            Task.FromResult(status);
    }

    private sealed class FakeReservationRepository(Reservation? reservation = null)
        : FakeRepository<Reservation>, IReservationRepository
    {
        public int? LastStatusId { get; private set; }
        public DateOnly? LastDate { get; private set; }
        public Reservation? AddedReservation { get; private set; }
        public List<Reservation> AddedReservations { get; } = [];
        public bool HasActiveReservationForTableResult { get; set; }

        public override Task AddAsync(Reservation entity)
        {
            entity.ReservationId = 100 + AddedReservations.Count;
            entity.Table = new RestaurantTable
            {
                TableId = entity.TableId,
                TableName = $"Table {entity.TableId}",
                TableStatus = new TableStatus { TableStatusId = 1, StatusName = "Trống" }
            };
            AddedReservation = entity;
            AddedReservations.Add(entity);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Reservation>> GetUserReservationsAsync(int userId) =>
            Task.FromResult<IEnumerable<Reservation>>([]);

        public Task<IEnumerable<Reservation>> GetStaffReservationsAsync(
            int? statusId,
            DateOnly? date)
        {
            LastStatusId = statusId;
            LastDate = date;
            return Task.FromResult<IEnumerable<Reservation>>(reservation is null ? [] : [reservation]);
        }

        public Task<Reservation?> GetByIdWithDetailsAsync(int reservationId) =>
            Task.FromResult<Reservation?>(
                reservation?.ReservationId == reservationId
                    ? reservation
                    : AddedReservation?.ReservationId == reservationId
                        ? AddedReservation
                        : AddedReservations.FirstOrDefault(item => item.ReservationId == reservationId));

        public Task<List<int>> GetUnavailableTableIdsAsync(DateOnly date, TimeOnly time) =>
            Task.FromResult<List<int>>([]);

        public Task<bool> HasActiveReservationForTableAsync(int tableId, int excludeReservationId) =>
            Task.FromResult(HasActiveReservationForTableResult);
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
        : FakeRepository<Notification>, INotificationRepository, INotificationWriter
    {
        public List<NotificationDto> Notifications { get; } = [];

        public Task<IEnumerable<Notification>> GetByUserIdAsync(int userId) =>
            Task.FromResult<IEnumerable<Notification>>([]);

        public Task<NotificationDto?> CreateAsync(int? userId, string title, string content, string type)
        {
            if (!userId.HasValue)
            {
                return Task.FromResult<NotificationDto?>(null);
            }

            var notification = new NotificationDto(
                Notifications.Count + 1,
                userId,
                title,
                content,
                type,
                false,
                DateTime.UtcNow);
            Notifications.Add(notification);
            return Task.FromResult<NotificationDto?>(notification);
        }
    }

    private sealed class FakeUserRepository(params User[] users) : FakeRepository<User>, IUserRepository
    {
        public Task<IEnumerable<User>> GetAdminUsersAsync(int? roleId, string? search, bool? active)
        {
            var query = users.AsEnumerable();
            if (active.HasValue)
            {
                query = query.Where(user => user.IsActive == active.Value);
            }

            return Task.FromResult(query);
        }

        public Task<User?> GetByIdWithRoleAsync(int id) =>
            Task.FromResult(users.FirstOrDefault(user => user.UserId == id));

        public Task<bool> ExistsByEmailOrPhoneAsync(string email, string phoneNumber) =>
            Task.FromResult(users.Any(user => user.Email == email || user.PhoneNumber == phoneNumber));

        public Task<User?> GetByEmailAsync(string email) =>
            Task.FromResult(users.FirstOrDefault(user => user.Email == email));

        public Task<User?> GetByLoginAsync(string login, string phoneNumber) =>
            Task.FromResult(users.FirstOrDefault(user => user.Email == login || user.PhoneNumber == phoneNumber));

        public Task<User?> GetFirstStaffAsync() =>
            Task.FromResult(users.FirstOrDefault(user => user.Role.RoleName == "Staff"));
    }

    private sealed class FakePaymentMethodRepository
        : FakeRepository<PaymentMethod>, IPaymentMethodRepository
    {
        public Task<PaymentMethod> GetByNameOrDefaultAsync(string name) =>
            Task.FromResult(new PaymentMethod { MethodId = 1, MethodName = name });
    }

    private sealed class FakeTableService(List<TableDto>? availableTables = null) : LoafNCatting.Service.Interfaces.ITableService
    {
        public Task<List<TableDto>> GetAvailableTablesAsync(
            DateOnly date,
            TimeOnly time,
            int guestCount) => Task.FromResult(availableTables ?? []);

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

    private sealed class FakeTableRepository : FakeRepository<RestaurantTable>, IRestaurantTableRepository
    {
        public RestaurantTable? UpdatedTable { get; private set; }

        public Task<IEnumerable<RestaurantTable>> GetAvailableTablesAsync(
            DateOnly date,
            TimeOnly time,
            int guestCount) => Task.FromResult<IEnumerable<RestaurantTable>>([]);

        public Task<IEnumerable<RestaurantTable>> GetTablesAsync() =>
            Task.FromResult<IEnumerable<RestaurantTable>>([]);

        public Task<RestaurantTable?> GetByIdWithStatusAsync(int id) =>
            Task.FromResult<RestaurantTable?>(null);

        public override void Update(RestaurantTable entity)
        {
            UpdatedTable = entity;
        }
    }

    private sealed class FakeTableStatusRepository(params TableStatus[] statuses)
        : FakeRepository<TableStatus>(statuses), ITableStatusRepository;

    private abstract class FakeRepository<T>(params T[] items) : IGenericRepository<T> where T : class
    {
        protected List<T> Items { get; } = items.ToList();
        public int SaveCount { get; private set; }

        public virtual Task<T?> GetByIdAsync(int id) => Task.FromResult<T?>(null);

        public virtual Task<IEnumerable<T>> GetAllAsync() =>
            Task.FromResult<IEnumerable<T>>(Items);

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
