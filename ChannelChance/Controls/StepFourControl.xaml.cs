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
    /// StepFourControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepFourControl : UserControl, IDirectionMove
    {
        public StepFourControl(MainWindow window)
        {
            InitializeComponent();
            Window = window;
            media.MediaEnded += OnSceneOver;
            media.MediaFailed += (sender, args) =>
            {
                _isMediaPlaying = false;
            };
            media.MediaOpened += (s, e) => {
                Panel.SetZIndex(media, 999);
            };
        }

        public event EventHandler SceneOver;

        private void OnSceneOver(object s, EventArgs e)
        {
            _isMediaPlaying = false;
            Window.Play();
            if (SeesawManager.Instance.IsFinish)
            {
                if (SceneOver != null)
                    SceneOver(this, e);
            }
            else
            {
                img.Source = new BitmapImage(new Uri(SeesawManager.Instance.CurrentImg));
            }
            media.Visibility = Visibility.Collapsed;
            Panel.SetZIndex(media, -1);
        }

        private void ShowMedia()
        {
            Window.Pause();
            media.Source = new Uri(SeesawManager.Instance.MP4Path);
            media.Visibility = Visibility.Visible;
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

        public void LeftHandsMoveY(int count)
        {
            _isMediaPlaying = true;
            SeesawManager.Instance.HandDirection = HandDirection.L;
            SeesawManager.Instance.LeftHandTimes++;
            if (SeesawManager.Instance.CanPlayMP4)
                ShowMedia();
        }

        public void RightHandsMoveY(int count)
        {
            _isMediaPlaying = true;
            SeesawManager.Instance.HandDirection = HandDirection.R;
            SeesawManager.Instance.RightHandTimes++;
            if (SeesawManager.Instance.CanPlayMP4)
                ShowMedia();
        }
        public void Reset()
        {
        }
        public void Initial()
        { }
        private bool _isMediaPlaying;
        public bool IsMediaPlaying
        {
            get { return _isMediaPlaying; }
        }

        public MainWindow Window { get; set; }
    }
}