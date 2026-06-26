namespace SQLiteStorage;

public static class SQLiteDataConvertExtensions
{
    public static object NormalizeValueToSQLite(this object value, Type type)
    {
        if (value == null)
            return null!;

        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (t == typeof(bool))
            return (bool)value ? 1 : 0;

        if (t.IsEnum)
            return Convert.ToInt32(value);

        return Convert.ChangeType(value, t);
    }
}
