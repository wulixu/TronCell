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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenClooVision.Imaging;

namespace OpenClooVision.Kernels.ViolaJones
{
    /// <summary>
    /// Unit tests for CpuProgramViolaJones
    /// </summary>
    [TestClass]
    public class CpuProgramViolaJonesTest : BaseTest
    {
        /// <summary>
        /// CPU functions for Viola & Jones
        /// </summary>
        public CpuProgramViolaJones CpuViolaJones { get; protected set; }

        Bitmap _facesTest;

        /// <summary>
        /// Constructor
        /// </summary>
        public CpuProgramViolaJonesTest()
        {
            CpuViolaJones = CpuProgramViolaJones.CreateFaceDetector(640, 480);

            // load faces test bitmap
            Assembly assembly = Assembly.GetExecutingAssembly();
            string name = assembly.GetManifestResourceNames().Where(x => x.EndsWith(".facestest.jpg")).FirstOrDefault();
            _facesTest = new Bitmap(assembly.GetManifestResourceStream(name));
        }

        /// <summary>
        /// Tests the scaling and weighting a HaarRectangle
        /// </summary>
        [TestMethod]
        public void CpuExecuteScaleWeightHaarRectangleTest()
        {
            List<HaarFeatureNode> list = new List<HaarFeatureNode>();
            list.AddHaarFeature(1, 1, 1, new HaarRectangle(1, 1, 1, 1, 1), new HaarRectangle(1, 1, 1, 1, 1));
            list.AddHaarFeature(1, 1, 1, new HaarRectangle(1, 1, 1, 1, 1), new HaarRectangle(1, 1, 1, 1, 1), new HaarRectangle(1, 1, 1, 1, 1));
            Assert.AreEqual(2, list.Count);

            var nodes = list.ToArray();
            float scaleRect = 2;
            float scaleWeight = 3;
            for (int i = 0; i < list.Count; i++)
                HaarExtensions.SetScaleAndWeight(ref nodes[i], scaleRect, scaleWeight);

            for (int i = 0; i < list.Count; i++)
            {
                Assert.AreNotEqual(0, nodes[i].Rect1.ScaledX);
                Assert.AreNotEqual(0, nodes[i].Rect1.ScaledY);
                Assert.AreNotEqual(0, nodes[i].Rect1.ScaledWidth);
                Assert.AreNotEqual(0, nodes[i].Rect1.ScaledHeight);
                Assert.AreNotEqual(0, nodes[i].Rect1.ScaledWeight);

                Assert.AreEqual(scaleRect * nodes[i].Rect1.X, nodes[i].Rect1.ScaledX);
                Assert.AreEqual(scaleRect * nodes[i].Rect1.Y, nodes[i].Rect1.ScaledY);
                Assert.AreEqual(scaleRect * nodes[i].Rect1.Width, nodes[i].Rect1.ScaledWidth);
                Assert.AreEqual(scaleRect * nodes[i].Rect1.Height, nodes[i].Rect1.ScaledHeight);

                Assert.AreNotEqual(0, nodes[i].Rect2.ScaledX);
                Assert.AreNotEqual(0, nodes[i].Rect2.ScaledY);
                Assert.AreNotEqual(0, nodes[i].Rect2.ScaledWidth);
                Assert.AreNotEqual(0, nodes[i].Rect2.ScaledHeight);

                Assert.AreEqual(scaleRect * nodes[i].Rect2.X, nodes[i].Rect2.ScaledX);
                Assert.AreEqual(scaleRect * nodes[i].Rect2.Y, nodes[i].Rect2.ScaledY);
                Assert.AreEqual(scaleRect * nodes[i].Rect2.Width, nodes[i].Rect2.ScaledWidth);
                Assert.AreEqual(scaleRect * nodes[i].Rect2.Height, nodes[i].Rect2.ScaledHeight);

                if (nodes[i].RectCount >= 3)
                {
                    Assert.AreNotEqual(0, nodes[i].Rect3.ScaledX);
                    Assert.AreNotEqual(0, nodes[i].Rect3.ScaledY);
                    Assert.AreNotEqual(0, nodes[i].Rect3.ScaledWidth);
                    Assert.AreNotEqual(0, nodes[i].Rect3.ScaledHeight);

                    Assert.AreEqual(scaleRect * nodes[i].Rect3.X, nodes[i].Rect3.ScaledX);
                    Assert.AreEqual(scaleRect * nodes[i].Rect3.Y, nodes[i].Rect3.ScaledY);
                    Assert.AreEqual(scaleRect * nodes[i].Rect3.Width, nodes[i].Rect3.ScaledWidth);
                    Assert.AreEqual(scaleRect * nodes[i].Rect3.Height, nodes[i].Rect3.ScaledHeight);
                    Assert.AreEqual(-(nodes[i].Rect2.GetArea() * nodes[i].Rect2.ScaledWeight + nodes[i].Rect3.GetArea() * nodes[i].Rect3.ScaledWeight)
                        / (nodes[i].Rect1.GetArea()), nodes[i].Rect1.ScaledWeight);
                }
                else
                {
                    Assert.AreEqual(-(nodes[i].Rect2.GetArea() * nodes[i].Rect2.ScaledWeight) / nodes[i].Rect1.GetArea(), nodes[i].Rect1.ScaledWeight);
                }
            }
        }

        /// <summary>
        /// Tests the Viola & Jones algorithm on CPU
        /// </summary>
        [TestMethod]
        public void CpuExecuteViolaJonesTest()
        {
            var facesTest = CpuImage2DByteA.CreateFromBitmap(_facesTest);

            var violaJones = CpuHaarObjectDetector.CreateFaceDetector(facesTest.Width, facesTest.Height);
            violaJones.ScalingFactor = 1.25f;
            violaJones.ScalingMode = ScalingMode.LargerToSmaller;
            violaJones.MinSize = new Size(20, 20);
            violaJones.MaxSize = new Size(300, 300);

            int facesCount = violaJones.ProcessFrame(facesTest);
            Assert.AreEqual(26, facesCount);
            using (Bitmap bitmap = facesTest.ToBitmap())
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    for (int i = 0; i < facesCount; i++)
                        g.DrawRectangle(Pens.Red, violaJones.ResultRectangles.HostBuffer[i]);
                }
                //bitmap.Save(@"C:\temp\test.png");
            }
        }
    }
}
