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
using OpenClooVision.Imaging;
using OpenClooVision.Kernels.Imaging;

namespace OpenClooVision
{
    /// <summary>
    /// Base unit test class
    /// </summary>
    [TestClass]
    public class BaseTest
    {
        /// <summary>
        /// Currently selected Cloo device
        /// </summary>
        public static ClooDevice Device { get; protected set; }

        /// <summary>
        /// Cloo context
        /// </summary>
        public static ClooContext Context { get; protected set; }

        /// <summary>
        /// Cloo command queue
        /// </summary>
        public static ClooCommandQueue Queue { get; protected set; }

        /// <summary>
        /// To generate random numbers
        /// </summary>
        /// <remarks>
        /// Made the seed predictable based on current date, so there's a
        /// chance to uncover rare corner cases, you never know
        /// </remarks>
        public static Random Random { get; protected set; }

        /// <summary>
        /// Cloo program for imaging
        /// </summary>
        public static ClooProgramImaging ClooImaging { get; protected set; }

        /// <summary>
        /// CPU methods for imaging
        /// </summary>
        public static CpuProgramImaging CpuImaging { get; protected set; }

        /// <summary>
        /// Initialize Cloo context
        /// </summary>
        static BaseTest()
        {
            // select device with most compute units multiplied by the clock frequency, 
            // should be the fastest GPU in most cases ;-)
            Device = ClooDevice.CompatibleDevices.Where(x => x.Available).OrderByDescending(x => x.MaxClockFrequency * x.MaxComputeUnits).FirstOrDefault();
            if (Device == null) Assert.Inconclusive("Could not find any suitable OpenCL device in your PC");

            Context = Device.CreateContext();
            Queue = Context.CreateCommandQueue();
            ClooImaging = ClooProgramImaging.Create(Context);
            CpuImaging = CpuProgramImaging.Create();
        }

        /// <summary>
        /// Creates a ClooBuffer and fills it with random values
        /// </summary>
        /// <param name="length">buffer length</param>
        /// <returns></returns>
        public virtual ClooBuffer<byte> CreateClooBufferByte(int length = 9999)
        {
            ResetRandomSeed();
            var buffer = new ClooBuffer<byte>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.AllocateHostPointer, length);

            Assert.AreNotEqual(0, buffer.HostBuffer.Length);
            Assert.AreEqual(buffer.HostBuffer.Length, buffer.Size);

            for (int i = 0; i < length; i++)
                buffer.HostBuffer[i] = (byte)Random.Next(256);
            buffer.WriteToDevice(Queue);
            Assert.AreNotEqual(0, buffer.HostBuffer.Sum(x => x));

            return buffer;
        }

        /// <summary>
        /// Creates a ClooBuffer and fills it with random values
        /// </summary>
        /// <param name="length">buffer length</param>
        /// <returns></returns>
        public virtual ClooBuffer<float> CreateClooBufferFloat(int length = 9999)
        {
            ResetRandomSeed();
            var buffer = new ClooBuffer<float>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.AllocateHostPointer, length);

            Assert.AreNotEqual(0, buffer.HostBuffer.Length);
            Assert.AreEqual(4 * buffer.HostBuffer.Length, buffer.Size);

            for (int i = 0; i < length; i++)
                buffer.HostBuffer[i] = (float)Random.Next(256);
            buffer.WriteToDevice(Queue);
            Assert.AreNotEqual(0, buffer.HostBuffer.Sum(x => x));

            return buffer;
        }

        /// <summary>
        /// Creates a ClooBuffer and fills it with random values
        /// </summary>
        /// <param name="length">buffer length</param>
        /// <returns></returns>
        public virtual ClooBuffer<uint> CreateClooBufferUInt(int length = 9999)
        {
            ResetRandomSeed();
            var buffer = new ClooBuffer<uint>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.AllocateHostPointer, length);

            Assert.AreNotEqual(0, buffer.HostBuffer.Length);
            Assert.AreEqual(4 * buffer.HostBuffer.Length, buffer.Size);

            for (int i = 0; i < length; i++)
                buffer.HostBuffer[i] = (uint)Random.Next(256);
            buffer.WriteToDevice(Queue);
            Assert.AreNotEqual(0, buffer.HostBuffer.Sum(x => x));

            return buffer;
        }

        /// <summary>
        /// Creates a ClooImageByteA and fills it with random values
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <returns></returns>
        public virtual ClooImage2DByteA CreateClooImageByteA(int width = 99, int height = 99)
        {
            ResetRandomSeed();
            var image = ClooImage2DByteA.Create(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, width, height);

            Assert.AreNotEqual(0, image.HostBuffer.Length);
            Assert.AreEqual(image.HostBuffer.Length, image.Size);

            int length = image.HostBuffer.Length;
            for (int i = 0; i < length; i++)
                image.HostBuffer[i] = (byte)Random.Next(256);
            image.WriteToDevice(Queue);
            Assert.AreNotEqual(0, image.HostBuffer.Sum(x => x));

            return image;
        }

        /// <summary>
        /// Creates a ClooImageByteRgbA and fills it with random values
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <returns></returns>
        public virtual ClooImage2DByteRgbA CreateClooImageByteRgbA(int width = 99, int height = 99)
        {
            ResetRandomSeed();
            var image = ClooImage2DByteRgbA.Create(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, width, height);

            Assert.AreNotEqual(0, image.HostBuffer.Length);
            Assert.AreEqual(image.HostBuffer.Length, image.Size);

            int length = image.HostBuffer.Length;
            for (int i = 0; i < length; i++)
                image.HostBuffer[i] = (byte)Random.Next(256);
            image.WriteToDevice(Queue);
            Assert.AreNotEqual(0, image.HostBuffer.Sum(x => x));

            return image;
        }

        /// <summary>
        /// Creates a ClooImageFloatA and fills it with random values
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <returns></returns>
        public virtual ClooImage2DFloatA CreateClooImageFloatA(int width = 99, int height = 99)
        {
            ResetRandomSeed();
            var image = ClooImage2DFloatA.Create(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, width, height);

            Assert.AreNotEqual(0, image.HostBuffer.Length);
            Assert.AreEqual(4 * image.HostBuffer.Length, image.Size);

            int length = image.HostBuffer.Length;
            for (int i = 0; i < length; i++)
                image.HostBuffer[i] = Random.Next(256);
            image.WriteToDevice(Queue);
            Assert.AreNotEqual(0, image.HostBuffer.Sum(x => x));

            return image;
        }

        /// <summary>
        /// Creates a ClooImageFloatRgbA and fills it with random values
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <returns></returns>
        public virtual ClooImage2DFloatRgbA CreateClooImageFloatRgbA(int width = 99, int height = 99)
        {
            ResetRandomSeed();
            var image = ClooImage2DFloatRgbA.Create(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, width, height);

            Assert.AreNotEqual(0, image.HostBuffer.Length);
            Assert.AreEqual(4 * image.HostBuffer.Length, image.Size);

            int length = image.HostBuffer.Length;
            for (int i = 0; i < length; i++)
                image.HostBuffer[i] = Random.Next(256);
            image.WriteToDevice(Queue);
            Assert.AreNotEqual(0, image.HostBuffer.Sum(x => x));

            return image;
        }

        /// <summary>
        /// Creates a ClooImageUIntA and fills it with random values
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <returns></returns>
        public virtual ClooImage2DUIntA CreateClooImageUIntA(int width = 99, int height = 99)
        {
            ResetRandomSeed();
            var image = ClooImage2DUIntA.Create(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, width, height);

            Assert.AreNotEqual(0, image.HostBuffer.Length);
            Assert.AreEqual(4 * image.HostBuffer.Length, image.Size);

            int length = image.HostBuffer.Length;
            for (int i = 0; i < length; i++)
                image.HostBuffer[i] = (byte)Random.Next(256);
            image.WriteToDevice(Queue);
            Assert.AreNotEqual(0, image.HostBuffer.Sum(x => x));

            return image;
        }

        /// <summary>
        /// Creates a ClooBuffer and fills it with random values
        /// </summary>
        /// <param name="length">buffer length</param>
        /// <returns></returns>
        public virtual CpuBuffer<byte> CreateCpuBufferByte(int length = 9999)
        {
            ResetRandomSeed();
            var buffer = new CpuBuffer<byte>(length);

            Assert.AreNotEqual(0, buffer.HostBuffer.Length);
            Assert.AreEqual(buffer.HostBuffer.Length, buffer.Size);

            for (int i = 0; i < length; i++)
                buffer.HostBuffer[i] = (byte)Random.Next(256);
            Assert.AreNotEqual(0, buffer.HostBuffer.Sum(x => x));

            return buffer;
        }

        /// <summary>
        /// Creates a ClooBuffer and fills it with random values
        /// </summary>
        /// <param name="length">buffer length</param>
        /// <returns></returns>
        public virtual CpuBuffer<float> CreateCpuBufferFloat(int length = 9999)
        {
            ResetRandomSeed();
            var buffer = new CpuBuffer<float>(length);

            Assert.AreNotEqual(0, buffer.HostBuffer.Length);
            Assert.AreEqual(4 * buffer.HostBuffer.Length, buffer.Size);

            for (int i = 0; i < length; i++)
                buffer.HostBuffer[i] = (float)Random.Next(256);
            Assert.AreNotEqual(0, buffer.HostBuffer.Sum(x => x));

            return buffer;
        }

        /// <summary>
        /// Creates a ClooBuffer and fills it with random values
        /// </summary>
        /// <param name="length">buffer length</param>
        /// <returns></returns>
        public virtual CpuBuffer<uint> CreateCpuBufferUInt(int length = 9999)
        {
            ResetRandomSeed();
            var buffer = new CpuBuffer<uint>(length);

            Assert.AreNotEqual(0, buffer.HostBuffer.Length);
            Assert.AreEqual(4 * buffer.HostBuffer.Length, buffer.Size);

            for (int i = 0; i < length; i++)
                buffer.HostBuffer[i] = (uint)Random.Next(256);
            Assert.AreNotEqual(0, buffer.HostBuffer.Sum(x => x));

            return buffer;
        }

        /// <summary>
        /// Creates a CpuImageByteA and fills it with random values
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <returns></returns>
        public virtual CpuImage2DByteA CreateCpuImageByteA(int width = 99, int height = 99)
        {
            ResetRandomSeed();
            var image = CpuImage2DByteA.Create(width, height);

            Assert.AreNotEqual(0, image.HostBuffer.Length);
            Assert.AreEqual(image.HostBuffer.Length, image.Size);

            int length = image.HostBuffer.Length;
            for (int i = 0; i < length; i++)
                image.HostBuffer[i] = (byte)Random.Next(256);
            Assert.AreNotEqual(0, image.HostBuffer.Sum(x => x));

            return image;
        }

        /// <summary>
        /// Creates a CpuImageByteRgbA and fills it with random values
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <returns></returns>
        public virtual CpuImage2DByteRgbA CreateCpuImageByteRgbA(int width = 99, int height = 99)
        {
            ResetRandomSeed();
            var image = CpuImage2DByteRgbA.Create(width, height);

            Assert.AreNotEqual(0, image.HostBuffer.Length);
            Assert.AreEqual(image.HostBuffer.Length, image.Size);

            int length = image.HostBuffer.Length;
            for (int i = 0; i < length; i++)
                image.HostBuffer[i] = (byte)Random.Next(256);
            Assert.AreNotEqual(0, image.HostBuffer.Sum(x => x));

            return image;
        }

        /// <summary>
        /// Creates a CpuImageFloatA and fills it with random values
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <returns></returns>
        public virtual CpuImage2DFloatA CreateCpuImageFloatA(int width = 99, int height = 99)
        {
            ResetRandomSeed();
            var image = CpuImage2DFloatA.Create(width, height);

            Assert.AreNotEqual(0, image.HostBuffer.Length);
            Assert.AreEqual(4 * image.HostBuffer.Length, image.Size);

            int length = image.HostBuffer.Length;
            for (int i = 0; i < length; i++)
                image.HostBuffer[i] = Random.Next(256);
            Assert.AreNotEqual(0, image.HostBuffer.Sum(x => x));

            return image;
        }

        /// <summary>
        /// Creates a CpuImageFloatRgbA and fills it with random values
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <returns></returns>
        public virtual CpuImage2DFloatRgbA CreateCpuImageFloatRgbA(int width = 99, int height = 99)
        {
            ResetRandomSeed();
            var image = CpuImage2DFloatRgbA.Create(width, height);

            Assert.AreNotEqual(0, image.HostBuffer.Length);
            Assert.AreEqual(4 * image.HostBuffer.Length, image.Size);

            int length = image.HostBuffer.Length;
            for (int i = 0; i < length; i++)
                image.HostBuffer[i] = Random.Next(256);
            Assert.AreNotEqual(0, image.HostBuffer.Sum(x => x));

            return image;
        }

        /// <summary>
        /// Creates a CpuImageUIntA and fills it with random values
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <returns></returns>
        public virtual CpuImage2DUIntA CreateCpuImageUIntA(int width = 99, int height = 99)
        {
            ResetRandomSeed();
            var image = CpuImage2DUIntA.Create(width, height);

            Assert.AreNotEqual(0, image.HostBuffer.Length);
            Assert.AreEqual(4 * image.HostBuffer.Length, image.Size);

            int length = image.HostBuffer.Length;
            for (int i = 0; i < length; i++)
                image.HostBuffer[i] = (byte)Random.Next(256);
            Assert.AreNotEqual(0, image.HostBuffer.Sum(x => x));

            return image;
        }

        /// <summary>
        /// Reset random seed
        /// </summary>
        /// <remarks>
        /// Made the seed predictable based on current date, so there's a
        /// chance to uncover rare corner cases, you never know
        /// </remarks>
        public virtual void ResetRandomSeed()
        {
            DateTime now = DateTime.Now;
            Random = new Random(now.Year * now.DayOfYear);
        }
    }
}
