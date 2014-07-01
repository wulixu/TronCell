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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Cloo;
using Microsoft.Win32;
using OpenClooVision.Capture;
using OpenClooVision.Imaging;
using OpenClooVision.Kernels.ViolaJones;

namespace OpenClooVision.WpfTest
{
    /// <summary>
    /// Application main window
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Drawing.Bitmap _bitmapImage1;
        bool _firstActivated = true;

        ClooContext _context;
        ClooCommandQueue _queue;
        ClooDevice _selectedDevice;
        ClooProgramViolaJones _kernels;
        ClooSampler _sampler;
        ClooBuffer<uint> _histogram;
        ClooBuffer<uint> _histogram2;
        ClooImage2DByteRgbA _clooImageByteOriginal;
        ClooImage2DByteA _clooImageByteGrayOriginal;
        ClooImage2DByteRgbA _clooImageByteResult;
        ClooImage2DByteA _clooImageByteResultA;
        ClooImage2DFloatRgbA _clooImageFloatOriginal;
        ClooImage2DFloatRgbA _clooImageFloatTemp1;
        ClooImage2DFloatRgbA _clooImageFloatTemp2;
        ClooImage2DFloatA _clooImageFloatGrayOriginal;
        ClooImage2DFloatA _clooImageFloatATemp1;
        ClooImage2DFloatA _clooImageFloatATemp2;
        ClooImage2DFloatA _clooImageFloatIntegral;
        ClooImage2DUIntA _clooImageUIntIntegral;
        ClooImage2DUIntA _clooImageUIntIntegralSquare;
        ClooHaarObjectDetector _haarObjectDetector = null;

        CaptureDevice _captureDevice;

        object _lockUpdate = new object();
        ManualResetEvent _manualResetEventWebCamStop = new ManualResetEvent(false);
        bool _startProcessing = false;
        string _title;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            _title = Title;
        }

        /// <summary>
        /// Selected device has changed
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event details</param>
        private void comboBoxDevices_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_firstActivated) return;
            initializeDevice();
        }

        void initializeDevice()
        {
            try
            {
                _selectedDevice = comboBoxDevices.SelectedItem as ClooDevice;
                if (_context != null)
                {
                    _startProcessing = false;

                    // dispose previous context
                    _context.Dispose();
                    _kernels = null;
                    _context = null;
                    _sampler = null;
                    _queue = null;

                    image2.Source = null;
                    image3.Source = null;
                    image4.Source = null;
                }
                if (_selectedDevice != null)
                {
                    // create context
                    _context = _selectedDevice.CreateContext();
                    _queue = _context.CreateCommandQueue();
                    _sampler = new ClooSampler(_context, false, ComputeImageAddressing.ClampToEdge, ComputeImageFiltering.Linear);
                    _kernels = ClooProgramViolaJones.Create(_context);

                    _haarObjectDetector = ClooHaarObjectDetector.CreateFaceDetector(_context, _queue, 640, 480);
                    _haarObjectDetector.ScalingFactor = 1.25f;
                    _haarObjectDetector.ScalingMode = ScalingMode.SmallerToLarger;
                    _haarObjectDetector.MinSize = new System.Drawing.Size(30, 30);
                    _haarObjectDetector.MaxSize = new System.Drawing.Size(100, 100);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Reset();
                    stopwatch.Start();
                    _histogram = new ClooBuffer<uint>(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.AllocateHostPointer, 32 * 32 * 32);
                    _histogram2 = new ClooBuffer<uint>(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.AllocateHostPointer, 32 * 32 * 32);
                    _clooImageByteOriginal = ClooImage2DByteRgbA.CreateFromBitmap(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1);
                    _clooImageByteOriginal.WriteToDevice(_queue);
                    _clooImageByteGrayOriginal = ClooImage2DByteA.CreateFromBitmap(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1);
                    _clooImageByteGrayOriginal.WriteToDevice(_queue);
                    _clooImageByteResult = ClooImage2DByteRgbA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1.Width, _bitmapImage1.Height);
                    _clooImageByteResultA = ClooImage2DByteA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1.Width, _bitmapImage1.Height);
                    _clooImageFloatOriginal = ClooImage2DFloatRgbA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1.Width, _bitmapImage1.Height);
                    _clooImageFloatGrayOriginal = ClooImage2DFloatA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1.Width, _bitmapImage1.Height);
                    _clooImageFloatTemp1 = ClooImage2DFloatRgbA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1.Width, _bitmapImage1.Height);
                    _clooImageFloatTemp2 = ClooImage2DFloatRgbA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1.Width, _bitmapImage1.Height);
                    _clooImageFloatATemp1 = ClooImage2DFloatA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1.Width, _bitmapImage1.Height);
                    _clooImageFloatATemp2 = ClooImage2DFloatA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1.Width, _bitmapImage1.Height);
                    _clooImageFloatIntegral = ClooImage2DFloatA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1.Width + 1, _bitmapImage1.Height + 1);
                    _clooImageUIntIntegral = ClooImage2DUIntA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1.Width + 1, _bitmapImage1.Height + 1);
                    _clooImageUIntIntegralSquare = ClooImage2DUIntA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, _bitmapImage1.Width + 1, _bitmapImage1.Height + 1);
                    _queue.Finish();
                    label1.Content = stopwatch.ElapsedMilliseconds + " ms - original " + _bitmapImage1.Width + "x" + _bitmapImage1.Height;

                    _startProcessing = true;

                    Update();
                    Update();
                }
            }
            catch (Exception ex)
            {
                // show exception
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }

        /// <summary>
        /// Update
        /// </summary>
        public void Update()
        {
            if (!_startProcessing) return;

            Stopwatch stopwatchAll = Stopwatch.StartNew();
            Stopwatch stopwatch = new Stopwatch();
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(_clooImageByteOriginal.Width, _clooImageByteOriginal.Height, PixelFormat.Format24bppRgb);

            // normalize
            lock (_queue)
            {
                stopwatch.Reset();
                stopwatch.Start();
                lock (_queue)
                {
                    _kernels.ByteToFloat(_queue, _clooImageByteOriginal, _clooImageFloatTemp1);
                    _kernels.Normalize(_queue, _clooImageFloatTemp1, _clooImageFloatOriginal);
                    _queue.Finish();
                    label2.Content = stopwatch.ElapsedMilliseconds + " ms - normalize";
                    _clooImageFloatOriginal.ToBitmap(_queue, bitmap);
                }
                image2.Source = bitmap.ToBitmapSource();
            }

            // blur
            {
                stopwatch.Reset();
                stopwatch.Start();

                lock (_queue)
                {
                    _kernels.BoxBlur(_queue, _clooImageFloatOriginal, _clooImageFloatTemp2, _sampler, 1);
                    _kernels.FloatToByte(_queue, _clooImageFloatTemp2, _clooImageByteResult);
                    _queue.Finish();
                    label3.Content = stopwatch.ElapsedMilliseconds + " ms - box blur";
                    _clooImageByteResult.ToBitmap(_queue, bitmap);
                }
                image3.Source = bitmap.ToBitmapSource();
            }

            // grayscale
            {
                stopwatch.Reset();
                stopwatch.Start();
                lock (_queue)
                {
                    _kernels.GrayScale(_queue, _clooImageByteOriginal, _clooImageFloatGrayOriginal);
                    _queue.Finish();
                    label4.Content = stopwatch.ElapsedMilliseconds + " ms - grayscale";
                    image4.Source = _clooImageFloatGrayOriginal.ToBitmap(_queue).ToBitmapSource();
                }
            }

            // histogram 256
            {
                lock (_queue)
                {
                    stopwatch.Reset();
                    stopwatch.Start();
                    _kernels.FloatToByte(_queue, _clooImageFloatGrayOriginal, _clooImageByteResultA);
                    _kernels.Histogram256(_queue, _clooImageByteResultA, _histogram);
                    _queue.Finish();
                    label5.Content = stopwatch.ElapsedMilliseconds + " ms - histogram";
                    _histogram.ReadFromDevice(_queue);
                }
                image5.Source = _histogram.HostBuffer.HistogramBufferToBitmap(256, 0, 128, 0, 0, 256).ToBitmapSource();
            }

            // sobel
            {
                stopwatch.Reset();
                stopwatch.Start();
                lock (_queue)
                {
                    _kernels.Sobel(_queue, _clooImageFloatGrayOriginal, _clooImageFloatATemp1, _sampler);
                    _kernels.FloatToByte(_queue, _clooImageFloatATemp1, _clooImageByteResultA);
                    _queue.Finish();
                    label6.Content = stopwatch.ElapsedMilliseconds + " ms - sobel";
                    image6.Source = _clooImageByteResultA.ToBitmap(_queue).ToBitmapSource();
                }
            }

            //// integral image
            //{
            //    stopwatch.Reset();
            //    stopwatch.Start();
            //    lock (_queue)
            //    {
            //        _kernels.Integral(_queue, _clooImageFloatGrayOriginal, _clooImageFloatIntegral);
            //        _queue.Finish();
            //        label7.Content = stopwatch.ElapsedMilliseconds + " ms - integral image";
            //        _clooImageFloatIntegral.ReadFromDevice(_queue);
            //        float maxValue = _clooImageFloatIntegral.HostBuffer.GetMaxValue();
            //        _kernels.MultiplyValue(_queue, _clooImageFloatIntegral, 255 / maxValue);
            //        image7.Source = _clooImageFloatIntegral.ToBitmap(_queue).ToBitmapSource();
            //    }
            //}

            // histogram backprojection
            {
                stopwatch.Reset();
                stopwatch.Start();
                lock (_queue)
                {
                    Rectangle rectSource = new Rectangle(0, 0, 100, 120);
                    Rectangle rectFrame = new Rectangle(0, 0, 640, 480);

                    _kernels.HistogramN(_queue, _clooImageByteOriginal, _histogram, 6, rectSource.Left, rectSource.Top, rectSource.Width, rectSource.Height);
                    _kernels.HistogramN(_queue, _clooImageByteOriginal, _histogram2, 6, rectFrame.Left, rectFrame.Top, rectFrame.Width, rectFrame.Height);
                    _kernels.HistogramBackprojection(_queue, _clooImageByteOriginal, _clooImageFloatATemp1, _histogram, _histogram2, 4);
                    _queue.Finish();
                    _clooImageFloatATemp1.ReadFromDevice(_queue);
                    float maxValue = _clooImageFloatATemp1.HostBuffer.Max();
                    _kernels.MultiplyValue(_queue, _clooImageFloatATemp1, 1 / maxValue);
                    label7.Content = stopwatch.ElapsedMilliseconds + " ms - histogram backprojection";
                    image7.Source = _clooImageFloatATemp1.ToBitmap(_queue).ToBitmapSource();
                }
            }

            // Viola & Jones
            {
                stopwatch.Reset();
                stopwatch.Start();
                lock (_queue)
                {
                    _kernels.Integral(_queue, _clooImageByteGrayOriginal, _clooImageUIntIntegral);
                    _kernels.IntegralSquare(_queue, _clooImageByteGrayOriginal, _clooImageUIntIntegralSquare);
                    _haarObjectDetector.IntegralImage = _clooImageUIntIntegral;
                    _haarObjectDetector.Integral2Image = _clooImageUIntIntegralSquare;

                    long milliseconds = stopwatch.ElapsedMilliseconds;
                    int facesCount = _haarObjectDetector.ProcessFrame();
                    milliseconds = stopwatch.ElapsedMilliseconds;

                    Dispatcher.Invoke(new Action(() =>
                    {
                        using (var bmp = _clooImageFloatGrayOriginal.ToBitmap(_queue))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                using (Pen pen = new Pen(Color.Red, 3))
                                {
                                    for (int i = 0; i < facesCount; i++)
                                        g.DrawRectangle(pen, _haarObjectDetector.ResultRectangles.HostBuffer[i]);
                                }
                            }
                            image8.Source = bmp.ToBitmapSource();
                            label8.Content = stopwatch.ElapsedMilliseconds + " ms - Viola & Jones";
                        }
                    }));
                }
            }

            // total
            Title = _title + " - " + stopwatchAll.ElapsedMilliseconds + " ms";
        }

        private void checkBoxConvertHSL_Checked(object sender, RoutedEventArgs e)
        {
            Update();
        }

        /// <summary>
        /// Browse for different image
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event details</param>
        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                _bitmapImage1 = new System.Drawing.Bitmap(openFileDialog.FileName);
                image1.Source = _bitmapImage1.ToBitmapSource();
                initializeDevice();
            }
        }

        private void checkBoxEnableWebCam_Checked(object sender, RoutedEventArgs e)
        {
            int width = 320;
            int height = 240;

            _haarObjectDetector.ScalingFactor = 1.0625f;
            _haarObjectDetector.ScalingMode = ScalingMode.SmallerToLarger;
            _haarObjectDetector.MinSize = new System.Drawing.Size(60, 60);
            _haarObjectDetector.MaxSize = new System.Drawing.Size(width, height);

            if (checkBoxEnableWebCam.IsChecked == true)
            {
                _captureDevice = new CaptureDevice(CaptureDevice.DeviceMonikers[0].MonikerString, width, height, PixelFormat.Format32bppRgb);
            }
            else
            {
                _manualResetEventWebCamStop.Set();
                if (_captureDevice != null)
                {
                    _captureDevice.Dispose();
                    _captureDevice = null;
                }
                return;
            }

            _manualResetEventWebCamStop.Reset();
            ClooImage2DByteRgbA imageOriginal = null;
            ClooImage2DFloatRgbA imageOriginalF = null;
            ClooImage2DByteA imageByteA = null;
            ClooImage2DByteRgbA imageByteRgbA = null;
            ClooImage2DFloatRgbA imageResult = null;
            ClooImage2DFloatA imageFloatA = null;
            ClooImage2DFloatA imageResFloatA = null;
            ClooImage2DUIntA imageIntegral = null;
            ClooImage2DUIntA imageIntegral2 = null;

            byte[] bitmap = null, bitmapReady = null;
            _captureDevice.NewFrame += (s, ea) =>
                {
                    try
                    {
                        if (_manualResetEventWebCamStop.WaitOne(0)) return;

                        bitmapReady = _captureDevice.BitmapBuffer;

                        if (bitmapReady != null)
                        {
                            bitmap = bitmapReady;
                            bitmapReady = null;

                            // recreate image if needed
                            if (imageOriginal == null || imageOriginal.Width != _captureDevice.Width || imageOriginal.Height != _captureDevice.Height)
                            {
                                width = _captureDevice.Width;
                                height = _captureDevice.Height;

                                lock (_queue)
                                {
                                    imageOriginal = ClooImage2DByteRgbA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, width, height);
                                    imageOriginalF = ClooImage2DFloatRgbA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, width, height);
                                    imageResult = ClooImage2DFloatRgbA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, width, height);
                                    imageByteA = ClooImage2DByteA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, width, height);
                                    imageByteRgbA = ClooImage2DByteRgbA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, width, height);
                                    imageFloatA = ClooImage2DFloatA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, width, height);
                                    imageResFloatA = ClooImage2DFloatA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, width, height);
                                    imageIntegral = ClooImage2DUIntA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, width, height);
                                    imageIntegral2 = ClooImage2DUIntA.Create(_context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, width, height);
                                }
                            }
                            Array.Copy(bitmap, imageOriginal.HostBuffer, bitmap.Length);

                            lock (_queue)
                            {
                                imageOriginal.WriteToDevice(_queue);

                                // set transparency and flip
                                _kernels.FlipY(_queue, imageOriginal, imageByteRgbA);
                                _kernels.SetChannel(_queue, imageByteRgbA, imageOriginal, 3, 255);
                                _kernels.SwapChannel(_queue, imageOriginal, imageOriginal, 0, 2);
                                imageOriginal.ReadFromDevice(_queue);
                            }

                            // show original image
                            Dispatcher.Invoke(new Action(() =>
                            {
                                Stopwatch stopwatch = Stopwatch.StartNew();
                                using (var bmp = imageOriginal.ToBitmap(_queue))
                                {
                                    image9.Source = bmp.ToBitmapSource();
                                    label9.Content = stopwatch.ElapsedMilliseconds + " ms - webcam";
                                }
                            }));

                            lock (_queue)
                            {
                                Stopwatch stopwatch = Stopwatch.StartNew();
                                //_kernels.ByteToFloat(_queue, imageOriginal, imageOriginalF);
                                _kernels.GrayScale(_queue, imageOriginal, imageByteA);
                                //_kernels.BoxBlur(_queue, imageFloatA, imageResFloatA, _sampler, 1);
                                //_kernels.Clamp(_queue, imageFloatA, 0, 255);

                                //_kernels.FloatToByte(_queue, imageFloatA, imageByteA);
                                _kernels.Integral(_queue, imageByteA, imageIntegral);
                                _kernels.IntegralSquare(_queue, imageByteA, imageIntegral2);

                                //imageByteA.ReadFromDevice(_queue);
                                //imageIntegral.ReadFromDevice(_queue);
                                //imageIntegral2.ReadFromDevice(_queue);
                                _haarObjectDetector.IntegralImage = imageIntegral;
                                _haarObjectDetector.Integral2Image = imageIntegral2;

                                int facesCount = _haarObjectDetector.ProcessFrame(imageByteA);
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    using (var bmp = imageByteA.ToBitmap(_queue))
                                    {
                                        using (Graphics g = Graphics.FromImage(bmp))
                                        {
                                            using (Pen pen = new Pen(Color.Red, 3))
                                            {
                                                for (int i = 0; i < facesCount; i++)

                                                    g.DrawRectangle(pen, _haarObjectDetector.ResultRectangles.HostBuffer[i]);
                                                
                                            }
                                        }
                                        image8.Source = bmp.ToBitmapSource();
                                        label8.Content = stopwatch.ElapsedMilliseconds + " ms - Viola & Jones";
                                    }
                                }));
                            }
                        }
                    }
                    catch
                    {
                        if (_manualResetEventWebCamStop.WaitOne(10)) return;
                    }
                };
            _captureDevice.Start();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (_firstActivated)
            {
                _firstActivated = false;

                try
                {
                    // load image from resource
                    using (Stream stream = Application.GetResourceStream(new Uri("Images/facestest.jpg", UriKind.Relative)).Stream)
                    {
                        _bitmapImage1 = new System.Drawing.Bitmap(stream);
                    }

                    // populate compute devices
                    foreach (ClooDevice device in ClooDevice.CompatibleDevices.Where(x => x.Available).OrderByDescending(x => x.MaxComputeUnits))
                        comboBoxDevices.Items.Add(device);
                    if (comboBoxDevices.Items.Count > 1)
                    {
                        comboBoxDevices.SelectedIndex = 1;
                    }
                    else MessageBox.Show("No compute devices found!");
                }
                catch (Exception ex)
                {
                    // show exception
                    MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Stop);
                }
            }
        }
    }
}
