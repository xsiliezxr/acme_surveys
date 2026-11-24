using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;

namespace AcmeSurveys.Infrastructure.Persistence
{
    public sealed class SqlConnectionFactory
    {
        private readonly string _connectionString;

        static SqlConnectionFactory()
        {
            SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2);
            SqlMapper.AddTypeMap(typeof(DateTime?), DbType.DateTime2);
        }

        public SqlConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A connection string is required.", nameof(connectionString));
            _connectionString = connectionString;
        }

        public async Task<SqlConnection> OpenAsync()
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }
    }
}
