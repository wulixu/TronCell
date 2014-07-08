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
using System.Windows.Threading;
using ChannelChance.Common;

namespace ChannelChance.Controls
{
    /// <summary>
    /// StepTwoControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepTwoControl : UserControl, IDirectionMove
    {
        private bool _isMediaPlaying;
        private DispatcherTimer timer = new DispatcherTimer();
        public StepTwoControl(MainWindow window)
        {
            InitializeComponent();
            timer.Interval = new TimeSpan(0, 0, Appconfig.AutoPlayInterval);
            timer.Tick += timer_Tick;
            Window = window;
            media.MediaEnded += OnSceneOver;
            media.MediaFailed += (sender, args) =>
            {
                _isMediaPlaying = false;
            };
            media.MediaOpened += (s, e) =>
            {
                Panel.SetZIndex(media, 999);
            };
            ElementAnimControl.LeftCount = new int[7] { 4, 4, 4, 4, 3, 3, 3 };
            ElementAnimControl.RightCount = new int[7] { 3, 3, 3, 4, 4, 4, 4 };
            ElementAnimControl.Initial(Appconfig.CutImagesDirName);
            ElementAnimControl.PlayRightNextPage += i =>
            {
                window.Pause();
                _isMediaPlaying = true;
                ShowMedia(Appconfig.II_A_MP4);
            };
            ElementAnimControl.PlayLeftNextPage += i =>
            {
                window.Pause();
                _isMediaPlaying = true;
                ShowMedia(Appconfig.II_B_MP4);
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
            media.Visibility = Visibility.Collapsed;
            media.Source = null;
            _isMediaPlaying = false;
            Window.Play();
            if (SceneOver != null)
                SceneOver(this, e);
        }

        private void ShowMedia(string mediaUri)
        {
            Panel.SetZIndex(media, -1);
            media.Source = new Uri(mediaUri);
            media.Visibility = Visibility.Visible;
            media.Play();
        }

        public void LeftHandMove(int count)
        {
            this.timer.Stop(); this.timer.Start();
            rightEllipseAnimControl.StopAutoMove();
            var length = Math.Abs(count);
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
            timer.Stop(); timer.Start();
        }

        public MainWindow Window { get; set; }

        public void RightHandUp(int count)
        {
            ElementAnimControl.HandUpAndDown(HandDirection.R);
            rightEllipseAnimControl.Reset();
            leftEllipseAnimControl.Reset();
            timer.Stop(); timer.Start();
        }

        public void LeftHandsMoveY(int count)
        {

        }

        public void RightHandsMoveY(int count)
        {

        }
        public void Reset()
        {
            this.timer.Stop();
            ElementAnimControl.Reset();
            rightEllipseAnimControl.Reset();
            leftEllipseAnimControl.Reset();
        }
        public void Initial()
        {
            this.timer.Start();
        }
        public bool IsMediaPlaying
        {
            get { return _isMediaPlaying; }
        }
    }
}
