namespace SQLiteStorage;

public interface IInstanceEntity<T>
    where T : BaseEntity<T>, new()
{
    public bool IsInstance { get; set; }
}
