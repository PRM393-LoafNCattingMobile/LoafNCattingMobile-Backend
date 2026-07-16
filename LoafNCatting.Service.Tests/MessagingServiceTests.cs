using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using LoafNCatting.Service.Interfaces;
using Xunit;

namespace LoafNCatting.Service.Tests;

public class MessagingServiceTests
{
    [Fact]
    public async Task GetInboxAsync_ReturnsConversationSummaries_ForStaffOrAdmin()
    {
        var sentAt = new DateTime(2026, 7, 1, 9, 30, 0, DateTimeKind.Utc);
        var conversations = new[]
        {
            new Conversation
            {
                ConversationId = 11,
                CustomerUserId = 7,
                CreatedAt = sentAt.AddHours(-2),
                UpdatedAt = sentAt,
                CustomerUser = new User
                {
                    UserId = 7,
                    Name = "Lan",
                    Email = "lan@example.com",
                    Password = "pw",
                    PhoneNumber = "0123456789",
                    Role = new Role { RoleId = 3, RoleName = "Customer" }
                },
                Messages =
                [
                    new Message
                    {
                        MessageId = 1,
                        ConversationId = 11,
                        SenderUserId = 7,
                        Content = "Need help with my order",
                        IsRead = false,
                        SentAt = sentAt
                    }
                ]
            }
        };
        IConversationService service = new ConversationService(new FakeConversationRepository(conversations));

        var inbox = await service.GetInboxAsync();

        var item = Assert.Single(inbox);
        Assert.Equal(11, item.ConversationId);
        Assert.Equal(7, item.CustomerUserId);
        Assert.Equal("Lan", item.CustomerName);
        Assert.Equal("Need help with my order", item.LastMessage);
        Assert.Equal("customer", item.LastMessageSender);
        Assert.Equal(sentAt, item.LastMessageSentAt);
        Assert.Equal(1, item.UnreadCount);
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsMessages_ForConversationOwner()
    {
        var conversation = new Conversation { ConversationId = 12, CustomerUserId = 7 };
        var service = new MessageService(
            new FakeConversationRepository([conversation]),
            new FakeMessageRepository([
                new Message
                {
                    MessageId = 1,
                    ConversationId = 12,
                    SenderUserId = 7,
                    Content = "hello",
                    SentAt = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)
                },
                new Message
                {
                    MessageId = 2,
                    ConversationId = 12,
                    SenderUserId = 21,
                    Content = "hi there",
                    SentAt = new DateTime(2026, 7, 1, 10, 1, 0, DateTimeKind.Utc)
                }
            ]));

        var messages = await service.GetMessagesAsync(conversation.ConversationId, requestingUserId: 7);

        Assert.NotNull(messages);
        Assert.Collection(
            messages!,
            message =>
            {
                Assert.Equal("hello", message.Content);
                Assert.Equal("customer", message.Sender);
            },
            message =>
            {
                Assert.Equal("hi there", message.Content);
                Assert.Equal("store", message.Sender);
            });
    }

    [Fact]
    public async Task GetMessagesForSupportAsync_ReturnsMessages_ForStaffOrAdmin()
    {
        var conversation = new Conversation { ConversationId = 15, CustomerUserId = 7 };
        IMessageService service = new MessageService(
            new FakeConversationRepository([conversation]),
            new FakeMessageRepository([
                new Message
                {
                    MessageId = 1,
                    ConversationId = 15,
                    SenderUserId = 7,
                    Content = "Is table 2 available?"
                },
                new Message
                {
                    MessageId = 2,
                    ConversationId = 15,
                    SenderUserId = 21,
                    Content = "Yes, it is."
                }
            ]));

        var messages = await service.GetMessagesForSupportAsync(conversation.ConversationId);

        Assert.NotNull(messages);
        Assert.Equal(2, messages!.Count);
        Assert.Equal(["customer", "store"], messages.Select(message => message.Sender).ToArray());
    }

    [Fact]
    public async Task SendCustomerMessageAsync_AddsOnlyCustomerMessage_WithoutAutoReply()
    {
        var conversation = new Conversation { ConversationId = 18, CustomerUserId = 7 };
        var messages = new FakeMessageRepository([]);
        var service = new MessageService(
            new FakeConversationRepository([conversation]),
            messages);

        var result = await service.SendMessageAsync(
            new CreateMessageDto(conversation.ConversationId, SenderUserId: 7, " Need help "),
            requestingUserId: 7);

        Assert.NotNull(result);
        var added = Assert.Single(messages.AddedMessages);
        Assert.Equal(7, added.SenderUserId);
        Assert.Equal("Need help", added.Content);
        Assert.Single(result!);
        Assert.Equal("Need help", result[0].Content);
    }

    [Fact]
    public async Task SendCustomerMessageAsync_CreatesChatNotifications_ForSupportUsers()
    {
        var conversation = new Conversation { ConversationId = 18, CustomerUserId = 7 };
        var notifications = new FakeNotificationWriter();
        var service = new MessageService(
            new FakeConversationRepository([conversation]),
            new FakeMessageRepository([]),
            notifications,
            new FakeUserRepository(users:
            [
                SupportUser(21, "Staff"),
                SupportUser(22, "Admin"),
                SupportUser(7, "Customer")
            ]));

        await service.SendMessageAsync(
            new CreateMessageDto(conversation.ConversationId, SenderUserId: 7, " Need help "),
            requestingUserId: 7);

        Assert.Equal([21, 22], notifications.Items.Select(item => item.UserId).ToArray());
        Assert.All(notifications.Items, item => Assert.Equal("chat", item.Type));
    }

    [Fact]
    public async Task SendSupportMessageAsync_AddsStaffMessage_AndPreservesSenderUserId()
    {
        var conversation = new Conversation { ConversationId = 22, CustomerUserId = 7 };
        var messages = new FakeMessageRepository([]);
        IMessageService service = new MessageService(
            new FakeConversationRepository([conversation]),
            messages);

        var result = await service.SendSupportMessageAsync(
            conversation.ConversationId,
            new SupportMessageDto("We're on it."),
            staffUserId: 21);

        Assert.NotNull(result);
        var added = Assert.Single(messages.AddedMessages);
        Assert.Equal(21, added.SenderUserId);
        Assert.Equal("We're on it.", added.Content);
        Assert.Equal("store", Assert.Single(result!).Sender);
    }

    [Fact]
    public async Task SendSupportMessageAsync_CreatesChatNotification_ForCustomer()
    {
        var conversation = new Conversation { ConversationId = 22, CustomerUserId = 7 };
        var notifications = new FakeNotificationWriter();
        IMessageService service = new MessageService(
            new FakeConversationRepository([conversation]),
            new FakeMessageRepository([]),
            notifications);

        await service.SendSupportMessageAsync(
            conversation.ConversationId,
            new SupportMessageDto("We're on it."),
            staffUserId: 21);

        var notification = Assert.Single(notifications.Items);
        Assert.Equal(7, notification.UserId);
        Assert.Equal("chat", notification.Type);
    }

    private static User SupportUser(int userId, string roleName) => new()
    {
        UserId = userId,
        Name = $"User {userId}",
        Email = $"user{userId}@example.com",
        Password = "pw",
        PhoneNumber = $"090000{userId}",
        Role = new Role { RoleId = userId, RoleName = roleName }
    };
}
