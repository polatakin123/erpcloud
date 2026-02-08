using ErpCloud.Api.Data;
using ErpCloud.Api.Tests.Infrastructure;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace ErpCloud.Api.Tests;

/// <summary>
/// Test database factory using SQLite in-memory for reliable constraint testing.
/// Each CreateContext() call creates a new isolated in-memory database.
/// </summary>
public class SqliteTestDbFactory : IDisposable
{
    private readonly List<(SqliteConnection Connection, TenantContext TenantContext)> _contexts = new();
    private bool _disposed;

    public (ErpDbContext Context, TenantContext TenantContext) CreateContext(Guid tenantId, Guid userId)
    {
        // Create a new connection for each context to ensure test isolation
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        // Enable foreign keys
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys=ON;";
            cmd.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlite(connection)
            .ReplaceService<IMethodCallTranslatorProvider, CustomSqliteMethodCallTranslatorProvider>()
            .Options;

        var tenantContext = new TenantContext
        {
            TenantId = tenantId,
            UserId = userId,
            IsBypassEnabled = false  // Explicitly disable bypass for test isolation
        };

        var context = new SqliteErpDbContext(options, tenantContext);
        
        // Create schema from model
        context.Database.EnsureCreated();

        // Track connection and tenant context for cleanup
        _contexts.Add((connection, tenantContext));
        
        return (context, tenantContext);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var (connection, _) in _contexts)
            {
                connection?.Dispose();
            }
            _contexts.Clear();
            _disposed = true;
        }
    }
}
