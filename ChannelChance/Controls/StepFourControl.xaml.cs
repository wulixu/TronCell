using ChannelChance.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChannelChance.Controls
{
    /// <summary>
    /// StepFourControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepFourControl : UserControl, IDirectionMove
    {
        private static readonly ILog _logger = LogManager.GetLogger("Logger");
        Storyboard sb = null;
        public StepFourControl(MainWindow window)
        {
            InitializeComponent();
            Window = window;
            sb = Resources["sb1"] as Storyboard;
            media.MediaEnded += OnSceneOver;
            media.MediaFailed += (sender, args) =>
            {
                _isMediaPlaying = false;
            };
            media.MediaOpened += (s, e) =>
            {
                //Panel.SetZIndex(media, 999);
                //border.Visibility = Visibility.Collapsed;
                //media.Opacity = 1;
            };
            sb.Completed += (s, e) =>
            {
                sb.Remove(media);
                media.Opacity = 1;
            };
        }

        public event EventHandler SceneOver;

        private void OnSceneOver(object s, EventArgs e)
        {
            _isMediaPlaying = false;
            Window.Play();
            if (SeesawManager.Instance.IsFinish)
            {
                img.Source = new BitmapImage(new Uri("/Imgs/seesaw.png", UriKind.Relative));
                if (SceneOver != null)
                    SceneOver(this, e);
            }
            else
            {
                try
                {
                    double[] p = Appconfig.GetAnimaEllipsePositions(SeesawManager.Instance.CurrentImgName);
                    if (p != null)
                    {
                        leftEllipseAnimControl.Margin = new Thickness(p[0], leftEllipseAnimControl.Margin.Top, leftEllipseAnimControl.Margin.Right, leftEllipseAnimControl.Margin.Bottom);// p[0];
                        rightEllipseAnimControl.Margin = new Thickness(p[1], rightEllipseAnimControl.Margin.Top, rightEllipseAnimControl.Margin.Right, rightEllipseAnimControl.Margin.Bottom);// p[0];
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                }
                img.Source = new BitmapImage(new Uri(SeesawManager.Instance.CurrentImg));
            }
            media.Stop();
            //media.Visibility = Visibility.Collapsed;
            media.Opacity = 0;
            media.Source = null;
        }

        private void ShowMedia()
        {
            Window.Pause();
            //Panel.SetZIndex(media, -1);
            media.Source = new Uri(SeesawManager.Instance.MP4Path);
            //media.Visibility = Visibility.Visible;
            //border.Visibility = Visibility.Visible;
            media.Play();
            sb.Begin(media, true);
        }

        public void LeftHandMove(int count)
        {

        }

        public void RightHandMove(int count)
        {

        }

        public void LeftHandUp(int count)
        {

        }

        public void RightHandUp(int count)
        {

        }

        private int _leftCount = 0;
        public void LeftHandsMoveY(int count)
        {
            if (_leftCount >= 3)
            {
                _isMediaPlaying = true;
                SeesawManager.Instance.HandDirection = HandDirection.L;
                SeesawManager.Instance.LeftHandTimes++;
                if (SeesawManager.Instance.CanPlayMP4)
                    ShowMedia();
                _leftCount = 0;
            }
            if (count > 0)
                _leftCount++;
        }

        private int _rightCount = 0;
        public void RightHandsMoveY(int count)
        {
           if (_rightCount >= 3)
            {
                _isMediaPlaying = true;
                SeesawManager.Instance.HandDirection = HandDirection.R;
                SeesawManager.Instance.RightHandTimes++;
                if (SeesawManager.Instance.CanPlayMP4)
                    ShowMedia();
                _rightCount = 0;
            }
            if (count > 0)
                _rightCount++;
        }
        public void Reset()
        {
        }
        public void Initial()
        {
            double[] p = Appconfig.GetAnimaEllipsePositions("Default");
            if (p != null)
            {
                leftEllipseAnimControl.Margin = new Thickness(p[0], leftEllipseAnimControl.Margin.Top, leftEllipseAnimControl.Margin.Right, leftEllipseAnimControl.Margin.Bottom);// p[0];
                rightEllipseAnimControl.Margin = new Thickness(p[1], rightEllipseAnimControl.Margin.Top, rightEllipseAnimControl.Margin.Right, rightEllipseAnimControl.Margin.Bottom);// p[0];
            }
        }
        private bool _isMediaPlaying;
        public bool IsMediaPlaying
        {
            get { return _isMediaPlaying; }
        }
        public int PageIndex
        {
            get
            {
                return 3;
            }
        }
        public MainWindow Window { get; set; }
    }
}