/*
 * Simple component to count object references.
 * Written by Dmitry Bond. at April 5, 2012
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace XService
{
    /// <summary>
    /// The idea is - when refCount == 0 it will release/dispose a referenced object.
    /// Usage example:
    ///     ObjectRefCounter.RefCount objRf = new ObjectRefCounter.RefCount(dbConnection);
    ///     using (ObjectRefCounter rf = new ObjectRefCounter(objRf))
    ///     {
    ///     }
    /// </summary>
    public class ObjectRefCounter : IDisposable 
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("ObjectRefCounter", "ObjectRefCounter");

        public class RefCount
        {
            public delegate void OnAutoDisposingMethod(RefCount pRef);

            public RefCount(object pObject)
            {
                this.Object = pObject;
                this.SyncRoot = this.Object;
            }

            public RefCount(object pObject, bool pEnableAutoDispose)
            {
                this.Object = pObject;
                this.EnableAutoDispose = pEnableAutoDispose;
            }

            public RefCount(object pObject, object pSyncRoot, bool pEnableAutoDispose)
            {
                this.Object = pObject;
                this.SyncRoot = pSyncRoot;
                this.EnableAutoDispose = pEnableAutoDispose;
            }

            public void Lock()
            {
                if (this.SyncRoot != null)
                    lock (this.SyncRoot)
                    {
                        this.UseCounter++;
                    }
                else
                    this.UseCounter++;
                this.Used = DateTime.Now;
            }

            public void Unlock()
            {
                if (this.SyncRoot != null)
                    lock (this.SyncRoot)
                    {
                        doUnlock();
                    }
                else
                    doUnlock();
                this.Released = DateTime.Now;
            }

            protected void doUnlock()
            {
                this.UseCounter--;

                Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? String.Format(" -(ref) for [{0}] = {1}",
                    this.Object, this.UseCounter) : "");

                if (this.UseCounter <= 0 && this.EnableAutoDispose)
                {
                    Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? String.Format(" (ref) auto-disposing [{0}] because UseCounter is {1}!",
                        this.Object, this.UseCounter) : "");

                    if (this.OnAutoDisposing != null)
                        this.OnAutoDisposing(this);

                    if (this.Object is IDisposable)
                    {
                        object obj = this.Object;
                        this.Object = null;
                        (obj as IDisposable).Dispose();
                    }
                    else
                        this.Object = null;
                }
            }

            public object Object;
            public object SyncRoot;
            public object Tag;
            public int UseCounter = 0;
            public bool EnableAutoDispose = false;
            public DateTime Used = DateTime.MinValue;
            public DateTime Released = DateTime.MinValue;
            public OnAutoDisposingMethod OnAutoDisposing { get; set; }
        }

        public ObjectRefCounter(RefCount pObjectRef)
        {
            this.objectRef = pObjectRef;
            this.objectRef.Lock();
            
            Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? String.Format(" (ref)+ for [{0}] = {1}", 
                this.objectRef.Object, this.objectRef.UseCounter) : "");
        }

        public void Dispose()
        {
            this.objectRef.Unlock();
        }

        public RefCount ObjectRef { get { return this.objectRef; } }
        private RefCount objectRef;
    }
}

namespace XService.Utils
{
	public class ObjectRefCounter : XService.ObjectRefCounter
	{
		public ObjectRefCounter(RefCount pObjectRef)
			: base (pObjectRef)
		{
		}
	}
}
