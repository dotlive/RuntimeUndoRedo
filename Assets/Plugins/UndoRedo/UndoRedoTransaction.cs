using System;
using System.Collections.Generic;

namespace UndoMethods
{
    /// <summary>
    /// This acts as a container for multiple undo/redo records.
    /// </summary>
    public class UndoRedoTransaction : IDisposable, IUndoRedoRecord
    {
        private string m_Name;
        private List<IUndoRedoRecord> m_UndoRedoOperations = new List<IUndoRedoRecord>();

        public string Name
        {
            get { return m_Name; }
        }

        public int OperationsCount
        {
            get { return m_UndoRedoOperations.Count; }
        }

        public UndoRedoTransaction(string name = "")
        {
            m_Name = name;
            UndoRedoManager.Instance.StartTransaction(this);
        }

        public void Dispose()
        {
            UndoRedoManager.Instance.EndTransaction(this);
        }

        public void AddUndoRedoOperation(IUndoRedoRecord operation)
        {
            m_UndoRedoOperations.Insert(0, operation);
        }

        public void Execute()
        {
            m_UndoRedoOperations.ForEach((a) => a.Execute());
        }
    }
}
