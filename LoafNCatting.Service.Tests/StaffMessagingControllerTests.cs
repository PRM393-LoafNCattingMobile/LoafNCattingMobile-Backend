using LoafNCatting.Api.Controllers;
using LoafNCatting.Api.Hubs;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Reflection;

namespace LoafNCatting.Service.Tests;

public class StaffMessagingControllerTests
{
    [Fact]
    public void StaffConversations_SendMessage_UsesTask7PostRoute()
    {
        var controllerRoute = typeof(StaffConversationsController)
            .GetCustomAttribute<RouteAttribute>();
        var actionRoute = typeof(StaffConversationsController)
            .GetMethod(nameof(StaffConversationsController.SendMessage))!
            .GetCustomAttribute<HttpPostAttribute>();

        Assert.Equal("api/staff/conversations", controllerRoute?.Template);
        Assert.Equal("{conversationId:int}/messages", actionRoute?.Template);
    }

    [Fact]
    public async Task StaffConversations_SendMessage_UsesRouteConversationId_AndActingSessionUser()
    {
        var service = new FakeMessageService();
        var hub = new FakeHubContext();
        var controller = CreateController("Staff", service, hub, userId: 77);

        var result = await controller.SendMessage(
            conversationId: 22,
            new SupportMessageDto("We're on it."));

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(77, service.LastStaffUserId);
        Assert.Equal(22, service.LastConversationId);
        Assert.Equal("We're on it.", service.LastSupportRequest?.Content);
        Assert.Equal(
            [SupportChatHub.ConversationGroup(22), SupportChatHub.StaffInboxGroup],
            hub.SentMessages.Select(message => message.GroupName).ToArray());
    }

    private static StaffConversationsController CreateController(
        string roleName,
        IMessageService messageService,
        FakeHubContext hub,
        int userId = 7)
    {
        var controller = new StaffConversationsController(
            new FakeConversationService(),
            messageService,
            new FakeSessionTokenService(
                new UserSession(userId, roleName, DateTime.UtcNow.AddHours(1))),
            hub)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private sealed class FakeConversationService : IConversationService
    {
        public Task<ConversationDto> GetOrCreateConversationAsync(int userId) =>
            Task.FromResult(new ConversationDto(1, userId, DateTime.UtcNow));

        public Task<List<ConversationInboxItemDto>> GetInboxAsync() =>
            Task.FromResult<List<ConversationInboxItemDto>>([]);
    }

    private sealed class FakeMessageService : IMessageService
    {
        public SupportMessageDto? LastSupportRequest { get; private set; }
        public int? LastConversationId { get; private set; }
        public int? LastStaffUserId { get; private set; }

        public Task<List<MessageDto>?> GetMessagesAsync(int conversationId, int requestingUserId) =>
            Task.FromResult<List<MessageDto>?>([]);

        public Task<List<MessageDto>?> GetMessagesForSupportAsync(int conversationId) =>
            Task.FromResult<List<MessageDto>?>([]);

        public Task<List<MessageDto>?> SendMessageAsync(CreateMessageDto request, int requestingUserId) =>
            Task.FromResult<List<MessageDto>?>([]);

        public Task<List<MessageDto>?> SendSupportMessageAsync(
            int conversationId,
            SupportMessageDto request,
            int staffUserId)
        {
            LastConversationId = conversationId;
            LastSupportRequest = request;
            LastStaffUserId = staffUserId;
            return Task.FromResult<List<MessageDto>?>([
                new MessageDto(
                    MessageId: 1,
                    ConversationId: conversationId,
                    SenderUserId: staffUserId,
                    Sender: "store",
                    Content: request.Content,
                    IsRead: false,
                    SentAt: DateTime.UtcNow)
            ]);
        }
    }

    private sealed class FakeSessionTokenService(UserSession? session) : ISessionTokenService
    {
        public string IssueToken(LoafNCatting.Data.Models.User user) => "test-token";

        public UserSession? GetSession(string token) =>
            token == "test-token" ? session : null;

        public void Revoke(string token) { }
    }

    private sealed class FakeHubContext : IHubContext<SupportChatHub>
    {
        private readonly FakeHubClients _clients = new();

        public List<SentHubMessage> SentMessages => _clients.SentMessages;
        public IHubClients Clients => _clients;
        public IGroupManager Groups { get; } = new FakeGroupManager();
    }

    private sealed class FakeHubClients : IHubClients
    {
        public List<SentHubMessage> SentMessages { get; } = [];

        public IClientProxy All => new FakeClientProxy("all", SentMessages);
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => new FakeClientProxy("all-except", SentMessages);
        public IClientProxy Client(string connectionId) => new FakeClientProxy($"client:{connectionId}", SentMessages);
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => new FakeClientProxy("clients", SentMessages);
        public IClientProxy Group(string groupName) => new FakeClientProxy(groupName, SentMessages);
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => new FakeClientProxy(groupName, SentMessages);
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => new FakeClientProxy("groups", SentMessages);
        public IClientProxy User(string userId) => new FakeClientProxy($"user:{userId}", SentMessages);
        public IClientProxy Users(IReadOnlyList<string> userIds) => new FakeClientProxy("users", SentMessages);
    }

    private sealed class FakeClientProxy(string groupName, List<SentHubMessage> sentMessages) : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            sentMessages.Add(new SentHubMessage(groupName, method, args));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed record SentHubMessage(string GroupName, string Method, object?[] Args);
}
