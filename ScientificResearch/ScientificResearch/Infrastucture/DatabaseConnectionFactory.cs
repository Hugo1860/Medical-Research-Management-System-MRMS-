using Npgsql;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScientificResearch.Infrastucture
{
    public interface IDatabaseConnectionFactory
    {
        string Create(string databaseKey, string environmentName);
    }

    /// <summary>
    /// Builds PostgreSQL connections from configuration without accepting connection-string fragments from clients.
    /// </summary>
    public sealed class DatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly IConfiguration configuration;

        public DatabaseConnectionFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string Create(string databaseKey, string environmentName)
        {
            if (string.IsNullOrWhiteSpace(databaseKey) || !Regex.IsMatch(databaseKey, "^[A-Za-z0-9_-]+$"))
            {
                throw new ArgumentException("数据库标识无效", nameof(databaseKey));
            }

            var allowedKeys = configuration.GetSection("allowedDatabaseKeys")
                .GetChildren()
                .Select(item => item.Value)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();
            if (allowedKeys.Length == 0 || !allowedKeys.Contains(databaseKey, StringComparer.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("数据库标识未被允许");
            }

            var configurationKey = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
                ? "connectionString:ScientificResearch_Development"
                : "connectionString:ScientificResearch_Release";
            var template = configuration.GetValue<string>(configurationKey);
            if (string.IsNullOrWhiteSpace(template) || !template.Contains("{0}", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"未配置有效的数据库连接模板: {configurationKey}");
            }

            var connectionString = template.Replace("{0}", databaseKey, StringComparison.Ordinal);
            return new NpgsqlConnectionStringBuilder(connectionString).ConnectionString;
        }
    }
}
