namespace UndoMethods
{
    /// <summary>
    /// Transaction execute type
    /// </summary>
    public enum ExecuteType
    {
        Default = 0,
        TopOnly,
        BottomOnly,
    }

    /// <summary>
    /// This is implemented by classes which act as records for storage of undo/redo records
    /// </summary>
    public interface IUndoRedoRecord
    {
        string Name { get; }

        void Execute(ExecuteType et);
    }
}
