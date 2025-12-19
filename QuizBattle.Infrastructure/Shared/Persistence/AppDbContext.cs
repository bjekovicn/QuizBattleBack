using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Exceptions;
using QuizBattle.Domain.Features.Auth;
using QuizBattle.Domain.Features.Friendships;
using QuizBattle.Domain.Features.Questions;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Infrastructure.Shared.Persistence
{
    public sealed class AppDbContext : DbContext, IUnitOfWork
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Friendship> Friendships => Set<Friendship>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrencyException("Concurrency exception occurred.", ex);
            }
        }
    }
}