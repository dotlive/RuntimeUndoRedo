using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Logger = UnityEngine.Debug;

namespace UndoMethods
{
    /// <summary>
    /// This is a singleton class which stores undo/redo records and executes the undo/redo operations specified in these records
    /// </summary>
    public class UndoRedoManager
    {
        public delegate void OnStackStatusChanged(bool hasItems);

        /// <summary>
        /// Is fired when the undo stack status is changed
        /// </summary>
        public event OnStackStatusChanged UndoStackStatusChanged;

        /// <summary>
        /// Is fired when the redo stack status is changed
        /// </summary>
        public event OnStackStatusChanged RedoStackStatusChanged;

        /// <summary>
        /// Stores undo records
        /// </summary>
        private List<IUndoRedoRecord> m_UndoStack = new List<IUndoRedoRecord>();

        /// <summary>
        /// Stores redo records
        /// </summary>
        private List<IUndoRedoRecord> m_RedoStack = new List<IUndoRedoRecord>();

        /// <summary>
        /// This is used to determine if an undo operation is going on
        /// </summary>
        private bool m_UndoGoingOn = false;

        /// <summary>
        /// This is used to determine if a redo operation is going on
        /// </summary>
        private bool m_RedoGoingOn = false;

        /// <summary>
        /// Stores instance of this singleton object
        /// </summary>
        private static volatile UndoRedoManager m_Instance = new UndoRedoManager();

        /// <summary>
        /// Maximum items to store in undo redo stack
        /// </summary>
        protected int m_MaxItems = 10;

        /// <summary>
        /// stores the transaction (if any) under which the current undo/redo operation(s) are occuring.
        /// </summary>
        private UndoRedoTransaction m_CurTran;

        /// <summary>
        /// Returns instance of this singleton object
        /// </summary>
        /// <returns></returns>
        public static UndoRedoManager Instance
        {
            get { return m_Instance; }
        }

        /// <summary>
        /// Sets/gets maximum items to be stored in the stack. Note that the change takes effect the next time an item is added to the undo/redo stack
        /// </summary>
        public int MaxItems
        {
            get { return m_MaxItems; }
            set { m_MaxItems = value; }
        }

        public int UndoOperationCount
        {
            get { return m_UndoStack.Count; }
        }

        public int RedoOperationCount
        {
            get { return m_RedoStack.Count; }
        }

        public bool HasUndoOperations
        {
            get { return m_UndoStack.Count != 0; }
        }

        public bool HasRedoOperations
        {
            get { return m_RedoStack.Count != 0; }
        }

        /// <summary>
        /// Starts a transaction under which all undo redo operations take place
        /// </summary>
        /// <param name="tran"></param>
        public void StartTransaction(UndoRedoTransaction tran)
        {
            if (m_CurTran == null)
            {
                m_CurTran = tran;
                ///push an empty undo operation
                m_UndoStack.Push(new UndoRedoTransaction(tran.Name));
                m_RedoStack.Push(new UndoRedoTransaction(tran.Name));
            }
        }

        /// <summary>
        /// Ends the transaction under which all undo/redo operations take place
        /// </summary>
        /// <param name="tran"></param>
        public void EndTransaction(UndoRedoTransaction tran)
        {
            if (m_CurTran == tran)
            {
                m_CurTran = null;
                ///now we might have had no items added to undo and redo stack as a part of this transaction. Check empty transaction at top and remove them
                if (m_UndoStack.Count > 0)
                {
                    UndoRedoTransaction t = m_UndoStack[0] as UndoRedoTransaction;
                    if (t != null && t.OperationsCount == 0)
                    {
                        m_UndoStack.Pop();
                    }
                }

                if (m_RedoStack.Count > 0)
                {
                    UndoRedoTransaction t = m_RedoStack[0] as UndoRedoTransaction;
                    if (t != null && t.OperationsCount == 0)
                    {
                        m_RedoStack.Pop();
                    }
                }
            }
        }

        /// <summary>
        /// Pushes an item onto the undo/redo stack.
        /// 1) If this is called outside the context of a undo/redo operation, the item is added to the undo stack.
        /// 2) If this is called in the context of an undo operation, the item is added to redo stack.
        /// 3) If this is called in context of an redo operation, item is added to undo stack.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="undoOperation"></param>
        /// <param name="undoData"></param>
        /// <param name="description"></param>
        public void Push<T>(UndoRedoOperation<T> undoOperation, T undoData, string description = "")
        {
            Debug.Assert(null != undoOperation);

            List<IUndoRedoRecord> stack = null;
            Action eventToFire;

            // Determien the stack to which this operation will be added
            if (m_UndoGoingOn)
            {
                Logger.LogFormat("Adding to redo stack {0} with data {1}", undoOperation.Method.Name, undoData);
                stack = m_RedoStack;
                eventToFire = new Action(FireRedoStackStatusChanged);
            }
            else
            {
                Logger.LogFormat("Adding to undo stack {0} with data {1}", undoOperation.Method.Name, undoData);
                stack = m_UndoStack;
                eventToFire = new Action(FireUndoStackStatusChanged);
            }

            if ((!m_UndoGoingOn) && (!m_RedoGoingOn))
            {
                // If someone added an item to undo stack while there are items in redo stack.. clear the redo stack
                m_RedoStack.Clear();
                FireRedoStackStatusChanged();
            }

            // If a transaction is going on, add the operation as a entry to the transaction operation
            if (m_CurTran == null)
            {
                stack.Push(new UndoRedoRecord<T>(undoOperation, undoData, description));
            }
            else
            {
                (stack[0] as UndoRedoTransaction).AddUndoRedoOperation(new UndoRedoRecord<T>(undoOperation, undoData, description));
            }

            // If the stack count exceeds maximum allowed items
            if (MaxItems > 0 && stack.Count > MaxItems)
            {
                object o = stack[stack.Count - 1];
                Logger.LogFormat("Removing item {0}", o);
                stack.RemoveRange(MaxItems-1, stack.Count-MaxItems);
            }

            //Fire event to inform consumers that the stack size has changed
            eventToFire();
        }

        /// <summary>
        /// Performs an undo operation
        /// </summary>
        public void Undo()
        {
            try
            {
                m_UndoGoingOn = true;

                if (m_UndoStack.Count == 0)
                {
                    throw new InvalidOperationException("Nothing in the undo stack");
                }
                object oUndoData = m_UndoStack.Pop();

                Type undoDataType = oUndoData.GetType();

                // If the stored operation was a transaction, perform the undo as a transaction too.
                if (typeof (UndoRedoTransaction).Equals(undoDataType))
                {
                    StartTransaction(oUndoData as UndoRedoTransaction);
                }

                undoDataType.InvokeMember("Execute", BindingFlags.InvokeMethod, null, oUndoData, null);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex.Message);
            }
            finally
            {
                m_UndoGoingOn = false;

                EndTransaction(m_CurTran);

                FireUndoStackStatusChanged();
            }
        }

        /// <summary>
        /// Performs a redo operation
        /// </summary>
        public void Redo()
        {
            try
            {
                m_RedoGoingOn = true;

                if (m_RedoStack.Count == 0)
                {
                    throw new InvalidOperationException("Nothing in the redo stack");
                }
                object oUndoData = m_RedoStack.Pop();

                Type undoDataType = oUndoData.GetType();

                // If the stored operation was a transaction, perform the redo as a transaction too.
                if (typeof (UndoRedoTransaction).Equals(undoDataType))
                {
                    StartTransaction(oUndoData as UndoRedoTransaction);
                }

                undoDataType.InvokeMember("Execute", BindingFlags.InvokeMethod, null, oUndoData, null);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex.Message);
            }
            finally
            {
                m_RedoGoingOn = false;

                EndTransaction(m_CurTran);

                FireRedoStackStatusChanged();
            }
        }

        /// <summary>
        /// Clears all undo/redo operations from the stack
        /// </summary>
        public void Clear()
        {
            m_UndoStack.Clear();
            m_RedoStack.Clear();
            FireUndoStackStatusChanged();
            FireRedoStackStatusChanged();
        }

        public T GetUndoStackTop<T>()
        {
            if (!HasUndoOperations)
            {
                return default(T);
            }

            object undoData = m_UndoStack[0];
            Type undoDataType = undoData.GetType();
            if (typeof(UndoRedoTransaction).Equals(undoDataType))
            {
                undoData = undoDataType.GetProperty("TopOperation").GetValue(undoData, null);
                undoDataType = undoData.GetType();
            }

            if (undoData == null)
            {
                return default(T);
            }

            return (T)undoDataType.GetField("m_UndoData").GetValue(undoData);
        }

        public T GetRedoStackTop<T>()
        {
            if (!HasRedoOperations)
            {
                return default(T);
            }

            object undoData = m_RedoStack[0];
            Type undoDataType = undoData.GetType();
            if (typeof(UndoRedoTransaction).Equals(undoDataType))
            {
                undoData = undoDataType.GetProperty("TopOperation").GetValue(undoData, null);
                undoDataType = undoData.GetType();
            }

            if (undoData == null)
            {
                return default(T);
            }

            return (T)undoDataType.GetField("m_UndoData").GetValue(undoData);
        }

        /// <summary>
        /// Returns a list containing description of all undo stack records
        /// </summary>
        /// <returns></returns>
        public IList<string> GetUndoStackInformation()
        {
            return m_UndoStack.ConvertAll((input) => input.Name == null ? "" : input.Name);
        }

        /// <summary>
        /// Returns a list containing description of all redo stack records
        /// </summary>
        /// <returns></returns>
        public IList<string> GetRedoStackInformation()
        {
            return m_RedoStack.ConvertAll((input) => input.Name == null ? "" : input.Name);
        }

        private void FireUndoStackStatusChanged()
        {
            if (null != UndoStackStatusChanged)
            {
                UndoStackStatusChanged(HasUndoOperations);
            }
        }

        private void FireRedoStackStatusChanged()
        {
            if (null != RedoStackStatusChanged)
            {
                RedoStackStatusChanged(HasRedoOperations);
            }
        }
    }
}
