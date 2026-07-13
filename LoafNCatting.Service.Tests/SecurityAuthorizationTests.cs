using System.Data;
using System.Reflection;
using LoafNCatting.Api.Controllers;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Net.payOS.Types;

namespace LoafNCatting.Service.Tests;

public class SecurityAuthorizationTests
{
    [Fact]
    public void SessionAuthorization_TryRequireAdmin_AcceptsAdminSession()
    {
        var result = RequireRole("TryRequireAdmin", "Admin");

        Assert.True(result.Allowed);
        Assert.NotNull(result.Session);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void SessionAuthorization_TryRequireAdmin_RejectsStaffSession()
    {
        var result = RequireRole("TryRequireAdmin", "Staff");

        Assert.False(result.Allowed);
        Assert.Null(result.Session);
        AssertForbidden(result.Failure);
    }

    [Fact]
    public void SessionAuthorization_TryRequireAdmin_RejectsCustomerSession()
    {
        var result = RequireRole("TryRequireAdmin", "Customer");

        Assert.False(result.Allowed);
        Assert.Null(result.Session);
        AssertForbidden(result.Failure);
    }

    [Fact]
    public void SessionAuthorization_TryRequireStaffOrAdmin_AcceptsAdminSession()
    {
        var result = RequireRole("TryRequireStaffOrAdmin", "Admin");

        Assert.True(result.Allowed);
        Assert.NotNull(result.Session);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void SessionAuthorization_TryRequireStaffOrAdmin_AcceptsStaffSession()
    {
        var result = RequireRole("TryRequireStaffOrAdmin", "Staff");

        Assert.True(result.Allowed);
        Assert.NotNull(result.Session);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void SessionAuthorization_TryRequireStaffOrAdmin_RejectsCustomerSession()
    {
        var result = RequireRole("TryRequireStaffOrAdmin", "Customer");

        Assert.False(result.Allowed);
        Assert.Null(result.Session);
        AssertForbidden(result.Failure);
    }

    [Fact]
    public async Task PaymentService_GetStatus_ReturnsNull_WhenOrderDoesNotBelongToUser()
    {
        var order = SampleOrder(customerUserId: 7);
        var service = new PaymentService(
            new FakePayOsClient(),
            new FakeOrderRepository(order),
            new FakeOrderStatusRepository(),
            new FakeNotificationRepository(),
            new ConfigurationBuilder().Build());

        var status = await service.GetPaymentStatusAsync(order.OrderId, userId: 99);

        Assert.Null(status);
    }

    [Fact]
    public async Task PaymentService_CreatePaymentLink_ReturnsNull_WhenOrderDoesNotBelongToUser()
    {
        var order = SampleOrder(customerUserId: 7);
        var service = new PaymentService(
            new FakePayOsClient(),
            new FakeOrderRepository(order),
            new FakeOrderStatusRepository(),
            new FakeNotificationRepository(),
            new ConfigurationBuilder().Build());

        var link = await service.CreatePaymentLinkAsync(order.OrderId, userId: 99);

        Assert.Null(link);
    }

    [Fact]
    public async Task PaymentService_GetStatus_ReturnsPaidStatus_WhenOrderBelongsToUser()
    {
        var order = SampleOrder(customerUserId: 7, paymentStatus: "Đã thanh toán");
        var service = new PaymentService(
            new FakePayOsClient(),
            new FakeOrderRepository(order),
            new FakeOrderStatusRepository(),
            new FakeNotificationRepository(),
            new ConfigurationBuilder().Build());

        var status = await service.GetPaymentStatusAsync(order.OrderId, userId: 7);

        Assert.NotNull(status);
        Assert.True(status.IsPaid);
        Assert.Equal("Đã thanh toán", status.PaymentStatus);
    }

    [Fact]
    public async Task MessageService_GetMessages_ReturnsNull_WhenConversationDoesNotBelongToUser()
    {
        var conversation = new Conversation { ConversationId = 12, CustomerUserId = 7 };
        var service = new MessageService(
            new FakeConversationRepository(conversation),
            new FakeMessageRepository([
                new Message
                {
                    MessageId = 1,
                    ConversationId = conversation.ConversationId,
                    SenderUserId = 7,
                    Content = "hello"
                }
            ]));

        var messages = await service.GetMessagesAsync(conversation.ConversationId, requestingUserId: 99);

        Assert.Null(messages);
    }

    [Fact]
    public async Task MessageService_GetMessages_ReturnsMessages_WhenConversationBelongsToUser()
    {
        var conversation = new Conversation { ConversationId = 12, CustomerUserId = 7 };
        var service = new MessageService(
            new FakeConversationRepository(conversation),
            new FakeMessageRepository([
                new Message
                {
                    MessageId = 1,
                    ConversationId = conversation.ConversationId,
                    SenderUserId = 7,
                    Content = "hello"
                }
            ]));

        var messages = await service.GetMessagesAsync(conversation.ConversationId, requestingUserId: 7);

        Assert.NotNull(messages);
        var message = Assert.Single(messages);
        Assert.Equal("hello", message.Content);
        Assert.Equal("customer", message.Sender);
    }

    [Fact]
    public async Task MessageService_SendMessage_ReturnsNull_WhenConversationDoesNotBelongToSender()
    {
        var conversation = new Conversation { ConversationId = 12, CustomerUserId = 7 };
        var messages = new FakeMessageRepository([]);
        var service = new MessageService(
            new FakeConversationRepository(conversation),
            messages);

        var result = await service.SendMessageAsync(
            new CreateMessageDto(conversation.ConversationId, SenderUserId: 99, "hi"),
            requestingUserId: 99);

        Assert.Null(result);
        Assert.Empty(messages.AddedMessages);
    }

    [Fact]
    public async Task MessageService_SendMessage_AddsMessage_WhenConversationBelongsToSender()
    {
        var conversation = new Conversation { ConversationId = 12, CustomerUserId = 7 };
        var messages = new FakeMessageRepository([]);
        var service = new MessageService(
            new FakeConversationRepository(conversation),
            messages);

        var result = await service.SendMessageAsync(
            new CreateMessageDto(conversation.ConversationId, SenderUserId: 7, "hi"),
            requestingUserId: 7);

        Assert.NotNull(result);
        Assert.Single(messages.AddedMessages);
        Assert.Equal("hi", Assert.Single(result).Content);
    }

    [Fact]
    public async Task NotificationService_MarkRead_ReturnsFalse_WhenNotificationBelongsToAnotherUser()
    {
        var notification = new Notification
        {
            NotificationId = 5,
            UserId = 7,
            Title = "Order",
            Content = "Ready"
        };
        var service = new NotificationService(new FakeNotificationRepository(notification));

        var marked = await service.MarkNotificationReadAsync(notification.NotificationId, userId: 99);

        Assert.False(marked);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task NotificationService_MarkRead_MarksNotification_WhenItBelongsToUser()
    {
        var notification = new Notification
        {
            NotificationId = 5,
            UserId = 7,
            Title = "Order",
            Content = "Ready"
        };
        var service = new NotificationService(new FakeNotificationRepository(notification));

        var marked = await service.MarkNotificationReadAsync(notification.NotificationId, userId: 7);

        Assert.True(marked);
        Assert.True(notification.IsRead);
    }

    [Fact]
    public async Task ReservationsController_CreateReservation_RequiresASessionEvenWhenUserIdIsMissing()
    {
        var controller = new ReservationsController(
            new FakeReservationService(),
            new InMemorySessionTokenService(new MemoryCache(new MemoryCacheOptions()), new SessionTokenOptions()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.CreateReservation(
            new CreateReservationDto(
                UserId: null,
                DateOnly.FromDateTime(DateTime.UtcNow.Date),
                new TimeOnly(18, 0),
                "Lan",
                "0123456789",
                2,
                null,
                TableId: 3));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task OrderService_CreateOrder_RejectsQuantityAboveAvailableStock()
    {
        var product = new Product
        {
            ProductId = 10,
            Name = "Latte",
            Price = 50000m,
            UnitInStock = 1,
            IsAvailable = true
        };
        var products = new FakeProductRepository([product]);
        var orders = new FakeOrderRepository(null, products);
        var service = new OrderService(
            orders,
            products,
            new FakeNotificationRepository(),
            new FakeOrderStatusRepository(),
            new FakePaymentMethodRepository());

        var order = await service.CreateOrderAsync(new CreateOrderRequestDto(
            UserId: 7,
            TableId: null,
            ReservationId: null,
            OrderType: "Mang đi",
            Note: null,
            PaymentMethod: "Tiền mặt",
            Items: [new OrderItemRequestDto(product.ProductId, Quantity: 2)]));

        Assert.Null(order);
        Assert.Empty(orders.AddedOrders);
        Assert.Equal(1, product.UnitInStock);
    }

    [Fact]
    public async Task OrderService_CreateOrder_ReservesStock_WhenQuantityIsAvailable()
    {
        var product = new Product
        {
            ProductId = 10,
            Name = "Latte",
            Price = 50000m,
            UnitInStock = 3,
            IsAvailable = true
        };
        var products = new FakeProductRepository([product]);
        var orders = new FakeOrderRepository(null, products);
        var service = new OrderService(
            orders,
            products,
            new FakeNotificationRepository(),
            new FakeOrderStatusRepository(),
            new FakePaymentMethodRepository());

        var order = await service.CreateOrderAsync(new CreateOrderRequestDto(
            UserId: 7,
            TableId: null,
            ReservationId: null,
            OrderType: "Mang đi",
            Note: null,
            PaymentMethod: "Tiền mặt",
            Items: [new OrderItemRequestDto(product.ProductId, Quantity: 2)]));

        Assert.NotNull(order);
        Assert.Single(orders.AddedOrders);
        Assert.Equal(1, product.UnitInStock);
        Assert.Equal(100000m, order.TotalPrice);
    }

    private static Order SampleOrder(int customerUserId, string paymentStatus = "Đang chờ thanh toán")
    {
        var status = new OrderStatus { OrderStatusId = 1, OrderStatusName = "Đang chờ" };
        return new Order
        {
            OrderId = 42,
            CustomerUserId = customerUserId,
            TotalPrice = 50000m,
            OrderStatusId = status.OrderStatusId,
            OrderStatus = status,
            Payments =
            {
                new Payment
                {
                    PaymentId = 9,
                    PaymentAmount = 50000m,
                    PaymentStatus = paymentStatus,
                    TransactionCode = null
                }
            }
        };
    }

    private static (bool Allowed, UserSession? Session, ActionResult? Failure) RequireRole(
        string methodName,
        string roleName)
    {
        var sessions = new FakeSessionTokenService(
            new UserSession(7, roleName, DateTime.UtcNow.AddHours(1)));
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer test-token";
        var method = typeof(AuthController).Assembly
            .GetType("LoafNCatting.Api.Infrastructure.SessionAuthorization")!
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.NotNull(method);

        object?[] args = [context.Request, sessions, null, null];
        var allowed = (bool)method!.Invoke(null, args)!;
        return (allowed, args[2] as UserSession, args[3] as ActionResult);
    }

    private static void AssertForbidden(ActionResult? failure)
    {
        var objectResult = Assert.IsType<ObjectResult>(failure);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    private abstract class FakeRepository<T> : IGenericRepository<T> where T : class
    {
        public virtual Task<T?> GetByIdAsync(int id) => Task.FromResult<T?>(null);
        public virtual Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>([]);
        public virtual Task AddAsync(T entity) => Task.CompletedTask;
        public virtual void Update(T entity) { }
        public virtual void Delete(T entity) { }
        public virtual Task<int> SaveChangesAsync() => Task.FromResult(0);
        public virtual Task<IDbContextTransaction> BeginTransactionAsync() => Task.FromResult<IDbContextTransaction>(new FakeTransaction());
        public virtual Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel) => Task.FromResult<IDbContextTransaction>(new FakeTransaction());
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

    private sealed class FakeOrderRepository(Order? order = null, FakeProductRepository? products = null)
        : FakeRepository<Order>, IOrderRepository
    {
        public List<Order> AddedOrders { get; } = [];

        public override Task AddAsync(Order entity)
        {
            entity.OrderId = 100 + AddedOrders.Count;
            entity.OrderStatus ??= new OrderStatus { OrderStatusId = entity.OrderStatusId, OrderStatusName = "Đang chờ" };
            foreach (var detail in entity.OrderDetails)
            {
                detail.Product = products?.Products.First(product => product.ProductId == detail.ProductId)
                    ?? new Product { ProductId = detail.ProductId, Name = "Product", Price = detail.UnitPrice };
            }

            AddedOrders.Add(entity);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Order>> GetUserOrdersAsync(int userId) => Task.FromResult<IEnumerable<Order>>([]);

        public Task<IEnumerable<Order>> GetStaffOrdersAsync(int? statusId, DateOnly? date) =>
            Task.FromResult<IEnumerable<Order>>([]);

        public Task<Order?> GetByIdWithDetailsAsync(int orderId)
        {
            return Task.FromResult(order ?? AddedOrders.FirstOrDefault(item => item.OrderId == orderId));
        }

        public Task<Order?> GetLatestPendingPaymentOrderAsync(int userId) =>
            Task.FromResult<Order?>(null);
    }

    private sealed class FakeProductRepository(List<Product> products)
        : FakeRepository<Product>, IProductRepository
    {
        public List<Product> Products { get; } = products;

        public Task<IEnumerable<Product>> GetProductsAsync(int? categoryId, string? search) =>
            Task.FromResult<IEnumerable<Product>>(Products);

        public Task<Product?> GetByIdWithCategoryAsync(int id) =>
            Task.FromResult(Products.FirstOrDefault(product => product.ProductId == id));

        public Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var productIds = ids.ToHashSet();
            return Task.FromResult(Products.Where(product => productIds.Contains(product.ProductId)).ToList());
        }

        public Task<bool> TryReserveStockAsync(IReadOnlyDictionary<int, int> quantitiesByProductId)
        {
            if (quantitiesByProductId.Any(item =>
                Products.FirstOrDefault(product => product.ProductId == item.Key) is not { IsAvailable: true } product ||
                product.UnitInStock < item.Value))
            {
                return Task.FromResult(false);
            }

            foreach (var item in quantitiesByProductId)
            {
                var product = Products.First(product => product.ProductId == item.Key);
                product.UnitInStock -= item.Value;
                product.IsAvailable = product.UnitInStock > 0 && product.IsAvailable;
            }

            return Task.FromResult(true);
        }
    }

    private sealed class FakeNotificationRepository(Notification? notification = null)
        : FakeRepository<Notification>, INotificationRepository, INotificationWriter
    {
        public override Task<Notification?> GetByIdAsync(int id) =>
            Task.FromResult(notification?.NotificationId == id ? notification : null);

        public Task<IEnumerable<Notification>> GetByUserIdAsync(int userId) =>
            Task.FromResult<IEnumerable<Notification>>(
                notification?.UserId == userId ? [notification] : []);

        public Task<NotificationDto?> CreateAsync(int? userId, string title, string content, string type)
        {
            return Task.FromResult<NotificationDto?>(userId.HasValue
                ? new NotificationDto(1, userId, title, content, type, false, DateTime.UtcNow)
                : null);
        }
    }

    private sealed class FakePayOsClient : IPayOsClient
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
                0,
                50000,
                "PENDING",
                DateTime.UtcNow.ToString("O"),
                [],
                null,
                null));
        }
    }

    private sealed class FakeConversationRepository(Conversation conversation)
        : FakeRepository<Conversation>, IConversationRepository
    {
        public override Task<Conversation?> GetByIdAsync(int id) =>
            Task.FromResult(conversation.ConversationId == id ? conversation : null);

        public Task<Conversation?> GetByCustomerUserIdAsync(int userId) =>
            Task.FromResult(conversation.CustomerUserId == userId ? conversation : null);

        public Task<IEnumerable<Conversation>> GetInboxAsync() =>
            Task.FromResult<IEnumerable<Conversation>>([conversation]);
    }

    private sealed class FakeMessageRepository(IEnumerable<Message> messages)
        : FakeRepository<Message>, IMessageRepository
    {
        private readonly List<Message> _messages = messages.ToList();
        public List<Message> AddedMessages { get; } = [];

        public override Task AddAsync(Message entity)
        {
            AddedMessages.Add(entity);
            _messages.Add(entity);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Message>> GetByConversationIdAsync(int conversationId) =>
            Task.FromResult<IEnumerable<Message>>(_messages.Where(message => message.ConversationId == conversationId));

        public Task<IEnumerable<Message>> GetByConversationIdForSupportAsync(int conversationId) =>
            GetByConversationIdAsync(conversationId);
    }

    private sealed class FakeUserRepository : FakeRepository<User>, IUserRepository
    {
        public Task<IEnumerable<User>> GetAdminUsersAsync(
            int? roleId,
            string? search,
            bool? active) => Task.FromResult<IEnumerable<User>>([]);

        public Task<User?> GetByIdWithRoleAsync(int id) => Task.FromResult<User?>(null);

        public Task<bool> ExistsByEmailOrPhoneAsync(string email, string phoneNumber) => Task.FromResult(false);
        public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);
        public Task<User?> GetByLoginAsync(string email, string phoneNumber) => Task.FromResult<User?>(null);
        public Task<User?> GetFirstStaffAsync() => Task.FromResult<User?>(null);
    }

    private sealed class FakeOrderStatusRepository : FakeRepository<OrderStatus>, IOrderStatusRepository
    {
        public Task<OrderStatus> GetByNameAsync(string name) =>
            Task.FromResult(new OrderStatus { OrderStatusId = 1, OrderStatusName = name });
    }

    private sealed class FakePaymentMethodRepository : FakeRepository<PaymentMethod>, IPaymentMethodRepository
    {
        public Task<PaymentMethod> GetByNameOrDefaultAsync(string name) =>
            Task.FromResult(new PaymentMethod { MethodId = 1, MethodName = name });
    }

    private sealed class FakeReservationService : IReservationService
    {
        public Task<ReservationDto?> CreateReservationAsync(CreateReservationDto request) =>
            Task.FromResult<ReservationDto?>(new ReservationDto(
                1,
                request.UserId,
                request.Date,
                request.Time,
                request.GuestName,
                request.GuestPhoneNumber,
                request.NumberOfGuests,
                request.Note,
                "Đang chờ",
                request.TableId ?? 3,
                "Window 1"));

        public Task<List<ReservationDto>> GetUserReservationsAsync(int userId) => Task.FromResult<List<ReservationDto>>([]);

        public Task<List<ReservationDto>> GetStaffReservationsAsync(
            int? statusId,
            DateOnly? date) => Task.FromResult<List<ReservationDto>>([]);

        public Task<ReservationDto?> UpdateReservationStatusAsync(
            int id,
            StaffReservationStatusDto request) => Task.FromResult<ReservationDto?>(null);
    }

    private sealed class FakeSessionTokenService(UserSession? session) : ISessionTokenService
    {
        public string IssueToken(User user) => "test-token";

        public UserSession? GetSession(string token) => token == "test-token" ? session : null;

        public void Revoke(string token) { }
    }
}
