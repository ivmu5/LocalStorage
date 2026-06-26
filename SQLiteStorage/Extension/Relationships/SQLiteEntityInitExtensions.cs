using System.Linq.Expressions;
using System.Reflection;

namespace SQLiteStorage;

public static class EntityInitExtensions
{
    public static async Task EnsureCreatedAsync<TEntity, TValue>(
        this TEntity entity,
        Expression<Func<TEntity, TValue?>> selector,
        Func<TValue> factory)
        where TEntity : BaseEntity, new()
        where TValue : class
    {
        if (selector.Body is not MemberExpression memberExpr)
            throw new InvalidOperationException("Selector must be property access");

        if (memberExpr.Member is not PropertyInfo prop)
            throw new InvalidOperationException("Selector must be property");

        var currentValue = (TValue?)prop.GetValue(entity);

        if (currentValue != null)
            return;

        var newValue = factory();

        prop.SetValue(entity, newValue);

        await entity.SaveCascadeAsync();
    }

    public static async Task EnsureCreatedAsync<TEntity, TValue>(
        this TEntity entity,
        Expression<Func<TEntity, TValue?>> selector)
        where TEntity : BaseEntity, new()
        where TValue : class, new()
    {
        await entity.EnsureCreatedAsync(selector, () => new TValue());
    }
}
