using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OnlySplit.Infrastructure.Database;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.Infrastructure.Repositories;

public sealed class EfRepository<TEntity>(OnlySplitDbContext context) : IRepository<TEntity>
    where TEntity : class
{
    public IQueryable<TEntity> Query() => context.Set<TEntity>().AsQueryable();

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Set<TEntity>().FindAsync([id], cancellationToken).AsTask();

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        context.Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        context.Set<TEntity>().AddAsync(entity, cancellationToken).AsTask();

    public void Update(TEntity entity) => context.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => context.Set<TEntity>().Remove(entity);
}
