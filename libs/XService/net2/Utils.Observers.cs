using System;
using System.Collections.Generic;
using System.Text;

namespace XService.Utils
{
    /// <summary>
    /// IObserver
    /// </summary>
    public interface IObserver
    {
        void update(Observable senbder, object arg);
    }


    /// <summary>
    /// Observable
    /// </summary>
    public class Observable
    {
        public Observable(object AOwner)
        {
            this.owner = AOwner; 
        }
        
        #region Interface

        public void AddObserver(IObserver observer)
        {
            lock (this)
                this.observers.Add(observer);  
        }

        public void DeleteObserver(IObserver observer)
        {
            lock (this)
                this.observers.Remove(observer);
        }

        public int CountObservers()
        {
            return this.observers.Count;
        }

        public void Clear()
        {
            lock (this)
                this.observers.Clear();
        }

        public void ClearChanged()
        {
            lock (this)
                this.changed = false;
        }

        public void SetChanged()
        {
            lock (this)
                this.changed = true;
        }

        public bool HasChanged()
        {            
            return this.changed;
        }

        public void NotifyObservers(object arg)
        {
            IObserver[] arrLocal = null;
            lock (this)
            {
                if (!this.changed) return;

                arrLocal = this.observers.ToArray();
                ClearChanged();
            }
            for (int i = arrLocal.Length - 1; i >= 0; i--)
                arrLocal[i].update(this, arg);
        }

        #endregion // Interface

        #region Implementation

        private object owner = null;
        private bool changed = false;
        private List<IObserver> observers = new List<IObserver>();

        #endregion // Implementation

    }
}
