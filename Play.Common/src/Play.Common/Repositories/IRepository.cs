using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Play.Common.Entities;

namespace Play.Common.Repositories
{
    public interface IRepository<T, TKey> where T : IEntity<TKey>
    {
        Task CreateAsync(T entity);
        Task<IReadOnlyCollection<T>> GetAllAsync();
        Task<T> GetAsync(TKey id);
        Task UpdateAsync(T entity);
        Task RemoveAsync(TKey id);
        Task<IReadOnlyCollection<T>> GetAllAsync(Expression<Func<T, bool>> filter); // With expression, we can receive an expression and return data that matchs to the expression, so we don't have to use only Id as filter
        Task<T> GetAsync(Expression<Func<T, bool>> filter);
    }
}

