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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace OpenClooVision.WpfTest
{
    /// <summary>
    /// Common extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// FxCop requires all Marshalled functions to be in a class called NativeMethods.
        /// </summary>
        internal static class NativeMethods
        {
            [DllImport("gdi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DeleteObject(IntPtr hObject);
        }

        /// <summary>
        /// Converts a system drawing bitmap to BitmapImage
        /// </summary>
        /// <param name="bitmap">System.Drawing.Bitmap to convert</param>
        /// <returns>WPF BitmapImage</returns>
        public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap bitmap)
        {
            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                hBitmap = bitmap.GetHbitmap();
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                NativeMethods.DeleteObject(hBitmap);
            }
        }

        public static System.Drawing.Bitmap ToBitmap(this InteropBitmap img)
        {
            if (img == null) return null;
            int imgW = img.PixelWidth;
            int imgH = img.PixelHeight;
            System.Drawing.Bitmap bmp;
            bmp = new System.Drawing.Bitmap(imgW, imgH,
                                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            ToBitmap(img, bmp);
            return bmp;
        }

        public static void ToBitmap(this InteropBitmap img, System.Drawing.Bitmap bmp)
        {
            if (img == null) return;
            int imgW = img.PixelWidth;
            int imgH = img.PixelHeight;
            byte[] byte_arr = new byte[(int)(4 * img.PixelWidth * img.PixelHeight)];

            int stride = ((img.PixelWidth * img.Format.BitsPerPixel + 31) & ~31) >> 3;
            img.CopyPixels(byte_arr, stride, 0);

            System.Drawing.Imaging.BitmapData bData;

            //The Width and Height should be static don't bother depending on the 
            //InteropBitmap for them
            if (imgW == -1 || imgH == -1)
            {
                imgW = (int)img.PixelWidth;
                imgH = (int)img.PixelHeight;
            }

            bData = bmp.LockBits(new System.Drawing.Rectangle(new System.Drawing.Point(), bmp.Size),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, 
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Marshal.Copy(byte_arr, 0, bData.Scan0, byte_arr.Length);
            bmp.UnlockBits(bData);
        }
    }
}
