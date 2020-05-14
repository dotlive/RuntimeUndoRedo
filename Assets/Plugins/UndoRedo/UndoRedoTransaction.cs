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

        public IUndoRedoRecord TopOperation
        {
            get
            {
                if (OperationsCount == 0)
                {
                    return null;
                }

                return m_UndoRedoOperations[0];
            }
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
            m_UndoRedoOperations.Push(operation);
        }

        public void Execute(ExecuteType et)
        {
            switch (et)
            {
                case ExecuteType.TopOnly:
                    RunByIndex(0, et);
                    break;

                case ExecuteType.BottomOnly:
                    RunByIndex(m_UndoRedoOperations.Count - 1, et);
                    break;

                default:
                    m_UndoRedoOperations.ForEach((a) => a.Execute(et));
                    break;
            }
        }

        protected bool RunByIndex(int index, ExecuteType et)
        {
            if (index < 0 || index >= OperationsCount)
            {
                return false;
            }

            m_UndoRedoOperations[index].Execute(et);

            return true;
        }
    }
}
