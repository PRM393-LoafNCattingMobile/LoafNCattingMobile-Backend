using System.Data;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoafNCatting.Service.Tests;

internal abstract class FakeRepository<T> : IGenericRepository<T> where T : class
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

internal sealed class FakeTransaction : IDbContextTransaction
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

internal sealed class FakeConversationRepository(IEnumerable<Conversation> conversations)
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

internal sealed class FakeMessageRepository(IEnumerable<Message> messages)
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

internal sealed class FakeUserRepository(User? firstStaff = null, IEnumerable<User>? users = null)
    : FakeRepository<User>, IUserRepository
{
    private readonly List<User> _users = users?.ToList() ??
        (firstStaff is null ? [] : [firstStaff]);

    public Task<IEnumerable<User>> GetAdminUsersAsync(int? roleId, string? search, bool? active) =>
        Task.FromResult<IEnumerable<User>>(_users);

    public Task<User?> GetByIdWithRoleAsync(int id) => Task.FromResult<User?>(null);
    public Task<bool> ExistsByEmailOrPhoneAsync(string email, string phoneNumber) => Task.FromResult(false);
    public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);
    public Task<User?> GetByLoginAsync(string email, string phoneNumber) => Task.FromResult<User?>(null);
    public Task<User?> GetFirstStaffAsync() => Task.FromResult(firstStaff);
}

internal sealed class FakeNotificationWriter : INotificationWriter
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
