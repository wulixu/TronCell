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
    /// StepFiveControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepFiveControl : UserControl, IDirectionMove
    {
        public StepFiveControl(MainWindow window)
        {
            InitializeComponent();
            media.MediaEnded += OnSceneOver;
            media.MediaFailed += (sender, args) =>
            {
                _isMediaPlaying = false;
            };
            media.MediaOpened += (s, e) =>
            {
                //Panel.SetZIndex(media, 999);
                border.Visibility = Visibility.Collapsed;
            };
            Window = window;
            imgCode.Source = new BitmapImage(new Uri(Appconfig.TowDimensionPic));
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
            _isMediaPlaying = true;
            Window.Pause();
            //Panel.SetZIndex(media, -1);
            media.Source = new Uri(mediaUri);
            media.Visibility = Visibility.Visible;
            border.Visibility = Visibility.Visible;
            media.Play();
        }

        public void Init()
        {
            imgLeft.Visibility = SeesawManager.Instance.HandDirection == HandDirection.L ? Visibility.Visible : Visibility.Collapsed;
            imgRight.Visibility = SeesawManager.Instance.HandDirection == HandDirection.R ? Visibility.Visible : Visibility.Collapsed;
            SeesawManager.Instance.HandDirection = HandDirection.None;
            SeesawManager.Instance.LeftHandTimes = SeesawManager.Instance.RightHandTimes = 0;
        }

        public MainWindow Window { get; set; }

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
            ShowMedia(Appconfig.V_B_MP4);
        }

        public void LeftHandsMoveY(int count)
        {
            if (count < 0)
                ShowMedia(Appconfig.V_A_MP4);
        }

        public void RightHandsMoveY(int count)
        {

        }

        public void Reset()
        {
        }
        public void Initial()
        {
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
                return 4;
            }
        }
    }
}