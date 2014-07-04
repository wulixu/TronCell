using ChannelChance.Common;
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

namespace ChannelChance.Controls
{
    /// <summary>
    /// StepFiveControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepFiveControl : UserControl
    {
        public StepFiveControl(MainWindow window)
        {
            InitializeComponent();
            media.MediaEnded += OnSceneOver;
            Window = window;
            imgCode.Source = new BitmapImage(new Uri(Appconfig.TowDimensionPic));
        }

        public event EventHandler SceneOver;

        private void OnSceneOver(object s, EventArgs e)
        {
            if (SceneOver != null)
                SceneOver(this, e);
            Window.Play();
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            ShowMedia(Appconfig.V_A_MP4);
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            ShowMedia(Appconfig.V_B_MP4);
        }

        private void ShowMedia(string mediaUri)
        {
            Window.Pause();
            media.Visibility = Visibility.Visible;
            media.Source = new Uri(mediaUri);
        }

        public void Init()
        {
            btnLeft.Visibility = imgLeft.Visibility = SeesawManager.Instance.HandDirection == HandDirection.L ? Visibility.Visible : Visibility.Collapsed;
            btnRight.Visibility = imgRight.Visibility = SeesawManager.Instance.HandDirection == HandDirection.R ? Visibility.Visible : Visibility.Collapsed;
            SeesawManager.Instance.HandDirection = HandDirection.None;
            SeesawManager.Instance.LeftHandTimes = SeesawManager.Instance.RightHandTimes = 0;
        }

        public MainWindow Window { get; set; }
    }
}