namespace SQLiteStorage;

public interface IRelationship
{
    string ForeignKeyName { get; }
    bool IsNullable { get; }
}
