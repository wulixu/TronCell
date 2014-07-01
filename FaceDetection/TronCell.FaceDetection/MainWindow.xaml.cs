using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV;
using Emgu.CV.Structure;

namespace TronCell.FaceDetection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                int ret = face_detect_sdk.PFD_Init(1000, 0);
                if (ret != face_detect_sdk.PFD_STATUS_OK)
                    MessageBox.Show(this,"初始化不成功!", "错误");

                _capture = new Capture();
                _capture.ImageGrabbed += ProcessFrame;
            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }

      private Capture _capture = null;
      private bool _captureInProgress;

        private void ProcessFrame(object sender, EventArgs arg)
        {
            Image<Bgr, Byte> frame = _capture.RetrieveBgrFrame();

            Image<Gray, Byte> grayFrame = frame.Convert<Gray, Byte>();
            Image<Gray, Byte> smallGrayFrame = grayFrame.PyrDown();
            Image<Gray, Byte> smoothedGrayFrame = smallGrayFrame.PyrUp();
            Image<Gray, Byte> cannyFrame = smoothedGrayFrame.Canny(100, 60);


            //var bitmap = frame.ToJpegData()
            //MemoryStream imgStream =new MemoryStream();
            //picture.Save(imgStream, System.Drawing.Imaging.ImageFormat.Bmp);
            //byte[] imgData = frame.ToJpegData();
            //frame.


                

            short faceRote = 0;
            faceRote = face_detect_sdk.PFD_OP_FACE_ROLL_0;

            face_detect_sdk.PFD_FACE_DETECT faceInfo = new face_detect_sdk.PFD_FACE_DETECT();
            face_detect_sdk.PFD_AGR_DETECT agrInfo = new face_detect_sdk.PFD_AGR_DETECT();
            face_detect_sdk.PFD_SMILE_DETECT smileInfo = new face_detect_sdk.PFD_SMILE_DETECT();
            face_detect_sdk.PFD_DIRECT_DETECT directInfo = new face_detect_sdk.PFD_DIRECT_DETECT();
            face_detect_sdk.PFD_BLINK_DETECT blinkInfo = new face_detect_sdk.PFD_BLINK_DETECT();

                //只识别人脸位置
            



            var bitmap = frame.ToBitmap(frame.Width, frame.Height);


            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb)
            {
                Bitmap temp = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(temp))
                {
                    g.DrawImage(bitmap, 0, 0);
                }
                bitmap = temp;
            }

            MemoryStream imgStream =new MemoryStream();
            bitmap.Save(imgStream, System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] imgData = imgStream.ToArray();

            int ret = face_detect_sdk.PFD_FaceRecog(imgData, ref faceInfo, face_detect_sdk.PFD_DISABLEINFO, faceRote);

            Dispatcher.Invoke(new Action(() =>{
                using (bitmap)
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        using (System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Red, 3))
                        {
                            for (int i = 0; i < faceInfo.num; i++)
                            {
                                var left = faceInfo.info[i].faceInfo.rect_l;
                                var top = faceInfo.info[i].faceInfo.rect_t;
                                var height = faceInfo.info[i].faceInfo.rect_b - faceInfo.info[i].faceInfo.rect_t;
                                var width = faceInfo.info[i].faceInfo.rect_r - faceInfo.info[i].faceInfo.rect_l;
                                g.DrawRectangle(pen,left , top, height, width);
                            }
                                
                        }
                    }
                    OutPutImage.Source = bitmap.ToBitmapSource();
                }
            }));

            //captureImageBox.Image = frame;
            //grayscaleImageBox.Image = grayFrame;
            //smoothedGrayscaleImageBox.Image = smoothedGrayFrame;
            //cannyImageBox.Image = cannyFrame;
        }

        private void captureButtonClick(object sender, EventArgs e)
      {
         if (_capture != null)
         {
            if (_captureInProgress)
            {  //stop the capture
               //captureButton.Text = "Start Capture";
               _capture.Pause();
            }
            else
            {
               //start the capture
               //captureButton.Text = "Stop";
               _capture.Start();
            }

            _captureInProgress = !_captureInProgress;
         }
      }

      private void ReleaseData()
      {
         if (_capture != null)
            _capture.Dispose();
      }

      private void FlipHorizontalButtonClick(object sender, EventArgs e)
      {
         if (_capture != null) _capture.FlipHorizontal = !_capture.FlipHorizontal;
      }

      private void FlipVerticalButtonClick(object sender, EventArgs e)
      {
         if (_capture != null) _capture.FlipVertical = !_capture.FlipVertical;
      }
    }
}
