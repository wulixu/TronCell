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
using System.Windows.Media.Animation;

namespace ChannelChance.Controls
{
    /// <summary>
    /// StepFiveControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepFiveControl : UserControl, IDirectionMove
    {
        Storyboard sb = null;
        HandDirection currentDirection = HandDirection.None;
        public StepFiveControl(MainWindow window)
        {
            InitializeComponent();
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
                sb.Begin(media, true);
            };
            sb.Completed += (s, e) =>
            {
                sb.Remove(media);
                media.Opacity = 1;
            };
            Window = window;
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
            _isMediaPlaying = true;
            Window.Pause();
            //Panel.SetZIndex(media, -1);
            media.Source = new Uri(mediaUri);
            //media.Visibility = Visibility.Visible;
            //border.Visibility = Visibility.Visible;

            media.Play();
            //sb.Begin(media, true);
        }

        public void Init()
        {
            leftImg.Visibility = SeesawManager.Instance.HandDirection == HandDirection.L ? Visibility.Visible : Visibility.Collapsed;
            rightImg.Visibility = SeesawManager.Instance.HandDirection == HandDirection.L ? Visibility.Collapsed : Visibility.Visible;
            Tuple<Uri, Uri> page1 = HandOption.Instance.Page1Img;
            Tuple<Uri, Uri> page2 = HandOption.Instance.Page2Img;
            Tuple<Uri, Uri> page3 = HandOption.Instance.Page3Img;
            Tuple<Uri, Uri> page4 = HandOption.Instance.Page4Img;
            img1.Source = new BitmapImage(page1.Item2);
            img2.Source = new BitmapImage(page2.Item2);
            img3.Source = new BitmapImage(page3.Item2);
            img4.Source = new BitmapImage(page4.Item2);
            demesion1.Source = new BitmapImage(page1.Item1);
            demesion2.Source = new BitmapImage(page2.Item1);
            demesion3.Source = new BitmapImage(page3.Item1);
            demesion4.Source = new BitmapImage(page4.Item1);
            currentDirection = SeesawManager.Instance.HandDirection;
            SeesawManager.Instance.HandDirection = HandDirection.None;
            SeesawManager.Instance.LeftHandTimes = SeesawManager.Instance.RightHandTimes = 0;
            HandOption.Instance.SetInit();
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
        }

        public void LeftHandsMoveY(int count)
        {
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

        public void Fly()
        {
            if (currentDirection == HandDirection.L)
                ShowMedia(Appconfig.V_A_MP4);
            else
                ShowMedia(Appconfig.V_B_MP4);
        }
    }
}