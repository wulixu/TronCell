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

namespace OpenClooVision.Kernels.Imaging
{
    /// <summary>
    /// Unit tests for CpuProgramImaging
    /// </summary>
    [TestClass]
    public class CpuProgramImagingTest : BaseTest
    {
        /// <summary>
        /// Testing to add a constant float value to all cells
        /// </summary>
        [TestMethod]
        public void CpuExecuteAddValue()
        {
            // gray, float
            {
                var image = CreateCpuImageFloatA();
                var sum = image.HostBuffer.Sum(x => x);

                CpuImaging.AddValue(image, image, 1f);

                // check if add operation processed successfully
                Assert.AreEqual(sum + 1f * image.HostBuffer.Length, image.HostBuffer.Sum(x => x), 2 * float.Epsilon);
            }

            // RGBA, float
            {
                var image = CreateCpuImageFloatRgbA();
                var sum = image.HostBuffer.Sum(x => x);

                CpuImaging.AddValue(image, image, 1f);

                // check if add operation processed successfully
                Assert.AreEqual(sum + 1f * image.HostBuffer.Length, image.HostBuffer.Sum(x => x), 2 * float.Epsilon);
            }
        }

        /// <summary>
        /// Testing flipping image on X axis
        /// </summary>
        [TestMethod]
        public void ClooExecuteFlipX()
        {
            // byte, gray
            {
                var source = CreateCpuImageByteA();
                var dest = CreateCpuImageByteA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();

                CpuImaging.FlipX(source, dest);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        Assert.AreEqual(source.HostBuffer[y * source.Width + x], dest.HostBuffer[y * dest.Width + source.Width - x - 1]);
            }

            // byte, RGBA
            {
                var source = CreateCpuImageByteRgbA();
                var dest = CreateCpuImageByteRgbA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();

                CpuImaging.FlipX(source, dest);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        for (int i = 0; i < 4; i++)
                            Assert.AreEqual(source.HostBuffer[4 * (y * source.Width + x) + i], dest.HostBuffer[4 * (y * dest.Width + source.Width - x - 1) + i]);
            }
            // float, gray
            {
                var source = CreateCpuImageFloatA();
                var dest = CreateCpuImageFloatA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();

                CpuImaging.FlipX(source, dest);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        Assert.AreEqual(source.HostBuffer[y * source.Width + x], dest.HostBuffer[y * dest.Width + source.Width - x - 1]);
            }

            // float, RGBA
            {
                var source = CreateCpuImageFloatRgbA();
                var dest = CreateCpuImageFloatRgbA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();

                CpuImaging.FlipX(source, dest);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        for (int i = 0; i < 4; i++)
                            Assert.AreEqual(source.HostBuffer[4 * (y * source.Width + x) + i], dest.HostBuffer[4 * (y * dest.Width + source.Width - x - 1) + i]);
            }
        }

        /// <summary>
        /// Testing flipping image on Y axis
        /// </summary>
        [TestMethod]
        public void CpuExecuteFlipY()
        {
            // byte, gray
            {
                var source = CreateCpuImageByteA();
                var dest = CreateCpuImageByteA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();

                CpuImaging.FlipY(source, dest);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        Assert.AreEqual(source.HostBuffer[y * source.Width + x], dest.HostBuffer[(source.Height - y - 1) * dest.Width + x]);
            }

            // byte, RGBA
            {
                var source = CreateCpuImageByteRgbA();
                var dest = CreateCpuImageByteRgbA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();

                CpuImaging.FlipY(source, dest);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        for (int i = 0; i < 4; i++)
                            Assert.AreEqual(source.HostBuffer[4 * (y * source.Width + x) + i], dest.HostBuffer[4 * ((source.Height - y - 1) * dest.Width + x) + i]);
            }
            // float, gray
            {
                var source = CreateCpuImageFloatA();
                var dest = CreateCpuImageFloatA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();

                CpuImaging.FlipY(source, dest);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        Assert.AreEqual(source.HostBuffer[y * source.Width + x], dest.HostBuffer[(source.Height - y - 1) * dest.Width + x]);
            }

            // float, RGBA
            {
                var source = CreateCpuImageFloatRgbA();
                var dest = CreateCpuImageFloatRgbA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();

                CpuImaging.FlipY(source, dest);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        for (int i = 0; i < 4; i++)
                            Assert.AreEqual(source.HostBuffer[4 * (y * source.Width + x) + i], dest.HostBuffer[4 * ((source.Height - y - 1) * dest.Width + x) + i]);
            }
        }

        /// <summary>
        /// Testing to add a constant float value to all cells
        /// </summary>
        [TestMethod]
        public void CpuExecuteIntegral()
        {
            // byte -> uint
            {
                var source = CreateCpuImageByteA();
                source.HostBuffer.Clear(1);
                var dest = CreateCpuImageUIntA(source.Width, source.Height);

                CpuImaging.Integral(source, dest);

                // check if add operation processed successfully
                Assert.AreNotEqual(0, dest.HostBuffer.Length);
                Assert.AreEqual((uint)((dest.Width - 1) * (dest.Height - 1)), dest.HostBuffer[dest.HostBuffer.Length - 1]);
                // we skip the last line to keep the original size (better performance)
            }
            // float
            {
                var source = CreateCpuImageFloatA();
                source.HostBuffer.Clear(1f);
                var dest = CreateCpuImageFloatA(source.Width, source.Height);

                CpuImaging.Integral(source, dest);

                // check if add operation processed successfully
                Assert.AreNotEqual(0, dest.HostBuffer.Length);
                Assert.AreEqual((dest.Width - 1) * (dest.Height - 1), dest.HostBuffer[dest.HostBuffer.Length - 1]);
                // we skip the last line to keep the original size (better performance)
            }
        }

        /// <summary>
        /// Testing to add a constant float value to all cells
        /// </summary>
        [TestMethod]
        public void CpuExecuteIntegralSquare()
        {
            // byte -> uint
            {
                var source = CreateCpuImageByteA();
                source.HostBuffer.Clear(1);
                var dest = CreateCpuImageUIntA(source.Width, source.Height);

                CpuImaging.IntegralSquare(source, dest);

                // check if add operation processed successfully
                Assert.AreNotEqual(0, dest.HostBuffer.Length);
                Assert.AreEqual((uint)((dest.Width - 1) * (dest.Height - 1)), dest.HostBuffer[dest.HostBuffer.Length - 1]);
                // we skip the last line to keep the original size (better performance)

                source.HostBuffer.Clear(2);
                CpuImaging.IntegralSquare(source, dest);
                Assert.AreNotEqual(0, dest.HostBuffer.Length);
                Assert.AreEqual((uint)(4 * (dest.Width - 1) * (dest.Height - 1)), dest.HostBuffer[dest.HostBuffer.Length - 1]);

                source.HostBuffer.Clear(3);
                CpuImaging.IntegralSquare(source, dest);
                Assert.AreNotEqual(0, dest.HostBuffer.Length);
                Assert.AreEqual((uint)(9 * (dest.Width - 1) * (dest.Height - 1)), dest.HostBuffer[dest.HostBuffer.Length - 1]);
            }

            // float
            {
                var source = CreateCpuImageFloatA();
                source.HostBuffer.Clear(1f);
                var dest = CreateCpuImageFloatA(source.Width, source.Height);

                CpuImaging.IntegralSquare(source, dest);

                // check if add operation processed successfully
                Assert.AreNotEqual(0, dest.HostBuffer.Length);
                Assert.AreEqual((dest.Width - 1) * (dest.Height - 1), dest.HostBuffer[dest.HostBuffer.Length - 1]);
                // we skip the last line to keep the original size (better performance)

                source.HostBuffer.Clear(2f);
                CpuImaging.IntegralSquare(source, dest);
                Assert.AreNotEqual(0, dest.HostBuffer.Length);
                Assert.AreEqual(4 * (dest.Width - 1) * (dest.Height - 1), dest.HostBuffer[dest.HostBuffer.Length - 1]);

                source.HostBuffer.Clear(3f);
                CpuImaging.IntegralSquare(source, dest);
                Assert.AreNotEqual(0, dest.HostBuffer.Length);
                Assert.AreEqual(9 * (dest.Width - 1) * (dest.Height - 1), dest.HostBuffer[dest.HostBuffer.Length - 1]);
            }
        }
    }
}
