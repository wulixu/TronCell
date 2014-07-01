#region License
/*

Copyright (c) 2010-2011 Hans Wolff

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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenClooVision.Kernels.Core
{
    /// <summary>
    /// Unit tests for ClooProgramCore
    /// </summary>
    [TestClass]
    public class ClooProgramCoreTest : BaseTest
    {
        /// <summary>
        /// Testing to add a constant float value to all cells
        /// </summary>
        [TestMethod]
        public void ClooExecuteSetValue()
        {
            // byte
            {
                var buffer = CreateClooBufferByte();
                var sum = buffer.HostBuffer.Sum(x => x);
                Assert.AreNotEqual(0, sum);

                ClooImaging.SetValue(Queue, buffer, 1);

                // we haven't read from device yet, so sum should be the same
                Assert.AreEqual(sum, buffer.HostBuffer.Sum(x => x));
                buffer.ReadFromDevice(Queue);

                // check if add operation processed successfully
                Assert.AreEqual(buffer.HostBuffer.Length, buffer.HostBuffer.Sum(x => x));
            }

            // float
            {
                var buffer = CreateClooBufferFloat();
                var sum = buffer.HostBuffer.Sum(x => x);
                Assert.AreNotEqual(0, sum);

                ClooImaging.SetValue(Queue, buffer, 1f);

                // we haven't read from device yet, so sum should be the same
                Assert.AreEqual(sum, buffer.HostBuffer.Sum(x => x));
                buffer.ReadFromDevice(Queue);

                // check if add operation processed successfully
                Assert.AreEqual((float)(buffer.HostBuffer.Length), buffer.HostBuffer.Sum(x => x));
            }

            // uint
            {
                var buffer = CreateClooBufferUInt();
                var sum = buffer.HostBuffer.Sum(x => x);
                Assert.AreNotEqual(0, sum);

                ClooImaging.SetValue(Queue, buffer, 1);

                // we haven't read from device yet, so sum should be the same
                Assert.AreEqual(sum, buffer.HostBuffer.Sum(x => x));
                buffer.ReadFromDevice(Queue);

                // check if add operation processed successfully
                Assert.AreEqual(buffer.HostBuffer.Length, buffer.HostBuffer.Sum(x => x));
            }
        }
    }
}
