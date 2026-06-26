using SQLite;

namespace SQLiteStorage;

[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : IgnoreAttribute, IRelationship
{
    public string ForeignKeyName { get; }
    public bool IsNullable { get; }

    public ForeignKeyAttribute(string foreignKeyName, bool isNullable = false)
    {
        ForeignKeyName = foreignKeyName;
        IsNullable = isNullable;
    }
}