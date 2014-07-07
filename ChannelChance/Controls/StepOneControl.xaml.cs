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

namespace ChannelChance.Controls
{
    /// <summary>
    /// StepOneControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepOneControl : UserControl, IDirectionMove
    {
        private bool _isMediaPlaying;

        public StepOneControl(MainWindow window)
        {
            InitializeComponent();
            media.MediaEnded += OnSceneOver;
            media.MediaFailed += (sender, args) =>
            {
                _isMediaPlaying = false;
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

        private void OnSceneOver(object s, EventArgs e)
        {
            media.Visibility = Visibility.Collapsed;
            _isMediaPlaying = false;
            Window.Play();
            if (SceneOver != null)
                SceneOver(this, e);
        }

        private void ShowMedia(string mediaUri)
        {
            media.Visibility = Visibility.Visible;
            media.Source = new Uri(mediaUri);
        }

        public void LeftHandMove(int count)
        {
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
            var length = Math.Abs(count);
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
        }

        public MainWindow Window { get; set; }

        public void RightHandUp(int count)
        {
            ElementAnimControl.HandUpAndDown(HandDirection.R);
            rightEllipseAnimControl.Reset();
            leftEllipseAnimControl.Reset();
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
        }

        public bool IsMediaPlaying
        {
            get { return _isMediaPlaying; }
        }
    }
}
