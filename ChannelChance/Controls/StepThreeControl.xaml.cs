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

namespace ChannelChance.Controls
{
    /// <summary>
    /// StepThreeControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepThreeControl : UserControl, IDirectionMove
    {
        private DispatcherTimer timer = new DispatcherTimer();
        public StepThreeControl(MainWindow window)
        {
            InitializeComponent();
            timer.Interval = new TimeSpan(0, 0, Appconfig.AutoPlayInterval);
            timer.Tick += timer_Tick;
            media.MediaEnded += OnSceneOver;
            media.MediaFailed += (sender, args) =>
            {
                _isMediaPlaying = false;
            };
            media.MediaOpened += (s, e) =>
            {
                //Panel.SetZIndex(media, 999);
                //border.Visibility = Visibility.Collapsed;
                media.Opacity = 1;
            };
            Window = window;
            ElementAnimControl.LeftCount = new int[7] { 4, 4, 4, 4, 3, 3, 3 };
            ElementAnimControl.RightCount = new int[7] { 3, 3, 3, 4, 4, 4, 4 };
            ElementAnimControl.Initial(Appconfig.GroundImagesDirName);
            ElementAnimControl.PlayRightNextPage += i =>
            {
                window.Pause();
                _isMediaPlaying = true;
                ShowMedia(Appconfig.III_A_MP4);
            };
            ElementAnimControl.PlayLeftNextPage += i =>
            {
                window.Pause();
                _isMediaPlaying = true;
                ShowMedia(Appconfig.III_B_MP4);
            };
        }
        void timer_Tick(object sender, EventArgs e)
        {
            leftEllipseAnimControl.BeginAutoMove();
            rightEllipseAnimControl.BeginAutoMove();
            ElementAnimControl.Reset();
        }
        public event EventHandler SceneOver;

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
        }

        public void LeftHandMove(int count)
        {

        }

        public void RightHandMove(int count)
        {

        }

        public void LeftHandUp(int count)
        {
            ElementAnimControl.HandUpAndDown(HandDirection.L);
            rightEllipseAnimControl.Reset();
            leftEllipseAnimControl.Reset();
            timer.Stop(); timer.Start();
        }

        public void RightHandUp(int count)
        {
            ElementAnimControl.HandUpAndDown(HandDirection.R);
            rightEllipseAnimControl.Reset();
            leftEllipseAnimControl.Reset();
            timer.Stop(); timer.Start();
        }

        public void LeftHandsMoveY(int count)
        {
            this.timer.Stop(); this.timer.Start();
            rightEllipseAnimControl.StopAutoMove();
            var length = Math.Abs(count);
            for (int i = 0; i < length; i++)
            {
                ElementAnimControl.ChangeLeftIndex(-Appconfig.ToRorL(count));
                if (count < 0)
                    leftEllipseAnimControl.MoveToNext();
                else
                    leftEllipseAnimControl.MoveToLast();
            }
        }

        public void RightHandsMoveY(int count)
        {
            this.timer.Stop(); this.timer.Start();
            leftEllipseAnimControl.StopAutoMove();
            var length = Math.Abs(count);
            for (int i = 0; i < length; i++)
            {
                ElementAnimControl.ChangeRightIndex(-Appconfig.ToRorL(count));
                if (count < 0)
                    rightEllipseAnimControl.MoveToNext();
                else
                    rightEllipseAnimControl.MoveToLast();
            }
        }

        public void Reset()
        {
            this.timer.Stop();
            ElementAnimControl.Reset();
            leftEllipseAnimControl.Reset();
            rightEllipseAnimControl.Reset();
        }

        public void Initial()
        {
            this.timer.Start();
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
                return 2;
            }
        }
        public MainWindow Window { get; set; }
    }
}
