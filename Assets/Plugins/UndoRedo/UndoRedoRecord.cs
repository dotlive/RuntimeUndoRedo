using Logger = UnityEngine.Debug;

namespace UndoMethods
{
    public delegate void UndoRedoOperation<T>(T undoData);

    /// <summary>
    /// Contains information about an undo or redo record
    /// </summary>
    public class UndoRedoRecord<T> : IUndoRedoRecord
    {
        private UndoRedoOperation<T> m_Operation;
        private T m_UndoData;
        private string m_Description;

        public string Name
        {
            get { return m_Description; }
        }

        public UndoRedoRecord()
        {
        }

        public UndoRedoRecord(UndoRedoOperation<T> operation, T undoData, string description="")
        {
            SetInfo(operation, undoData, description);
        }

        public void SetInfo(UndoRedoOperation<T> operation, T undoData, string description="")
        {
            m_Operation = operation;
            m_UndoData = undoData;
            m_Description = description;
        }

        public void Execute(ExecuteType et)
        {
            Logger.LogFormat("Undo/redo operation {0} with data {1} - {2}", m_Operation, m_UndoData, m_Description);
            m_Operation(m_UndoData);
        }
    }
}
