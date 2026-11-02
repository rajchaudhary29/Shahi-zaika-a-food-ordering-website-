using Microsoft.EntityFrameworkCore;
using ShahiZayka.Domain.Entities;

namespace ShahiZayka.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<User> Users { get; }
    DbSet<Review> Reviews { get; }
    DbSet<NewsletterSubscriber> NewsletterSubscribers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
