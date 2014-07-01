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

using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Cloo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenClooVision.Imaging;

namespace OpenClooVision.Kernels.ViolaJones
{
    /// <summary>
    /// Unit tests for ClooProgramViolaJones
    /// </summary>
    [TestClass]
    public class ClooProgramViolaJonesTest : BaseTest
    {
        /// <summary>
        /// CPU functions for Viola & Jones
        /// </summary>
        public ClooProgramViolaJones ClooViolaJones { get; protected set; }

        Bitmap _facesTest;

        /// <summary>
        /// Constructor
        /// </summary>
        public ClooProgramViolaJonesTest()
        {
            ClooViolaJones = ClooProgramViolaJones.Create(Context);

            // load faces test bitmap
            Assembly assembly = Assembly.GetExecutingAssembly();
            string name = assembly.GetManifestResourceNames().Where(x => x.EndsWith(".facestest.jpg")).FirstOrDefault();
            _facesTest = new Bitmap(assembly.GetManifestResourceStream(name));
        }

        /// <summary>
        /// Creates a ClooBuffer and fills it with random values
        /// </summary>
        /// <param name="length">buffer length</param>
        /// <returns></returns>
        public virtual ClooBuffer<HaarFeatureNode> CreateClooBufferHaarFeatureNode(int length = 9999)
        {
            ResetRandomSeed();
            var buffer = new ClooBuffer<HaarFeatureNode>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, length);

            Assert.AreNotEqual(0, buffer.HostBuffer.Length);
            Assert.AreEqual(buffer.HostBuffer.Length * Marshal.SizeOf(typeof(HaarFeatureNode)), buffer.Size);
            buffer.WriteToDevice(Queue);

            return buffer;
        }

        /// <summary>
        /// Tests the Viola & Jones algorithm on GPU
        /// </summary>
        [TestMethod]
        public void ClooExecuteViolaJonesTest()
        {
            var facesTest = ClooImage2DByteA.CreateFromBitmap(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _facesTest);
            
            var violaJones = ClooHaarObjectDetector.CreateFaceDetector(Context, Queue, facesTest.Width, facesTest.Height);
            violaJones.ScalingFactor = 1.5f;
            violaJones.ScalingMode = ScalingMode.LargerToSmaller;
            violaJones.MinSize = new Size(30, 30);
            violaJones.MaxSize = new Size(300, 300);

            int facesCount = violaJones.ProcessFrame(facesTest);
            using (Bitmap bitmap = facesTest.ToBitmap(Queue))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    for (int i = 0; i < facesCount; i++)
                        g.DrawRectangle(Pens.Red, violaJones.ResultRectangles.HostBuffer[i]);
                }
                //bitmap.Save(@"C:\temp\test.png");
            }

            // we actually how many faces the GPU finds since float operations on each GPU may differ
            Assert.AreNotEqual(0, facesCount);
            Assert.IsTrue(facesCount < 20);
        }
    }
}
