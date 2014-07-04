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
    /// StepThreeControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepThreeControl : UserControl, IDirectionMove
    {
        public StepThreeControl(MainWindow window)
        {
            InitializeComponent();
            media.MediaEnded += OnSceneOver;
            Window = window;
            ElementAnimControl.LeftCount = new int[5] { 5, 5, 5, 5, 5 };
            ElementAnimControl.RightCount = new int[5] { 5, 5, 5, 5, 5 };
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

        public event EventHandler SceneOver;

        private void OnSceneOver(object s, EventArgs e)
        {
            if (SceneOver != null)
                SceneOver(this, e);
            media.Visibility = Visibility.Collapsed;
            _isMediaPlaying = false;
            Window.Play();
        }

        private void ShowMedia(string mediaUri)
        {
            media.Visibility = Visibility.Visible;
            media.Source = new Uri(mediaUri);
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
        }

        public void RightHandUp(int count)
        {
            ElementAnimControl.HandUpAndDown(HandDirection.R);
        }

        public void LeftHandsMoveY(int count)
        {
            if (count > 0)
                return;
            var length = Math.Abs(count);
            for (int i = 0; i < length; i++)
            {
                ElementAnimControl.ChangeLeftIndex(1);
            }
        }

        public void RightHandsMoveY(int count)
        {
            if (count > 0)
                return;
            var length = Math.Abs(count);
            for (int i = 0; i < length; i++)
            {
                ElementAnimControl.ChangeRightIndex(1);
            }
        }

        public void Reset()
        {
            ElementAnimControl.Reset();
        }


        private bool _isMediaPlaying;
        public bool IsMediaPlaying
        {
            get { return _isMediaPlaying; }
        }

        public MainWindow Window { get; set; }
    }
}
