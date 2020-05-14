namespace UndoMethods
{
    /// <summary>
    /// This is implemented by classes which act as records for storage of undo/redo records
    /// </summary>
    public interface IUndoRedoRecord
    {
        string Name { get; }

        void Execute();
    }
}