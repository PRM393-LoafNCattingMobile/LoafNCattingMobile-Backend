using LoafNCatting.Api.Controllers;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Service.Tests;

public class LookupsControllerTests
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("Staff")]
    public async Task GetAdminLookups_ReturnsLookups_ForAdminOrStaff(string roleName)
    {
        var controller = CreateController(roleName);

        var result = await controller.GetAdminLookups();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var lookups = Assert.IsType<AdminLookupsDto>(ok.Value);
        Assert.Equal("Admin", lookups.Roles.Single().Name);
    }

    [Fact]
    public async Task GetAdminLookups_ReturnsForbidden_ForCustomer()
    {
        var controller = CreateController("Customer");

        var result = await controller.GetAdminLookups();

        var failure = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, failure.StatusCode);
    }

    private static LookupsController CreateController(string roleName)
    {
        var controller = new LookupsController(
            new FakeLookupService(),
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

    private sealed class FakeLookupService : ILookupService
    {
        public Task<AdminLookupsDto> GetAdminLookupsAsync()
        {
            return Task.FromResult(new AdminLookupsDto(
                [new LookupItemDto(1, "Admin", "Quản trị viên")],
                [],
                [],
                [],
                [],
                [],
                [],
                []));
        }
    }

    private sealed class FakeSessionTokenService(UserSession? session) : ISessionTokenService
    {
        public string IssueToken(User user) => "test-token";

        public UserSession? GetSession(string token) => token == "test-token" ? session : null;

        public void Revoke(string token) { }
    }
}
