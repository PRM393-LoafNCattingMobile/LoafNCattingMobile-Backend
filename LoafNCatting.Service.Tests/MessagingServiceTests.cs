using System.Data;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using LoafNCatting.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
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
            new FakeMessageRepository(
            [
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
            ]),
            new FakeUserRepository());

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
            new FakeMessageRepository(
            [
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
            ]),
            new FakeUserRepository());

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
            messages,
            new FakeUserRepository(new User
            {
                UserId = 21,
                Name = "Support",
                Email = "support@example.com",
                Password = "pw",
                PhoneNumber = "0999999999",
                Role = new Role { RoleId = 2, RoleName = "Staff" }
            }));

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
    public async Task SendSupportMessageAsync_AddsStaffMessage_AndPreservesSenderUserId()
    {
        var conversation = new Conversation { ConversationId = 22, CustomerUserId = 7 };
        var messages = new FakeMessageRepository([]);
        IMessageService service = new MessageService(
            new FakeConversationRepository([conversation]),
            messages,
            new FakeUserRepository());

        var result = await service.SendSupportMessageAsync(
            new SupportMessageDto(conversation.ConversationId, "We're on it."),
            staffUserId: 21);

        Assert.NotNull(result);
        var added = Assert.Single(messages.AddedMessages);
        Assert.Equal(21, added.SenderUserId);
        Assert.Equal("We're on it.", added.Content);
        Assert.Equal("store", Assert.Single(result!).Sender);
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

    private sealed class FakeConversationRepository(IEnumerable<Conversation> conversations)
        : FakeRepository<Conversation>, IConversationRepository
    {
        private readonly List<Conversation> _conversations = conversations.ToList();

        public override Task<Conversation?> GetByIdAsync(int id) =>
            Task.FromResult(_conversations.FirstOrDefault(conversation => conversation.ConversationId == id));

        public Task<Conversation?> GetByCustomerUserIdAsync(int userId) =>
            Task.FromResult(_conversations.FirstOrDefault(conversation => conversation.CustomerUserId == userId));

        public Task<IEnumerable<Conversation>> GetInboxAsync() =>
            Task.FromResult<IEnumerable<Conversation>>(_conversations);
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

    private sealed class FakeUserRepository(User? firstStaff = null) : FakeRepository<User>, IUserRepository
    {
        public Task<IEnumerable<User>> GetAdminUsersAsync(int? roleId, string? search, bool? active) =>
            Task.FromResult<IEnumerable<User>>([]);

        public Task<User?> GetByIdWithRoleAsync(int id) => Task.FromResult<User?>(null);
        public Task<bool> ExistsByEmailOrPhoneAsync(string email, string phoneNumber) => Task.FromResult(false);
        public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);
        public Task<User?> GetByLoginAsync(string email, string phoneNumber) => Task.FromResult<User?>(null);
        public Task<User?> GetFirstStaffAsync() => Task.FromResult(firstStaff);
    }
}
