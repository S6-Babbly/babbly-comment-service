using Cassandra;
using Cassandra.Mapping;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace babbly_comment_service.Data
{
    public abstract class CassandraRepository<T> : ICassandraRepository<T> where T : class
    {
        protected readonly CassandraContext _context;
        protected readonly IMapper _mapper;
        protected readonly ILogger<CassandraRepository<T>> _logger;
        protected readonly string _tableName;

        protected CassandraRepository(
            CassandraContext context, 
            ILogger<CassandraRepository<T>> logger,
            string tableName)
        {
            _context = context;
            _mapper = context.Mapper;
            _logger = logger;
            _tableName = tableName;
        }

        public virtual async Task<T> GetByIdAsync(Guid id)
        {
            try
            {
                var result = await _mapper.FirstOrDefaultAsync<T>("WHERE id = ?", id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving entity with ID {id} from {_tableName}");
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                return await _mapper.FetchAsync<T>($"SELECT * FROM {_tableName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving all entities from {_tableName}");
                throw;
            }
        }

        public virtual async Task<T> CreateAsync(T entity)
        {
            try
            {
                await _mapper.InsertAsync(entity);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating entity in {_tableName}");
                throw;
            }
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            try
            {
                await _mapper.UpdateAsync(entity);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity in {_tableName}");
                throw;
            }
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            try
            {
                await _mapper.DeleteAsync<T>("WHERE id = ?", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity with ID {id} from {_tableName}");
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> QueryAsync(string cql, params object[] parameters)
        {
            try
            {
                return await _mapper.FetchAsync<T>(cql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing query on {_tableName}: {cql}");
                throw;
            }
        }

        public virtual async Task ExecuteAsync(string cql, params object[] parameters)
        {
            try
            {
                await _context.Session.ExecuteAsync(new SimpleStatement(cql, parameters));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing statement on {_tableName}: {cql}");
                throw;
            }
        }
    }
} 