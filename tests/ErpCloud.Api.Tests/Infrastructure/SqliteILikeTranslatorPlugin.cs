using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ErpCloud.Api.Tests.Infrastructure;

/// <summary>
/// Custom method call translator provider for SQLite that adds ILike support.
/// </summary>
#pragma warning disable EF1001 // Internal EF Core API usage
public class CustomSqliteMethodCallTranslatorProvider : IMethodCallTranslatorProvider
{
    private readonly SqliteMethodCallTranslatorProvider _baseProvider;
    private readonly List<IMethodCallTranslatorPlugin> _plugins;

    public CustomSqliteMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies)
    {
        _baseProvider = new SqliteMethodCallTranslatorProvider(dependencies);
        _plugins = new List<IMethodCallTranslatorPlugin>
        {
            new SqliteILikeTranslatorPlugin(dependencies.SqlExpressionFactory)
        };
    }

    public IEnumerable<IMethodCallTranslatorPlugin> Plugins => _plugins;

    public SqlExpression? Translate(
        IModel model,
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        // Try our custom translators first
        foreach (var plugin in _plugins)
        {
            foreach (var translator in plugin.Translators)
            {
                var result = translator.Translate(instance, method, arguments, logger);
                if (result != null)
                    return result;
            }
        }

        // Fall back to base SQLite translators
        return _baseProvider.Translate(model, instance, method, arguments, logger);
    }
}
#pragma warning restore EF1001

/// <summary>
/// Translates Npgsql ILike calls to SQLite LIKE for test compatibility.
/// SQLite LIKE is case-insensitive by default for ASCII characters.
/// </summary>
public class SqliteILikeTranslatorPlugin : IMethodCallTranslatorPlugin
{
    public SqliteILikeTranslatorPlugin(ISqlExpressionFactory sqlExpressionFactory)
    {
        Translators = new IMethodCallTranslator[]
        {
            new SqliteILikeTranslator(sqlExpressionFactory)
        };
    }

    public virtual IEnumerable<IMethodCallTranslator> Translators { get; }
}

/// <summary>
/// Translates ILike method calls to LIKE for SQLite.
/// </summary>
public class SqliteILikeTranslator : IMethodCallTranslator
{
    private static readonly MethodInfo _iLikeMethod = 
        typeof(Microsoft.EntityFrameworkCore.NpgsqlDbFunctionsExtensions)
            .GetMethod(
                nameof(Microsoft.EntityFrameworkCore.NpgsqlDbFunctionsExtensions.ILike),
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(DbFunctions), typeof(string), typeof(string) },
                null)!;

    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    public SqliteILikeTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
    }

    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.Equals(_iLikeMethod))
        {
            // Translate ILike(match, pattern) to match LIKE pattern
            // SQLite LIKE is case-insensitive by default
            return _sqlExpressionFactory.Like(
                arguments[1],  // matchExpression (first string arg)
                arguments[2]); // pattern (second string arg)
        }

        return null;
    }
}
