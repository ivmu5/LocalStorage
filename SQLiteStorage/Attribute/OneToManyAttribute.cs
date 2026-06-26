using SQLite;

namespace SQLiteStorage;

[AttributeUsage(AttributeTargets.Property)]
public class OneToManyAttribute : IgnoreAttribute, IRelationship
{
    public string ForeignKeyName { get; }
    public bool IsNullable { get; }

    public OneToManyAttribute(string foreignKeyName, bool isNullable = false)
    {
        ForeignKeyName = foreignKeyName;
        IsNullable = isNullable;
    }
}