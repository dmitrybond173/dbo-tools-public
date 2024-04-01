using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace XService.Utils
{
    /// <summary>
    /// Pool of big storage objects (memory streams). 
    /// Such storage objects used to decrease usage of Big-Object-Heap memory in .NET.
    /// 
    /// Before usage need to initialize it, like this: 
    ///   MemoryStoragePool.Instance = new MemoryStoragePool(1024 * 1024, 32);
    /// </summary>
    public class MemoryStoragePool
    {
        public static TraceSwitch TrcLvl = new TraceSwitch("MemoryStoragePool", "MemoryStoragePool");

        public static MemoryStoragePool Instance = null;

        /// <summary>Create MemoryStoragePool instance</summary>
        /// <param name="pOneStreamSize">Size of one cell in storage</param>
        /// <param name="pCellsCount">Number of cell to create initially</param>
        public MemoryStoragePool(int pOneStreamSize, int pCellsCount)
        {
            if (pOneStreamSize < 0)
                pOneStreamSize = 1024 * 1024;
            this.CellSize = pOneStreamSize;

            if (pCellsCount <= 0)
                pCellsCount = 32;
            this.CellsCount = pCellsCount;
        }

        /// <summary>Get or set size of one cell in storage. When size changed it will not affect existing cells</summary>
        public int CellSize { get; set; }

        /// <summary>Get or set number of cells in storage. When less than number of existing cells it will delete extra, when bigger it will create more cells</summary>
        public int CellsCount
        {
            get { return this.cells.Count; }
            set
            {
                if (this.cells.Count == value) return;

                object syncRoot = this.cells;
                SyncUtils.LockInfo lck = TrcLvl.TraceVerbose ? SyncUtils.S_EnteringLock(string.Format("BigStreamsPool.getCellsCount"), syncRoot) : null;
                lock (syncRoot)
                {
                    if (lck != null) SyncUtils.S_LockEntered(lck);

                    if (this.cells.Count < value)
                    {
                        while (this.cells.Count < value)
                        {
                            StorageCell c = new StorageCell(this, this.cells.Count);
                            this.cells.Add(c);
                        }
                    }
                    else
                    {
                        while (this.cells.Count > value)
                        {
                            StorageCell c = this.cells[this.cells.Count - 1];
                            c.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>Allocate new storage object and mark it as 'allocated'</summary>
        public StorageCellWrapper AllocateCell()
        {
            object syncRoot = this.cells;
            SyncUtils.LockInfo lck = TrcLvl.TraceVerbose ? SyncUtils.S_EnteringLock(string.Format("BigStreamsPool.AllocateCell"), syncRoot) : null;
            lock (syncRoot)
            {
                if (lck != null) SyncUtils.S_LockEntered(lck);

                foreach (StorageCell c in this.cells)
                {
                    if (c.Allocated == DateTime.MinValue)
                    {
                        c.Allocated = DateTime.Now;
                        return new StorageCellWrapper(c);
                    }
                }

                StorageCell newCell = new StorageCell(this, this.cells.Count);
                this.cells.Add(newCell);
                newCell.Allocated = DateTime.Now;
                return new StorageCellWrapper(newCell);
            }
        }

        /// <summary>Dump to Trace information on all cells in storage</summary>
        public void Dump()
        {
            object syncRoot = this.cells;
            SyncUtils.LockInfo lck = TrcLvl.TraceVerbose ? SyncUtils.S_EnteringLock(string.Format("BigStreamsPool.Dump"), syncRoot) : null;
            lock (syncRoot)
            {
                if (lck != null) SyncUtils.S_LockEntered(lck);

                int allocCnt = 0, maxSz = 0, minSz = Int32.MaxValue;
                string list = null;
                foreach (StorageCell c in this.cells)
                {
                    bool isFree = (c.Allocated == DateTime.MinValue);
                    if (!isFree)
                        allocCnt++;
                    if (maxSz < c.Data.Capacity)
                        maxSz = c.Data.Capacity;
                    if (minSz > c.Data.Capacity)
                        minSz = c.Data.Capacity;
                    string item = string.Format("#{0}:{1}({2}); ",
                        c.Index, (isFree ? "free" : "inUse"), c.Data.Capacity
                        );
                    if (list == null) list = item;
                    else list += item;
                }
                string info = string.Format("{0} cells [{1} allocated; minSize={2}; maxSize={3}]: {4}",
                    this.cells.Count, allocCnt, minSz, maxSz, list);
                Trace.WriteLine(info);
            }
        }

        private List<StorageCell> cells = new List<StorageCell>();

        /// <summary>Wrapper for StorageCell to auto-release after usage. It can be used in using (...) { ... } statement</summary>
        /// <example>
        /// using (MemoryStoragePool.StorageCellWrapper cell = MemoryStoragePool.Instance.AllocateCell()) 
        /// {
        ///   MemoryStream strm = cell.Stream;
        ///   // [...]
        /// }  
        /// </example>
        public class StorageCellWrapper : IDisposable
        {
            internal StorageCellWrapper(StorageCell pCell)
            {
                this.Cell = pCell;
                this.Stream.Position = 0;
                this.Stream.SetLength(0);

                Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? String.Format(
                    "StrmPool:Cell(#{0}, sz={1} of {2}) allocated...", this.Cell.Index, this.Cell.Data.Capacity, this.Cell.Owner.CellSize) : "");
            }

            public void Dispose()
            {
                if (this.Cell != null)
                {
                    Trace.WriteLineIf(TrcLvl.TraceInfo, TrcLvl.TraceInfo ? String.Format(
                        "StrmPool:Cell(#{0}, sz={1} of {2}) released!", this.Cell.Index, this.Cell.Data.Capacity, this.Cell.Owner.CellSize) : "");

                    this.Cell.Allocated = DateTime.MinValue;
                    this.Cell = null;
                }
            }

            public StorageCell Cell { get; protected set; }
            public MemoryStream Stream { get { return this.Cell.Data; } }
        }

        /// <summary>Holder for storage object with allocated/free indicator. It is not intended for direct usage</summary>
        public class StorageCell : IDisposable
        {
            internal StorageCell(MemoryStoragePool pOwner, int pIndex)
            {
                this.Owner = pOwner;
                this.Index = pIndex;
                this.Data = new MemoryStream(this.Owner.CellSize);
            }

            public void Dispose()
            {
                if (this.Data != null)
                {
                    MemoryStream strm = this.Data;
                    this.Data = null;
                    strm.Dispose();
                }
                GC.SuppressFinalize(this);
            }

            public MemoryStoragePool Owner;
            public int Index { get; protected set; }
            public MemoryStream Data { get; protected set; }
            public DateTime Allocated = DateTime.MinValue;
        }
    }
}
