using LoafNCatting.Api.Controllers;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Service.Tests;

public class StaffOrderReservationControllerTests
{
    [Fact]
    public async Task StaffOrders_GetOrders_ReturnsForbidden_ForCustomer()
    {
        var controller = CreateOrdersController("Customer", new FakeOrderService());

        var result = await controller.GetOrders(status: null, date: null);

        var failure = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, failure.StatusCode);
    }

    [Fact]
    public async Task StaffOrders_UpdateStatus_UsesActingSessionUserId()
    {
        var service = new FakeOrderService();
        var controller = CreateOrdersController("Staff", service, userId: 77);

        var result = await controller.UpdateStatus(
            id: 10,
            new StaffOrderStatusDto(StatusId: 2));

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(77, service.LastActingUserId);
    }

    [Fact]
    public async Task StaffReservations_GetReservations_AllowsAdmin()
    {
        var controller = CreateReservationsController(
            "Admin",
            new FakeReservationService());

        var result = await controller.GetReservations(status: 1, date: null);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task StaffReservations_UpdateStatus_ReturnsForbidden_ForCustomer()
    {
        var controller = CreateReservationsController(
            "Customer",
            new FakeReservationService());

        var result = await controller.UpdateStatus(
            id: 20,
            new StaffReservationStatusDto(StatusId: 2));

        var failure = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, failure.StatusCode);
    }

    private static StaffOrdersController CreateOrdersController(
        string roleName,
        IOrderService service,
        int userId = 7)
    {
        var controller = new StaffOrdersController(
            service,
            new FakeSessionTokenService(
                new UserSession(userId, roleName, DateTime.UtcNow.AddHours(1))))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private static StaffReservationsController CreateReservationsController(
        string roleName,
        IReservationService service)
    {
        var controller = new StaffReservationsController(
            service,
            new FakeSessionTokenService(
                new UserSession(7, roleName, DateTime.UtcNow.AddHours(1))))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private sealed class FakeOrderService : IOrderService
    {
        public int? LastActingUserId { get; private set; }

        public Task<OrderDto?> CreateOrderAsync(CreateOrderRequestDto request) =>
            Task.FromResult<OrderDto?>(null);

        public Task<List<OrderDto>> GetUserOrdersAsync(int userId) =>
            Task.FromResult<List<OrderDto>>([]);

        public Task<List<OrderDto>> GetStaffOrdersAsync(int? statusId, DateOnly? date) =>
            Task.FromResult<List<OrderDto>>([]);

        public Task<OrderDto?> UpdateOrderStatusAsync(
            int id,
            int actingUserId,
            StaffOrderStatusDto request)
        {
            LastActingUserId = actingUserId;
            return Task.FromResult<OrderDto?>(new OrderDto(
                id,
                DateTime.UtcNow,
                45000m,
                CustomerUserId: 5,
                StatusName: "Đang chuẩn bị",
                PaymentStatus: "Đã thanh toán",
                Items: [],
                CustomerName: "Customer"));
        }
    }

    private sealed class FakeReservationService : IReservationService
    {
        public Task<ReservationDto?> CreateReservationAsync(CreateReservationDto request) =>
            Task.FromResult<ReservationDto?>(null);

        public Task<List<ReservationDto>> GetUserReservationsAsync(int userId) =>
            Task.FromResult<List<ReservationDto>>([]);

        public Task<List<ReservationDto>> GetStaffReservationsAsync(
            int? statusId,
            DateOnly? date) => Task.FromResult<List<ReservationDto>>([]);

        public Task<ReservationDto?> UpdateReservationStatusAsync(
            int id,
            StaffReservationStatusDto request) =>
            Task.FromResult<ReservationDto?>(new ReservationDto(
                id,
                UserId: 5,
                Date: new DateOnly(2026, 6, 30),
                Time: new TimeOnly(18, 30),
                GuestName: "Customer",
                GuestPhoneNumber: "0900000000",
                NumberOfGuests: 2,
                Note: null,
                StatusName: "Đã xác nhận",
                TableId: 3,
                TableName: "A3"));
    }

    private sealed class FakeSessionTokenService(UserSession? session) : ISessionTokenService
    {
        public string IssueToken(User user) => "test-token";

        public UserSession? GetSession(string token) =>
            token == "test-token" ? session : null;

        public void Revoke(string token) { }
    }
}
