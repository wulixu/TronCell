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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChannelChance.Common;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace ChannelChance.Controls
{
    /// <summary>
    /// StepOneControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepOneControl : UserControl, IDirectionMove
    {
        private bool _isMediaPlaying;
        private DispatcherTimer timer = new DispatcherTimer();
        Storyboard sb = null;
        public StepOneControl(MainWindow window)
        {
            InitializeComponent();
            sb = Resources["sb1"] as Storyboard;
            timer.Interval = new TimeSpan(0, 0, Appconfig.AutoPlayInterval);
            timer.Tick += timer_Tick;
            timer.Start();
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
                sb.Begin(media, true);
            };
            sb.Completed += (s, e) =>
            {
                sb.Remove(media);
                media.Opacity = 1;
            };
            Window = window;
            ElementAnimControl.LeftCount = new int[7] { 4, 3, 3, 3, 3, 3, 3 };
            ElementAnimControl.RightCount = new int[7] { 3, 3, 3, 3, 3, 3, 4 };
            ElementAnimControl.Initial(Appconfig.TiltImagesDirName);
            ElementAnimControl.PlayRightNextPage += i =>
            {
                window.Pause();
                _isMediaPlaying = true;
                ShowMedia(Appconfig.I_A_MP4);
            };
            ElementAnimControl.PlayLeftNextPage += i =>
            {
                window.Pause();
                _isMediaPlaying = true;
                ShowMedia(Appconfig.I_B_MP4);
            };
        }

        public event EventHandler SceneOver;
        void timer_Tick(object sender, EventArgs e)
        {
            leftEllipseAnimControl.BeginAutoMove();
            rightEllipseAnimControl.BeginAutoMove();
            ElementAnimControl.Reset();
        }
        private void OnSceneOver(object s, EventArgs e)
        {
            media.Stop();
            //media.Visibility = Visibility.Collapsed;
            media.Opacity = 0;
            media.Source = null;
            _isMediaPlaying = false;
            Window.Play();
            if (SceneOver != null)
                SceneOver(this, e);
        }

        private void ShowMedia(string mediaUri)
        {
            //Panel.SetZIndex(media, -1);
            media.Source = new Uri(mediaUri);
            //media.Visibility = Visibility.Visible;
            //border.Visibility = Visibility.Visible;
            media.Play();
            //media.Opacity = 1;
            //sb.Begin(media, true);
        }

        public void LeftHandMove(int count)
        {
            this.timer.Stop(); this.timer.Start();
            var length = Math.Abs(count);
            rightEllipseAnimControl.StopAutoMove();
            for (int i = 0; i < length; i++)
            {
                ElementAnimControl.ChangeLeftIndex(Appconfig.ToRorL(count));
                if (count > 0)
                    leftEllipseAnimControl.MoveToNext();
                else
                    leftEllipseAnimControl.MoveToLast();
            }
        }

        public void RightHandMove(int count)
        {
            this.timer.Stop(); this.timer.Start();
            var length = Math.Abs(count);
            leftEllipseAnimControl.StopAutoMove();
            for (int i = 0; i < length; i++)
            {
                ElementAnimControl.ChangeRightIndex(Appconfig.ToRorL(count));
                if (count > 0)
                    rightEllipseAnimControl.MoveToNext();
                else
                    rightEllipseAnimControl.MoveToLast();
            }

        }

        public void LeftHandUp(int count)
        {
            ElementAnimControl.HandUpAndDown(HandDirection.L);
            rightEllipseAnimControl.Reset();
            leftEllipseAnimControl.Reset();
            //this.timer.Stop(); this.timer.Start();
        }

        public MainWindow Window { get; set; }

        public void RightHandUp(int count)
        {
            ElementAnimControl.HandUpAndDown(HandDirection.R);
            rightEllipseAnimControl.Reset();
            leftEllipseAnimControl.Reset();
            //this.timer.Stop(); this.timer.Start();
        }

        public void LeftHandsMoveY(int count)
        {

        }

        public void RightHandsMoveY(int count)
        {

        }

        public void Reset()
        {
            ElementAnimControl.Reset();
            rightEllipseAnimControl.Reset();
            leftEllipseAnimControl.Reset();
            this.timer.Stop();
        }
        public void Initial()
        {
            this.timer.Start();
        }

        public int PageIndex
        {
            get
            {
                return 0;
            }
        }

        public bool IsMediaPlaying
        {
            get { return _isMediaPlaying; }
        }

        
    }
}
