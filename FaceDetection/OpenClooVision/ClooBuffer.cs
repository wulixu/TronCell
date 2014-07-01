#region License
/*

Copyright (c) 2010-2011 by Hans Wolff

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

*/
#endregion

using System;
using System.Runtime.InteropServices;
using Cloo;
using OpenClooVision.Imaging;

namespace OpenClooVision
{
    /// <summary>
    /// Cloo computer buffer
    /// </summary>
    [CLSCompliant(false)]
    public class ClooBuffer<T> : ComputeBuffer<T>, IBuffer<T> where T : struct
    {
        private object _lockRead = new object();
        private object _lockWrite = new object();

        private ClooContext _context;
        /// <summary>
        /// Gets the associated compute context
        /// </summary>
        public new ClooContext Context
        {
            get { return _context; }
        }

        /// <summary>
        /// Host buffer
        /// </summary>
        protected T[] _hostBuffer = null;
        /// <summary>
        /// Host buffer
        /// </summary>
        public T[] HostBuffer
        {
            get { return _hostBuffer; }
            set { _hostBuffer = value; }
        }

        protected bool _modified = false;
        /// <summary>
        /// Get or set state if host buffer has been modified since last operation
        /// </summary>
        public bool Modified
        {
            get { return _modified; }
            set { _modified = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">compute context</param>
        /// <param name="flags">memory flags</param>
        /// <param name="data">buffer data</param>
        public ClooBuffer(ClooContext context, ComputeMemoryFlags flags, T[] data)
            : base(context, flags, data)
        {
            _context = context;
            _hostBuffer = data;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">compute context</param>
        /// <param name="flags">memory flags</param>
        /// <param name="count">buffer size</param>
        public ClooBuffer(ClooContext context, ComputeMemoryFlags flags, long count)
            : base(context, flags, count)
        {
            _context = context;
            _hostBuffer = new T[count];
        }

        /// <summary>
        /// Reads a buffer from device
        /// </summary>
        /// <param name="buffer">compute buffer</param>
        /// <param name="queue">command queue</param>
        /// <param name="buffer">buffer to read into</param>
        /// <param name="count">item count to read</param>
        public void ReadFromDevice(ClooCommandQueue queue)
        {
            lock (_lockRead)
            {
                GCHandle handle = GCHandle.Alloc(_hostBuffer, GCHandleType.Pinned);
                try { queue.Read(this, true, 0, Count, handle.AddrOfPinnedObject(), null); }
                finally { handle.Free(); }
            }
        }

        /// <summary>
        /// Write a buffer to device
        /// </summary>
        /// <param name="image">compute image</param>
        /// <param name="queue">command queue</param>
        public void WriteToDevice(ClooCommandQueue queue)
        {
            WriteToDevice(queue, _hostBuffer);
        }

        /// <summary>
        /// Writes a buffer from device
        /// </summary>
        /// <param name="image">compute image</param>
        /// <param name="queue">command queue</param>
        /// <param name="buffer">buffer to read into</param>
        public void WriteToDevice(ClooCommandQueue queue, T[] buf)
        {
            if (_hostBuffer.Length != buf.Length) throw new ArgumentException("Write buffer size (" + buf.Length + ") does not match the original size (" + _hostBuffer.Length + ")");

            lock (_lockWrite)
            {
                GCHandle handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
                try { queue.Write(this, true, 0, buf.Length, handle.AddrOfPinnedObject(), null); }
                finally { handle.Free(); }
                _hostBuffer = buf;

                _modified = false;
            }
        }
    }
}
