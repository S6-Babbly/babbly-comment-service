using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace babbly_comment_service.Data
{
    public interface ICassandraRepository<T> where T : class
    {
        Task<T> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> CreateAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<T>> QueryAsync(string cql, params object[] parameters);
        Task ExecuteAsync(string cql, params object[] parameters);
    }
} 