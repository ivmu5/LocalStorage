namespace SQLiteStorage;

/// <summary>
/// Event args при изменении значения.
/// </summary>
public class ValueChangedEventArgs<TValue> : EventArgs
{
    public TValue? NewValue { get; private set; }

    public TValue? OldValue { get; private set; }

    public ValueChangedEventArgs(TValue? oldValue, TValue? newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}