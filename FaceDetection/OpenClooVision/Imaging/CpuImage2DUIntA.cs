﻿#region License
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
using System.Drawing;
using System.Drawing.Imaging;
using Cloo;

namespace OpenClooVision.Imaging
{
    /// <summary>
    /// Compute image 2D for CPU only (gray, uint)
    /// </summary>
    [CLSCompliant(false)]
    public class CpuImage2DUIntA : CpuImage2D<uint>, IImage2DUIntA
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <param name="data">image data</param>
        public CpuImage2DUIntA(int width, int height, uint[] data = null)
            : base(width, height, data)
        {
        }

        /// <summary>
        /// Create image
        /// </summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <param name="data">initial data</param>
        public static CpuImage2DUIntA Create(int width, int height, uint[] data = null)
        {
            if ((width < 1) || (height < 1)) throw new ArgumentException("OpenCL requires the image to have a width and height of at least one pixel", "image");

            CpuImage2DUIntA res = new CpuImage2DUIntA(width, height, data);
            return res;
        }

        /// <summary>
        /// Create image
        /// </summary>
        /// <param name="bitmap">bitmap</param>
        public static CpuImage2DUIntA Create(Bitmap bitmap)
        {
            CpuImage2DUIntA res = new CpuImage2DUIntA(bitmap.Width, bitmap.Height);
            BitmapToBuffer(bitmap, res._hostBuffer);
            return res;
        }

        /// <summary>
        /// Converts a bitmap to buffer
        /// </summary>
        /// <param name="bitmap">bitmap</param>
        /// <param name="data">buffer to put the data into</param>
        /// <returns>bitmap as buffer</returns>
        public static unsafe void BitmapToBuffer(Bitmap bitmap, uint[] data)
        {
            if (data.Length < bitmap.Width * bitmap.Height)
                throw new ArgumentException("Buffer size is too small for bitmap");

            fixed (uint* p = data)
            {
                BitmapData bitmapData = null;
                try
                {
                    bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                    byte* scan = (byte*)bitmapData.Scan0.ToPointer();
                    int nOffset = bitmapData.Stride - bitmap.Width * 3;

                    int pos = 0;
                    int width = bitmap.Width;
                    int height = bitmap.Height;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            p[pos++] = (byte)((float)scan[2] * 0.2989f + (float)scan[1] * 0.5870f + (float)scan[0] * 0.1140f);
                            scan += 3;
                        }
                        scan += nOffset;
                    }
                }
                finally
                {
                    if (bitmapData != null) bitmap.UnlockBits(bitmapData);
                }
            }
        }

        /// <summary>
        /// Converts a bitmap to buffer
        /// </summary>
        /// <param name="bitmap">bitmap</param>
        /// <returns>bitmap as buffer</returns>
        public static unsafe uint[] BitmapToBuffer(Bitmap bitmap)
        {
            uint[] data = new uint[bitmap.Width * bitmap.Height];
            BitmapToBuffer(bitmap, data);
            return data;
        }

        /// <summary>
        /// Creates a CpuImage2D from bitmap
        /// </summary>
        /// <param name="context">compute context</param>
        /// <param name="flags">memory flags</param>
        /// <param name="bitmap">bitmap</param>
        /// <returns>CpuImage2D</returns>
        /// <exception cref="ArgumentNullException">bitmap</exception>
        /// <exception cref="ArgumentException">OpenCL requires the image to have a width and height of at least one pixel</exception>
        public static unsafe CpuImage2DUIntA CreateFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException("image");
            if ((bitmap.Width < 1) || (bitmap.Height < 1)) throw new ArgumentException("OpenCL requires the image to have a width and height of at least one pixel", "image");

            uint[] data = BitmapToBuffer(bitmap);

            CpuImage2DUIntA res = new CpuImage2DUIntA(bitmap.Width, bitmap.Height, data);
            return res;
        }

        /// <summary>
        /// Creates a new managed bitmap
        /// </summary>
        /// <param name="bitmap">we already have a bitmap to put it in</param>
        /// <returns>managed bitmap</returns>
        public unsafe void ToBitmap(Bitmap bitmap)
        {
            BitmapData bitmapData = null;

            try
            {
                bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                byte* scan = (byte*)bitmapData.Scan0.ToPointer();
                int nOffset = bitmapData.Stride - bitmap.Width * 3;

                int pos = 0;
                int width = bitmap.Width;
                int height = bitmap.Height;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte val = (byte)_hostBuffer[pos++];
                        scan[2] = val;
                        scan[1] = val;
                        scan[0] = val;
                        scan += 3;
                    }
                    scan += nOffset;
                }
            }
            finally
            {
                if (bitmapData != null) bitmap.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// Creates a new managed bitmap
        /// </summary>
        /// <returns>managed bitmap</returns>
        public unsafe Bitmap ToBitmap()
        {
            Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            ToBitmap(bitmap);
            return bitmap;
        }
    }
}
