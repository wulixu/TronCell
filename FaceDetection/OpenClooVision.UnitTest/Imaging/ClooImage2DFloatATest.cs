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

using System;
using System.Linq;
using Cloo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenClooVision.Imaging
{
    /// <summary>
    /// Unit test for ClooImage2DFloatA
    /// </summary>
    [TestClass]
    public class ClooImage2DFloatATest : BaseTest
    {
        /// <summary>
        /// Test reading and writing to device
        /// </summary>
        [TestMethod]
        public void ClooWriteReadDeviceFloatA()
        {
            ResetRandomSeed();

            ClooImage2DFloatA image = CreateClooImageFloatA();

            // sum buffer values
            var sumToBe = image.HostBuffer.Sum(x => x);
            Assert.AreNotEqual(0, sumToBe); // just in case

            // now clear the buffer
            image.HostBuffer.Clear();

            // check if buffer is really empty (just in case)
            Assert.AreEqual(0, image.HostBuffer.Sum(x => x));

            image.ReadFromDevice(Queue);

            // sum again and compare
            Assert.AreEqual(sumToBe, image.HostBuffer.Sum(x => x), 2 * float.Epsilon);
        }
    }
}
