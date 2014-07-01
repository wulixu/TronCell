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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cloo;
using OpenClooVision.Imaging;

namespace OpenClooVision.Kernels.Imaging
{
    /// <summary>
    /// Unit tests for ClooProgramImaging
    /// </summary>
    [TestClass]
    public class ClooProgramImagingTest : BaseTest
    {
        /// <summary>
        /// Testing if making all absolute value cells works
        /// </summary>
        [TestMethod]
        public void ClooExecuteAbs()
        {
            // gray, float
            {
                var image = CreateClooImageFloatA(100, 100);

                for (int i = 0; i < image.HostBuffer.Length; i++)
                    image.HostBuffer[i] = (i % 2 == 0) ? -1 : 1;

                // check if sum is zero, as -1 and 1 are alternating
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));
                image.WriteToDevice(Queue);

                ClooImaging.Abs(Queue, image, image);

                // we haven't read from device yet, so it still must be 0
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));

                image.ReadFromDevice(Queue);
                Assert.AreEqual(image.HostBuffer.Length, image.HostBuffer.Sum(x => x));
            }

            // RGBA, float
            {
                var image = CreateClooImageFloatRgbA(100, 100);

                for (int i = 0; i < image.HostBuffer.Length; i++)
                    image.HostBuffer[i] = (i % 2 == 0) ? -1 : 1;

                // check if sum is zero, as -1 and 1 are alternating
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));
                image.WriteToDevice(Queue);

                ClooImaging.Abs(Queue, image, image);

                // we haven't read from device yet, so it still must be 0
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));

                image.ReadFromDevice(Queue);
                Assert.AreEqual(image.HostBuffer.Length, image.HostBuffer.Sum(x => x));
            }
        }

        /// <summary>
        /// Testing to add a constant float value to all cells
        /// </summary>
        [TestMethod]
        public void ClooExecuteAddValue()
        {
            // gray, float
            {
                var image = CreateClooImageFloatA();
                var sum = image.HostBuffer.Sum(x => x);

                ClooImaging.AddValue(Queue, image, image, 1f);

                // we haven't read from device yet, so sum should be the same
                Assert.AreEqual(sum, image.HostBuffer.Sum(x => x));
                image.ReadFromDevice(Queue);

                // check if add operation processed successfully
                Assert.AreEqual(sum + 1f * image.HostBuffer.Length, image.HostBuffer.Sum(x => x));
            }

            // RGBA, float
            {
                var image = CreateClooImageFloatRgbA();
                var sum = image.HostBuffer.Sum(x => x);

                ClooImaging.AddValue(Queue, image, image, 1f);

                // we haven't read from device yet, so sum should be the same
                Assert.AreEqual(sum, image.HostBuffer.Sum(x => x));
                image.ReadFromDevice(Queue);

                // check if add operation processed successfully
                Assert.AreEqual(sum + 1f * image.HostBuffer.Length, image.HostBuffer.Sum(x => x));
            }
        }

        /// <summary>
        /// Testing BoxBlur
        /// </summary>
        [TestMethod]
        public void ClooExecuteBoxBlur()
        {
            // TODO: Testing BoxBlur
        }

        /// <summary>
        /// Testing ByteAToByteRgbA
        /// </summary>
        [TestMethod]
        public void ClooExecuteByteAToByteRgbA()
        {
            var source = CreateClooImageByteA();

            // create empty destination
            var dest = CreateClooImageByteRgbA(source.Width, source.Height);
            dest.HostBuffer.Clear();
            dest.WriteToDevice(Queue);
            Assert.AreEqual(0, dest.HostBuffer.Sum(x => x));
            dest.ReadFromDevice(Queue);
            Assert.AreEqual(0, dest.HostBuffer.Sum(x => x));

            ClooImaging.ByteAToByteRgbA(Queue, source, dest);
            Assert.AreEqual(0, dest.HostBuffer.Sum(x => x));
            dest.ReadFromDevice(Queue);
            Assert.AreEqual(source.HostBuffer.Sum(x => x) * 3 + 255 * source.HostBuffer.Length, dest.HostBuffer.Sum(x => x));
        }

        /// <summary>
        /// Testing ByteToFloat
        /// </summary>
        [TestMethod]
        public void ClooExecuteByteToFloat()
        {
            // gray
            {
                var source = CreateClooImageByteA();
                var dest = CreateClooImageFloatA(source.Width, source.Height);
                dest.HostBuffer.Clear();
                dest.WriteToDevice(Queue);
                Assert.AreEqual(0, dest.HostBuffer.Sum(x => x));
                dest.ReadFromDevice(Queue);
                Assert.AreEqual(0, dest.HostBuffer.Sum(x => x));

                ClooImaging.ByteToFloat(Queue, source, dest);
                Assert.AreEqual(0, dest.HostBuffer.Sum(x => x));
                dest.ReadFromDevice(Queue);
                Assert.AreEqual((float)source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));
            }

            // RGBA
            {
                var source = CreateClooImageByteRgbA();
                var dest = CreateClooImageFloatRgbA(source.Width, source.Height);
                dest.HostBuffer.Clear();
                dest.WriteToDevice(Queue);
                Assert.AreEqual(0, dest.HostBuffer.Sum(x => x));
                dest.ReadFromDevice(Queue);
                Assert.AreEqual(0, dest.HostBuffer.Sum(x => x));

                ClooImaging.ByteToFloat(Queue, source, dest);
                Assert.AreEqual(0, dest.HostBuffer.Sum(x => x));
                dest.ReadFromDevice(Queue);
                Assert.AreEqual((float)source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));
            }
        }

        /// <summary>
        /// Testing clamping all cell values to minimum and maximum value
        /// </summary>
        [TestMethod]
        public void ClooExecuteClamp()
        {
            // gray, float
            {
                var image = CreateClooImageFloatA(100, 100);

                for (int i = 0; i < image.HostBuffer.Length; i++)
                {
                    image.HostBuffer[i++] = -(i / 2);
                    image.HostBuffer[i] = (i / 2);
                }

                // check minimum and maximum value
                Assert.AreEqual(-image.HostBuffer.Length / 2 + 1, image.HostBuffer.Min(x => x));
                Assert.AreEqual(image.HostBuffer.Length / 2 - 1, image.HostBuffer.Max(x => x));

                // check if sum is zero, as values are alternating and symmetric
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));
                image.WriteToDevice(Queue);

                ClooImaging.Clamp(Queue, image, image, 0, 1);

                // we haven't read from device yet, so it still must be 0
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));

                image.ReadFromDevice(Queue);
                Assert.AreEqual(0, image.HostBuffer.Min(x => x));
                Assert.AreEqual(1, image.HostBuffer.Max(x => x));
                Assert.AreEqual(image.HostBuffer.Length / 2f - 1, image.HostBuffer.Sum(x => x));
            }

            // RGBA, float
            {
                var image = CreateClooImageFloatRgbA(100, 100);

                for (int i = 0; i < image.HostBuffer.Length; i++)
                {
                    image.HostBuffer[i++] = -(i / 2);
                    image.HostBuffer[i] = (i / 2);
                }

                // check minimum and maximum value
                Assert.AreEqual(-image.HostBuffer.Length / 2 + 1, image.HostBuffer.Min(x => x));
                Assert.AreEqual(image.HostBuffer.Length / 2 - 1, image.HostBuffer.Max(x => x));

                // check if sum is zero, as values are alternating and symmetric
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));
                image.WriteToDevice(Queue);

                ClooImaging.Clamp(Queue, image, image, 0, 1);

                // we haven't read from device yet, so it still must be 0
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));

                image.ReadFromDevice(Queue);
                Assert.AreEqual(0, image.HostBuffer.Min(x => x));
                Assert.AreEqual(1, image.HostBuffer.Max(x => x));
                Assert.AreEqual(image.HostBuffer.Length / 2f - 1, image.HostBuffer.Sum(x => x));
            }
        }

        /// <summary>
        /// Testing Denormalize
        /// </summary>
        [TestMethod]
        public void ClooExecuteDenormalize()
        {
            // gray, float
            {
                var image = CreateClooImageFloatA();

                // set random value between 0 and 1
                ResetRandomSeed();
                for (int i = 0; i < image.HostBuffer.Length; i++)
                    image.HostBuffer[i] = (float)Random.NextDouble();
                image.Normalized = true;
                image.WriteToDevice(Queue);

                Assert.IsTrue(image.HostBuffer.Min(x => x) >= 0);
                Assert.IsTrue(image.HostBuffer.Max(x => x) <= 1);

                ClooImaging.Denormalize(Queue, image, image);
                image.ReadFromDevice(Queue);

                Assert.IsFalse(image.Normalized);
                Assert.IsFalse(image.HostBuffer.Max(x => x) <= 1);
                Assert.IsTrue(image.HostBuffer.Min(x => x) >= 0);
                Assert.IsTrue(image.HostBuffer.Max(x => x) <= 255);
                Assert.IsTrue(image.HostBuffer.Max(x => x) > 128);
            }

            // RGBA, float
            {
                var image = CreateClooImageFloatRgbA();

                // set random value between 0 and 1
                ResetRandomSeed();
                for (int i = 0; i < image.HostBuffer.Length; i++)
                    image.HostBuffer[i] = (float)Random.NextDouble();
                image.Normalized = true;
                image.WriteToDevice(Queue);

                Assert.IsTrue(image.HostBuffer.Min(x => x) >= 0);
                Assert.IsTrue(image.HostBuffer.Max(x => x) <= 1);

                ClooImaging.Denormalize(Queue, image, image);
                image.ReadFromDevice(Queue);

                Assert.IsFalse(image.Normalized);
                Assert.IsFalse(image.HostBuffer.Max(x => x) <= 1);
                Assert.IsTrue(image.HostBuffer.Min(x => x) >= 0);
                Assert.IsTrue(image.HostBuffer.Max(x => x) <= 255);
                Assert.IsTrue(image.HostBuffer.Max(x => x) > 128);
            }
        }

        /// <summary>
        /// Testing ExtractChannel
        /// </summary>
        [TestMethod]
        public void ClooExecuteExtractChannel()
        {
            // byte
            {
                var source = CreateClooImageByteRgbA();
                var dest = CreateClooImageByteA(source.Width, source.Height);

                float totalSum = 0;
                for (byte offset = 0; offset < 4; offset++)
                {
                    ClooImaging.ExtractChannel(Queue, source, dest, offset);
                    dest.ReadFromDevice(Queue);

                    Assert.AreNotEqual(0, dest.HostBuffer.Sum(x => x));

                    int sum = dest.HostBuffer.Sum(x => x); totalSum += sum;
                    int index = 0;
                    Assert.AreEqual(source.HostBuffer.Sum(x => { return (index++ % 4 == offset) ? x : 0; }), sum);
                }

                Assert.AreEqual(totalSum, source.HostBuffer.Sum(x => x));
            }

            // float
            {
                var source = CreateClooImageFloatRgbA();
                var dest = CreateClooImageFloatA(source.Width, source.Height);

                float totalSum = 0;
                for (byte offset = 0; offset < 4; offset++)
                {
                    ClooImaging.ExtractChannel(Queue, source, dest, offset);
                    dest.ReadFromDevice(Queue);

                    Assert.AreNotEqual(0, dest.HostBuffer.Sum(x => x));

                    float sum = dest.HostBuffer.Sum(x => x); totalSum += sum;
                    int index = 0;
                    Assert.AreEqual(source.HostBuffer.Sum(x => { return (index++ % 4 == offset) ? x : 0; }), sum);
                }

                Assert.AreEqual(totalSum, source.HostBuffer.Sum(x => x));
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
                var source = CreateClooImageByteA();
                var dest = CreateClooImageByteA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();
                dest.WriteToDevice(Queue);

                ClooImaging.FlipX(Queue, source, dest);
                dest.ReadFromDevice(Queue);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        Assert.AreEqual(source.HostBuffer[y * source.Width + x], dest.HostBuffer[y * dest.Width + source.Width - x - 1]);
            }

            // byte, RGBA
            {
                var source = CreateClooImageByteRgbA();
                var dest = CreateClooImageByteRgbA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();
                dest.WriteToDevice(Queue);

                ClooImaging.FlipX(Queue, source, dest);
                dest.ReadFromDevice(Queue);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        for (int i = 0; i < 4; i++)
                            Assert.AreEqual(source.HostBuffer[4 * (y * source.Width + x) + i], dest.HostBuffer[4 * (y * dest.Width + source.Width - x - 1) + i]);
            }
            // float, gray
            {
                var source = CreateClooImageFloatA();
                var dest = CreateClooImageFloatA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();
                dest.WriteToDevice(Queue);

                ClooImaging.FlipX(Queue, source, dest);
                dest.ReadFromDevice(Queue);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        Assert.AreEqual(source.HostBuffer[y * source.Width + x], dest.HostBuffer[y * dest.Width + source.Width - x - 1]);
            }

            // float, RGBA
            {
                var source = CreateClooImageFloatRgbA();
                var dest = CreateClooImageFloatRgbA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();
                dest.WriteToDevice(Queue);

                ClooImaging.FlipX(Queue, source, dest);
                dest.ReadFromDevice(Queue);
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
        public void ClooExecuteFlipY()
        {
            // byte, gray
            {
                var source = CreateClooImageByteA();
                var dest = CreateClooImageByteA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();
                dest.WriteToDevice(Queue);

                ClooImaging.FlipY(Queue, source, dest);
                dest.ReadFromDevice(Queue);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        Assert.AreEqual(source.HostBuffer[y * source.Width + x], dest.HostBuffer[(source.Height - y - 1) * dest.Width + x]);
            }

            // byte, RGBA
            {
                var source = CreateClooImageByteRgbA();
                var dest = CreateClooImageByteRgbA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();
                dest.WriteToDevice(Queue);

                ClooImaging.FlipY(Queue, source, dest);
                dest.ReadFromDevice(Queue);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        for (int i = 0; i < 4; i++)
                            Assert.AreEqual(source.HostBuffer[4 * (y * source.Width + x) + i], dest.HostBuffer[4 * ((source.Height - y - 1) * dest.Width + x) + i]);
            }
            // float, gray
            {
                var source = CreateClooImageFloatA();
                var dest = CreateClooImageFloatA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();
                dest.WriteToDevice(Queue);

                ClooImaging.FlipY(Queue, source, dest);
                dest.ReadFromDevice(Queue);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        Assert.AreEqual(source.HostBuffer[y * source.Width + x], dest.HostBuffer[(source.Height - y - 1) * dest.Width + x]);
            }

            // float, RGBA
            {
                var source = CreateClooImageFloatRgbA();
                var dest = CreateClooImageFloatRgbA(source.Width + 1, source.Height + 1);
                dest.HostBuffer.Clear();
                dest.WriteToDevice(Queue);

                ClooImaging.FlipY(Queue, source, dest);
                dest.ReadFromDevice(Queue);
                Assert.AreEqual(source.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x));

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                        for (int i = 0; i < 4; i++)
                            Assert.AreEqual(source.HostBuffer[4 * (y * source.Width + x) + i], dest.HostBuffer[4 * ((source.Height - y - 1) * dest.Width + x) + i]);
            }
        }

        /// <summary>
        /// Testing GrayScale
        /// </summary>
        [TestMethod]
        public void ClooExecuteGrayScale()
        {
            var source = CreateClooImageFloatRgbA();
            var dest = CreateClooImageFloatA(source.Width, source.Height);

            ClooImaging.GrayScale(Queue, source, dest);
            dest.ReadFromDevice(Queue);
            Assert.AreNotEqual(0, dest.HostBuffer.Sum(x => x));

            // a randomly colored image should not have the same gray values in any RGB channel
            for (byte offset = 0; offset < 3; offset++)
            {
                int index = 0;
                Assert.AreNotEqual(source.HostBuffer.Sum(x => { return (index++ % 4 == offset) ? x : 0; }), dest.HostBuffer.Sum(x => x));
            }

            // now create grayscale image
            ResetRandomSeed();
            float totalSum = 0;
            for (int i = 0; i < source.HostBuffer.Length; i++)
            {
                float value = Random.Next(256);
                totalSum += value;
                source.HostBuffer[i++] = value;
                source.HostBuffer[i++] = value;
                source.HostBuffer[i++] = value;
                source.HostBuffer[i] = 255;
            }
            source.WriteToDevice(Queue);

            ClooImaging.GrayScale(Queue, source, dest);
            dest.ReadFromDevice(Queue);

            // round the values, since they might be a little inaccurate
            for (int i = 0; i < dest.HostBuffer.Length; i++)
                dest.HostBuffer[i] = (float)Math.Round(dest.HostBuffer[i]);

            Assert.AreEqual(totalSum, dest.HostBuffer.Sum(x => x));
        }

        /// <summary>
        /// Testing histogram 256
        /// </summary>
        [TestMethod]
        public void ClooExecuteHistogram256()
        {
            var gpuHist = new ClooBuffer<uint>(Context, Cloo.ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.AllocateHostPointer, 256);
            var cpuHist = new CpuBuffer<uint>(256);

            var gpuImage = CreateClooImageByteA(100, 100);
            ClooImaging.Histogram256(Queue, gpuImage, gpuHist);
            gpuHist.ReadFromDevice(Queue);

            // compare GPU histogram with CPU version
            CpuImaging.Histogram256(gpuImage, cpuHist);
            Assert.AreNotEqual(0, cpuHist.HostBuffer.Sum(x => x));
            Assert.AreEqual(cpuHist.HostBuffer.Sum(x => x), gpuHist.HostBuffer.Sum(x => x));

            // check each value
            for (int i = 0; i < cpuHist.HostBuffer.Length; i++)
                Assert.AreEqual(cpuHist.HostBuffer[i], gpuHist.HostBuffer[i], "Histogram comparison failed at index " + i);
        }

        /// <summary>
        /// Testing histogram with N bins
        /// </summary>
        [TestMethod]
        public void ClooExecuteHistogramN()
        {
            var gpuHist = new ClooBuffer<uint>(Context, Cloo.ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.AllocateHostPointer, 64);
            var cpuHist = new CpuBuffer<uint>(64);

            var gpuImage = CreateClooImageByteRgbA(100, 100);
            ClooImaging.HistogramN(Queue, gpuImage, gpuHist, 4);
            gpuHist.ReadFromDevice(Queue);

            // compare GPU histogram with CPU version
            CpuImaging.HistogramN(gpuImage, cpuHist, 4);
            Assert.AreNotEqual(0, cpuHist.HostBuffer.Sum(x => x));
            Assert.AreEqual(cpuHist.HostBuffer.Sum(x => x), gpuHist.HostBuffer.Sum(x => x));

            // check each value
            for (int i = 0; i < cpuHist.HostBuffer.Length; i++)
                Assert.AreEqual(cpuHist.HostBuffer[i], gpuHist.HostBuffer[i], "Histogram comparison failed at index " + i);
        }

        /// <summary>
        /// Testing histogram backprojection
        /// </summary>
        [TestMethod]
        public void ClooExecuteHistogramBackprojection()
        {
            var gpuHist = new ClooBuffer<uint>(Context, Cloo.ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.AllocateHostPointer, 64);

            var srcImage = CreateClooImageByteRgbA(100, 100);
            var dstImage = CreateClooImageByteA(100, 100);
            srcImage.HostBuffer.Clear();
            srcImage.WriteToDevice(Queue);
            ClooImaging.HistogramN(Queue, srcImage, gpuHist, 4);
            gpuHist.ReadFromDevice(Queue);

            ClooImaging.HistogramBackprojection(Queue, srcImage, dstImage, gpuHist, gpuHist, 4);
            dstImage.ReadFromDevice(Queue);

            // check each value
            for (int i = 0; i < dstImage.HostBuffer.Length; i++)
                Assert.AreEqual((byte)255, dstImage.HostBuffer[i], "Destination image mismatched probability at index " + i);

            // TODO: add more tests here
        }

        /// <summary>
        /// Testing HslToRgb
        /// </summary>
        [TestMethod]
        public void ClooExecuteHslToRgb()
        {
            // byte
            {
                var source = CreateClooImageByteRgbA(3, 1);

                int index = 0;

                // red
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 255;
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 0;

                // green
                source.HostBuffer[index++] = 85;
                source.HostBuffer[index++] = 255;
                source.HostBuffer[index++] = 63;
                source.HostBuffer[index++] = 0;

                // gray
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 0;

                source.WriteToDevice(Queue);

                ClooImaging.HslToRgb(Queue, source, source);
                source.ReadFromDevice(Queue);

                // red in RGB
                Assert.AreEqual(254, source.HostBuffer[0]);
                Assert.AreEqual(0, source.HostBuffer[1]);
                Assert.AreEqual(0, source.HostBuffer[2]);

                // green in HSL
                Assert.AreEqual(0, source.HostBuffer[4]);
                Assert.AreEqual(126, source.HostBuffer[5]);
                Assert.AreEqual(0, source.HostBuffer[6]);

                // gray in HSL
                Assert.AreEqual(127, source.HostBuffer[8]);
                Assert.AreEqual(127, source.HostBuffer[9]);
                Assert.AreEqual(127, source.HostBuffer[10]);
            }

            // float
            {
                var source = CreateClooImageFloatRgbA(3, 1);

                int index = 0;

                // red
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 255;
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 0;

                // green
                source.HostBuffer[index++] = 85;
                source.HostBuffer[index++] = 255;
                source.HostBuffer[index++] = 63;
                source.HostBuffer[index++] = 0;

                // gray
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 0;

                source.WriteToDevice(Queue);

                ClooImaging.HslToRgb(Queue, source, source);
                source.ReadFromDevice(Queue);

                // red in RGB
                Assert.AreEqual(254, (int)source.HostBuffer[0]);
                Assert.AreEqual(0, (int)source.HostBuffer[1]);
                Assert.AreEqual(0, (int)source.HostBuffer[2]);

                // green in RGB
                Assert.AreEqual(0, (int)source.HostBuffer[4]);
                Assert.AreEqual(126, (int)source.HostBuffer[5]);
                Assert.AreEqual(0, (int)source.HostBuffer[6]);

                // gray in RGB
                Assert.AreEqual(127, (int)source.HostBuffer[8]);
                Assert.AreEqual(127, (int)source.HostBuffer[9]);
                Assert.AreEqual(127, (int)source.HostBuffer[10]);
            }
        }

        /// <summary>
        /// Testing to add a constant float value to all cells
        /// </summary>
        [TestMethod]
        public void ClooExecuteIntegral()
        {
            var source = CreateClooImageFloatA();
            source.HostBuffer.Clear(1f);
            source.WriteToDevice(Queue);
            var dest = CreateClooImageFloatA(source.Width, source.Height);

            ClooImaging.Integral(Queue, source, dest);
            dest.ReadFromDevice(Queue);

            // check if add operation processed successfully
            Assert.AreNotEqual(0, dest.HostBuffer.Length);
            Assert.AreEqual((dest.Width - 1) * (dest.Height - 1), dest.HostBuffer[dest.HostBuffer.Length - 1]);
            // we skip the last line to keep the original size (better performance)

            // compare random values with CPU implementation
            ResetRandomSeed();
            for (int i = 0; i < source.HostBuffer.Length; i++)
                source.HostBuffer[i] = Random.Next(255);
            source.WriteToDevice(Queue);
            ClooImaging.Integral(Queue, source, dest);
            dest.ReadFromDevice(Queue);
            var cpu = CreateCpuImageFloatA(source.Width, source.Height);
            CpuImaging.Integral(source, cpu);
            Assert.AreNotEqual(0, cpu.HostBuffer.Sum(x => x));
            Assert.AreEqual(dest.HostBuffer.Sum(x => x), cpu.HostBuffer.Sum(x => x));
        }

        /// <summary>
        /// Testing to add a constant float value to all cells
        /// </summary>
        [TestMethod]
        public void ClooExecuteIntegralSquare()
        {
            var source = CreateClooImageFloatA();
            var dest = CreateClooImageFloatA(source.Width, source.Height);

            source.HostBuffer.Clear(1f);
            source.WriteToDevice(Queue);

            ClooImaging.Integral(Queue, source, dest);
            dest.ReadFromDevice(Queue);

            // check if add operation processed successfully
            Assert.AreNotEqual(0, dest.HostBuffer.Length);
            Assert.AreEqual((dest.Width - 1) * (dest.Height - 1), dest.HostBuffer[dest.HostBuffer.Length - 1]);
            // we skip the last line to keep the original size (better performance)

            source.HostBuffer.Clear(2f);
            source.WriteToDevice(Queue);
            ClooImaging.IntegralSquare(Queue, source, dest);
            dest.ReadFromDevice(Queue);
            Assert.AreNotEqual(0, dest.HostBuffer.Length);
            Assert.AreEqual(4 * (dest.Width - 1) * (dest.Height - 1), dest.HostBuffer[dest.HostBuffer.Length - 1]);

            // compare random values with CPU implementation
            for (int i = 0; i < source.HostBuffer.Length; i++)
                source.HostBuffer[i] = i;
            source.WriteToDevice(Queue);
            ClooImaging.IntegralSquare(Queue, source, dest);
            dest.ReadFromDevice(Queue);
            var cpu = CreateCpuImageFloatA(source.Width, source.Height);
            CpuImaging.IntegralSquare(source, cpu);
            Assert.AreNotEqual(0, cpu.HostBuffer.Sum(x => x));

            // floating point results may differ a bit between GPU and CPU
            Assert.AreEqual(cpu.HostBuffer.Sum(x => x), dest.HostBuffer.Sum(x => x), 0.00001f * dest.HostBuffer.Sum(x => x));
        }

        /// <summary>
        /// Testing MultiplyValue
        /// </summary>
        [TestMethod]
        public void ClooExecuteMultiplyValue()
        {
            // gray, float
            {
                var image = CreateClooImageFloatA();
                var sum = image.HostBuffer.Sum(x => x);

                ClooImaging.MultiplyValue(Queue, image, image, 1f);

                // sum should be the same
                image.ReadFromDevice(Queue);
                Assert.AreEqual(sum, image.HostBuffer.Sum(x => x));

                ClooImaging.MultiplyValue(Queue, image, image, 2f);

                // sum should be the same
                image.ReadFromDevice(Queue);
                Assert.AreEqual(2 * sum, image.HostBuffer.Sum(x => x));
            }

            // RGBA, float
            {
                var image = CreateClooImageFloatRgbA();
                var sum = image.HostBuffer.Sum(x => x);

                ClooImaging.MultiplyValue(Queue, image, image, 1f);

                // sum should be the same
                image.ReadFromDevice(Queue);
                Assert.AreEqual(sum, image.HostBuffer.Sum(x => x));

                ClooImaging.MultiplyValue(Queue, image, image, 2f);

                // sum should be the same
                image.ReadFromDevice(Queue);
                Assert.AreEqual(2 * sum, image.HostBuffer.Sum(x => x));
            }
        }

        /// <summary>
        /// Testing Normalize
        /// </summary>
        [TestMethod]
        public void ClooExecuteNormalize()
        {
            // gray, float
            {
                var image = CreateClooImageFloatA();
                image.WriteToDevice(Queue);

                Assert.IsFalse(image.Normalized);
                Assert.IsTrue(image.HostBuffer.Min(x => x) >= 0);
                Assert.IsTrue(image.HostBuffer.Max(x => x) > 128);
                Assert.IsTrue(image.HostBuffer.Max(x => x) <= 255);

                ClooImaging.Normalize(Queue, image, image);
                image.ReadFromDevice(Queue);

                Assert.IsTrue(image.Normalized);
                Assert.IsTrue(image.HostBuffer.Max(x => x) > 0.5);
                Assert.IsTrue(image.HostBuffer.Min(x => x) >= 0);
                Assert.IsTrue(image.HostBuffer.Max(x => x) <= 1);
            }

            // RGBA, float
            {
                var image = CreateClooImageFloatRgbA();
                image.WriteToDevice(Queue);

                Assert.IsFalse(image.Normalized);
                Assert.IsTrue(image.HostBuffer.Min(x => x) >= 0);
                Assert.IsTrue(image.HostBuffer.Max(x => x) > 128);
                Assert.IsTrue(image.HostBuffer.Max(x => x) <= 255);

                ClooImaging.Normalize(Queue, image, image);
                image.ReadFromDevice(Queue);

                Assert.IsTrue(image.Normalized);
                Assert.IsTrue(image.HostBuffer.Max(x => x) > 0.5);
                Assert.IsTrue(image.HostBuffer.Min(x => x) >= 0);
                Assert.IsTrue(image.HostBuffer.Max(x => x) <= 1);
            }
        }

        /// <summary>
        /// Testing RgbToHsl
        /// </summary>
        [TestMethod]
        public void ClooExecuteRgbToHsl()
        {
            // byte
            {
                var source = CreateClooImageByteRgbA(3, 1);

                int index = 0;

                // red
                source.HostBuffer[index++] = 255;
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 0;

                // green
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 0;

                // gray
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 0;

                source.WriteToDevice(Queue);

                ClooImaging.RgbToHsl(Queue, source, source);
                source.ReadFromDevice(Queue);

                // red in HSL
                Assert.AreEqual(0, source.HostBuffer[0]);
                Assert.AreEqual(255, source.HostBuffer[1]);
                Assert.AreEqual(127, source.HostBuffer[2]);

                // green in HSL
                Assert.AreEqual(85, source.HostBuffer[4]);
                Assert.AreEqual(255, source.HostBuffer[5]);
                Assert.AreEqual(63, source.HostBuffer[6]);

                // gray in HSL
                Assert.AreEqual(0, source.HostBuffer[8]);
                Assert.AreEqual(0, source.HostBuffer[9]);
                Assert.AreEqual(127, source.HostBuffer[10]);
            }

            // float
            {
                var source = CreateClooImageFloatRgbA(3, 1);

                int index = 0;

                // red
                source.HostBuffer[index++] = 255;
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 0;

                // green
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 0;
                source.HostBuffer[index++] = 0;

                // gray
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 127;
                source.HostBuffer[index++] = 0;

                source.WriteToDevice(Queue);

                ClooImaging.RgbToHsl(Queue, source, source);
                source.ReadFromDevice(Queue);

                // red in HSL
                Assert.AreEqual(0, (int)source.HostBuffer[0]);
                Assert.AreEqual(255, (int)source.HostBuffer[1]);
                Assert.AreEqual(127, (int)source.HostBuffer[2]);

                // green in HSL
                Assert.AreEqual(85, (int)source.HostBuffer[4]);
                Assert.AreEqual(255, (int)source.HostBuffer[5]);
                Assert.AreEqual(63, (int)source.HostBuffer[6]);

                // gray in HSL
                Assert.AreEqual(0, (int)source.HostBuffer[8]);
                Assert.AreEqual(0, (int)source.HostBuffer[9]);
                Assert.AreEqual(127, (int)source.HostBuffer[10]);
            }
        }

        /// <summary>
        /// Testing SetChannel
        /// </summary>
        [TestMethod]
        public void ClooExecuteSetChannel()
        {
            // byte, constant
            {
                var source = CreateClooImageByteRgbA();
                var dest = CreateClooImageByteRgbA(source.Width, source.Height);

                for (byte offset = 0; offset < 4; offset++)
                {
                    ClooImaging.SetChannel(Queue, source, dest, offset, 255);
                    dest.ReadFromDevice(Queue);

                    Assert.AreNotEqual(0, dest.HostBuffer.Sum(x => x));

                    int index = 0;
                    Assert.AreEqual(source.Width * source.Height * 255, dest.HostBuffer.Sum(x => { return (index++ % 4 == offset) ? x : 0; }));
                    Assert.AreNotEqual(source.Width * source.Height * 255 * 4, dest.HostBuffer.Sum(x => x));
                }
            }

            // float, constant
            {
                var source = CreateClooImageFloatRgbA();
                var dest = CreateClooImageFloatRgbA(source.Width, source.Height);

                for (byte offset = 0; offset < 4; offset++)
                {
                    ClooImaging.SetChannel(Queue, source, dest, offset, 255);
                    dest.ReadFromDevice(Queue);

                    Assert.AreNotEqual(0, dest.HostBuffer.Sum(x => x));

                    int index = 0;
                    Assert.AreEqual(source.Width * source.Height * 255, dest.HostBuffer.Sum(x => { return (index++ % 4 == offset) ? x : 0; }));
                    Assert.AreNotEqual(source.Width * source.Height * 255 * 4, dest.HostBuffer.Sum(x => x));
                }
            }

            // byte, mask
            {
                var source = CreateClooImageByteRgbA();
                var mask = CreateClooImageByteA();
                var dest = CreateClooImageByteRgbA(source.Width, source.Height);

                mask.HostBuffer.Clear(255);
                mask.WriteToDevice(Queue);
                Assert.AreEqual(source.Width * source.Height * 255, mask.HostBuffer.Sum(x => x));

                for (byte offset = 0; offset < 4; offset++)
                {
                    ClooImaging.SetChannel(Queue, source, mask, dest, offset);
                    dest.ReadFromDevice(Queue);

                    Assert.AreNotEqual(0, dest.HostBuffer.Sum(x => x));

                    int index = 0;
                    Assert.AreEqual(source.Width * source.Height * 255, dest.HostBuffer.Sum(x => { return (index++ % 4 == offset) ? x : 0; }));
                    Assert.AreNotEqual(source.Width * source.Height * 255 * 4, dest.HostBuffer.Sum(x => x));
                }
            }

            // float, mask
            {
                var source = CreateClooImageFloatRgbA();
                var mask = CreateClooImageFloatA();
                var dest = CreateClooImageFloatRgbA(source.Width, source.Height);

                mask.HostBuffer.Clear(255);
                mask.WriteToDevice(Queue);
                Assert.AreEqual(source.Width * source.Height * 255, mask.HostBuffer.Sum(x => x));

                for (byte offset = 0; offset < 4; offset++)
                {
                    ClooImaging.SetChannel(Queue, source, mask, dest, offset);
                    dest.ReadFromDevice(Queue);

                    Assert.AreNotEqual(0, dest.HostBuffer.Sum(x => x));

                    int index = 0;
                    Assert.AreEqual(source.Width * source.Height * 255, dest.HostBuffer.Sum(x => { return (index++ % 4 == offset) ? x : 0; }));
                    Assert.AreNotEqual(source.Width * source.Height * 255 * 4, dest.HostBuffer.Sum(x => x));
                }
            }
        }

        /// <summary>
        /// Testing to set a constant float value to all cells
        /// </summary>
        [TestMethod]
        public void ClooExecuteSetValue()
        {
            // gray, float
            {
                var image = CreateClooImageFloatA();
                var sum = image.HostBuffer.Sum(x => x);
                Assert.AreNotEqual(0, sum);

                ClooImaging.SetValue(Queue, image, 0f);

                // we haven't read from device yet, so sum should be the same
                Assert.AreEqual(sum, image.HostBuffer.Sum(x => x));
                image.ReadFromDevice(Queue);

                // check if add operation processed successfully
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));

                ClooImaging.SetValue(Queue, image, 1f);
                image.ReadFromDevice(Queue);
                Assert.AreEqual(image.Width * image.Height, image.HostBuffer.Sum(x => x));
            }

            // RGBA, float
            {
                var image = CreateClooImageFloatRgbA();
                var sum = image.HostBuffer.Sum(x => x);
                Assert.AreNotEqual(0, sum);

                ClooImaging.SetValue(Queue, image, 0f);

                // we haven't read from device yet, so sum should be the same
                Assert.AreEqual(sum, image.HostBuffer.Sum(x => x));
                image.ReadFromDevice(Queue);

                // check if add operation processed successfully
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));

                ClooImaging.SetValue(Queue, image, 1f);
                image.ReadFromDevice(Queue);
                Assert.AreEqual(4 * image.Width * image.Height, image.HostBuffer.Sum(x => x));
            }

            // gray, byte
            {
                var image = CreateClooImageByteA();
                var sum = image.HostBuffer.Sum(x => x);
                Assert.AreNotEqual(0, sum);

                ClooImaging.SetValue(Queue, image, 0);

                // we haven't read from device yet, so sum should be the same
                Assert.AreEqual(sum, image.HostBuffer.Sum(x => x));
                image.ReadFromDevice(Queue);

                // check if add operation processed successfully
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));

                ClooImaging.SetValue(Queue, image, 1);
                image.ReadFromDevice(Queue);
                Assert.AreEqual(image.Width * image.Height, image.HostBuffer.Sum(x => x));
            }

            // RGBA, byte
            {
                var image = CreateClooImageByteRgbA();
                var sum = image.HostBuffer.Sum(x => x);
                Assert.AreNotEqual(0, sum);

                ClooImaging.SetValue(Queue, image, 0);

                // we haven't read from device yet, so sum should be the same
                Assert.AreEqual(sum, image.HostBuffer.Sum(x => x));
                image.ReadFromDevice(Queue);

                // check if add operation processed successfully
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));

                ClooImaging.SetValue(Queue, image, (byte)1);
                image.ReadFromDevice(Queue);
                Assert.AreEqual(4 * image.Width * image.Height, image.HostBuffer.Sum(x => x));
            }

            // RGBA, uint
            {
                var image = CreateClooImageByteRgbA();
                var sum = image.HostBuffer.Sum(x => x);
                Assert.AreNotEqual(0, sum);

                ClooImaging.SetValue(Queue, image, 0);

                // we haven't read from device yet, so sum should be the same
                Assert.AreEqual(sum, image.HostBuffer.Sum(x => x));
                image.ReadFromDevice(Queue);

                // check if add operation processed successfully
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));

                ClooImaging.SetValue(Queue, image, (uint)0x01020304);
                image.ReadFromDevice(Queue);
                Assert.AreEqual(10 * image.Width * image.Height, image.HostBuffer.Sum(x => x));
                Assert.AreEqual(1, image.HostBuffer[0]);
                Assert.AreEqual(2, image.HostBuffer[1]);
                Assert.AreEqual(3, image.HostBuffer[2]);
                Assert.AreEqual(4, image.HostBuffer[3]);
                Assert.AreEqual(1, image.HostBuffer[4]);
            }

            // gray, uint
            {
                var image = CreateClooImageUIntA();
                var sum = image.HostBuffer.Sum(x => x);
                Assert.AreNotEqual(0, sum);

                ClooImaging.SetValue(Queue, image, 0);

                // we haven't read from device yet, so sum should be the same
                Assert.AreEqual(sum, image.HostBuffer.Sum(x => x));
                image.ReadFromDevice(Queue);

                // check if add operation processed successfully
                Assert.AreEqual(0, image.HostBuffer.Sum(x => x));

                ClooImaging.SetValue(Queue, image, 1);
                image.ReadFromDevice(Queue);
                Assert.AreEqual(image.Width * image.Height, image.HostBuffer.Sum(x => x));
            }
        }

        /// <summary>
        /// Testing Sobel
        /// </summary>
        [TestMethod]
        public void ClooExecuteSobel()
        {
            // TODO: Testing Sobel
        }
    }
}
