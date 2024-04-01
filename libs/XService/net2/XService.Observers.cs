/*
 * Implementation of classic Observer/Observable components.
 * Written by Dmitry Bond. at June 14, 2006
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace XService.Utils
{
    /// <summary>
    /// IObserver - is a copy of appropriate interface from Java
    /// Used together with Observable class.
    /// </summary>
    public interface IObserver
    {
        /// <summary>Notification method to call for observer object</summary>
        /// <param name="pSender">Observable object which initiated this call</param>
        /// <param name="pArg">Addtional object parameter which was passed into Observable.Notify() method</param>
        void update(Observable pSender, object pArg);
    }


    /// <summary>
    /// EventObserverUpdateArg
    /// </summary>
    public class EventObserverUpdateArg : EventArgs
    {
        public EventObserverUpdateArg(object pReasonId, object pData)
        {
            this.ReasonId = pReasonId;
            this.Data = pData;
        }

        public object ReasonId;
        public object Data;
    }


    /// <summary>
    /// Observable - is a copy of appropriate class from Java
    /// Used together with IObserver interface.
    /// </summary>
    public class Observable
    {
        /// <summary>Create new Observable object.</summary>
        /// <param name="AOwner">Object which is *owner* of this Observable object</param>
        public Observable(object AOwner)
        {
            this.owner = AOwner; 
        }
        
        #region Public interface

        /// <summary>
        /// Observer delegate method. Can be used instead of object which has to implemnt IObserver intreface.
        /// So, using delegates is a specific feature of .NET, so both methods are supported by Observable class - delegates and objects which has to implement IObserver intreface.
        /// </summary>
        /// <param name="pSender">Observable object which initiated this call</param>
        /// <param name="pArg">Addtional object parameter which was passed into Observable.Notify() method</param>
        public delegate void NotificationMethod(Observable pSender, object pArg);

        /// <summary>Add new observer to list. Observer object is supposed to implemnt *IObserver* interface. When pObserver is null nothing is added. When this pObserver was already added then nothing done.</summary>
        /// <param name="pObserver">Object which will receive notifications about changes in owner of this Observable object</param>
        public void AddObserver(IObserver pObserver)
        {
            lock (this)
            {
                if (pObserver != null && !this.observers.Contains(pObserver))
                    this.observers.Add(pObserver);
            }
        }

        /// <summary>Add new observer delagate to list. When pObserver is null nothing is added. When this pObserver was already added then nothing done.</summary>
        /// <param name="pObserver">Delegate which will receive notifications about changes in owner of this Observable object</param>
        public void AddObserver(NotificationMethod pObserver)
        {
            lock (this)
            {
                if (pObserver != null && !this.delegates.Contains(pObserver))
                    this.delegates.Add(pObserver);
            }
        }

        /// <summary>Remove specified observer from list.</summary>
        /// <param name="pObserver">Object which should be removed from list of observers. When specified observer is not in the list it will do nothing</param>
        public void DeleteObserver(IObserver pObserver)
        {
            lock (this)
            {
                this.observers.Remove(pObserver);
            }
        }

        /// <summary>Remove specified observer delegate from list.</summary>
        /// <param name="pObserver">Delegte which should be removed from list of observers. When specified observer delegate is not in the list it will do nothing</param>
        public void DeleteObserver(NotificationMethod pObserver)
        {
            lock (this)
            {
                this.delegates.Remove(pObserver);
            }
        }

        /// <summary>Return is specified observer delegate is exists in list of observers.</summary>
        /// <param name="pObserver">Observer delegate object to search in list of observers</param>
        public bool HasObserver(IObserver pObserver)
        {
            lock (this)
            {
                return (this.observers.IndexOf(pObserver) >= 0);
            }
        }

        /// <summary>Return is specified observer is exists in list of observers.</summary>
        /// <param name="pObserver">Observer object to search in list of observers</param>
        public bool HasObserver(NotificationMethod pObserver)
        {
            lock (this)
            {
                return (this.delegates.IndexOf(pObserver) >= 0);
            }
        }

        /// <summary>Return number of observes and observer delegates in list</summary>
        /// <returns>Number of observes and observer delegates in list for this Observable object</returns>
        public int CountObservers()
        {
            lock (this)
            {
                return this.observers.Count + this.delegates.Count;
            }
        }

        /// <summary>Remove all observers from list and reset *IsChanged* flag in this Observable object to *false*</summary>
        public void Clear()
        {
            lock (this)
            {
                this.observers.Clear();
                this.delegates.Clear();
                ClearChanged();
            }
        }

        /// <summary>Reset *IsChanged* flag in this Observable object to *false*</summary>
        public void ClearChanged()
        {
            lock (this)
            {
                this.changed = false;
            }
        }

        /// <summary>Set *IsChanged* flag in this Observable object to *true*</summary>
        public void SetChanged()
        {
            lock (this)
            {
                this.changed = true;
            }
        }

        /// <summary>Test *IsChanged* flag in this Observable object</summary>
        /// <returns>Returns current value of *IsChanged* flag in this Observable object</returns>
        public bool HasChanged()
        {
            lock (this)
            {
                return this.changed;
            }
        }

        /// <summary>Returns Owner object for this Observable object</summary>
        /// <returns>Owner object for this Observable object</returns>
        public object GetOwner()
        {
            return this.owner;
        }

        /// <summary>
        /// If *IsChanged* flag is *true* then it will notify all observers and observer delegates in list for this Observable object.
        /// Here *notify* means - call *update(sender, arg)* method for every observer and observer delegate in list.
        /// </summary>
        /// <param name="arg">Additional object argument to pass to all observers and observer delegates as additional parameter</param>
        public void NotifyObservers(object arg)
        {
            NotifyObservers(arg, false);
        }

        /// <summary>
        /// If *IsChanged* flag is *true* or when *pForce* is *true* then it will notify all observers and observer delegates in list for this Observable object.
        /// Here *notify* means - call *update(sender, arg)* method for every observer and observer delegate in list.
        /// </summary>
        /// <param name="arg">Additional object argument to pass to all observers and observer delegates as additional parameter</param>
        public void NotifyObservers(object arg, bool pForce)
        {
            // notifications should be done outside of lock() section, that is why need an array here (a copy of observers list)
            IObserver[] arrObservers = null;
            NotificationMethod[] arrDelegates = null;
            lock (this)
            {
                if (!(pForce || this.changed)) return;

                arrObservers = this.observers.ToArray();
                arrDelegates = this.delegates.ToArray();
                ClearChanged();
            }
            int i;
            for (i = arrObservers.Length - 1; i >= 0; i--)
                arrObservers[i].update(this, arg);
            for (i = arrDelegates.Length - 1; i >= 0; i--)
                arrDelegates[i](this, arg);
        }

        #endregion // Public interface

        #region Implementation details

        private object owner = null;
        private bool changed = false;
        private List<IObserver> observers = new List<IObserver>();
        private List<NotificationMethod> delegates = new List<NotificationMethod>();

        #endregion // Implementation details

    }
}
