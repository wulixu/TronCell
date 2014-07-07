using ChannelChance.Common;
using ChannelChance.Controls;
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
using ChannelChance.Kinect;
using Microsoft.Samples.Kinect.WpfViewers;
using log4net;

namespace ChannelChance
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILog _logger = LogManager.GetLogger("Logger");
        StepOneControl ctrOne = null;
        StepTwoControl ctrTwo = null;
        StepThreeControl ctrThree = null;
        StepFourControl ctrFour = null;
        StepFiveControl ctrFive = null;
        KinectGestureControl gestureControl = new KinectGestureControl();
        private List<UserControl> controls = new List<UserControl>();
        private IDirectionMove _currentControl;

        public MainWindow()
        {
            InitializeComponent();
            ctrOne = new StepOneControl(this);
            ctrTwo = new StepTwoControl(this) { Visibility = Visibility.Collapsed };
            ctrThree = new StepThreeControl(this) { Visibility = Visibility.Collapsed };
            ctrFour = new StepFourControl(this) { Visibility = Visibility.Collapsed };
            ctrFive = new StepFiveControl(this) { Visibility = Visibility.Collapsed };
            media.MediaEnded += (s, e) =>
            {
                media.Source = new Uri(Appconfig.MP3);
                media.Play();
            };
            this.Loaded += (s, e) =>
            {
                _logger.Debug("................启动......................");
                media.Source = new Uri(Appconfig.MP3);
                media.Play();
            };

            gestureControl.OnKinectGestureDetected += gestureControl_OnKinectGestureDetected;

            ctrOne.SceneOver += OnSceneOver;
            ctrTwo.SceneOver += OnSceneOver;
            ctrThree.SceneOver += OnSceneOver;
            ctrFour.SceneOver += OnSceneOver;
            ctrFive.SceneOver += OnSceneOver;
            ctrOne.LayoutTransform = ctrTwo.LayoutTransform = ctrThree.LayoutTransform = ctrFour.LayoutTransform = ctrFive.LayoutTransform = this.LayoutTransform =
                new ScaleTransform(SystemParameters.PrimaryScreenWidth / 1920.0d, SystemParameters.PrimaryScreenHeight / 1080d);
            layoutGrid.Children.Add(ctrOne);
            layoutGrid.Children.Add(ctrTwo);
            layoutGrid.Children.Add(ctrThree);
            layoutGrid.Children.Add(ctrFour);
            layoutGrid.Children.Add(ctrFive);

            controls.Add(ctrOne);
            controls.Add(ctrTwo);
            controls.Add(ctrThree);
            controls.Add(ctrFour);
            controls.Add(ctrFive);

            _currentControl = ctrOne;

            //显示kinect可视窗口
            KinectColorViewer kc = new KinectColorViewer();
            kc.Width = 160d;
            kc.Height = 120d;
            kc.HorizontalAlignment = HorizontalAlignment.Right;
            kc.VerticalAlignment = VerticalAlignment.Top;
            kc.Margin = new Thickness(50);
            kc.KinectSensorManager = gestureControl.KinectSensorManager;
            this.layoutGrid.Children.Add(kc);
        }
        void gestureControl_OnKinectGestureDetected(object sender, KinectGestureEventArgs e)
        {
            if (_currentControl == null || _currentControl.IsMediaPlaying)
                return;

            switch (e.GestureType)
            {
                case KinectGestureType.LeftHandsUP:
                    Console.WriteLine("LeftHandsUp ActionStep:" + e.ActionStep);
                    _currentControl.LeftHandUp(e.ActionStep);
                    break;
                case KinectGestureType.LeftHandsMove:
                    Console.WriteLine("LeftHandsMove ActionStep:" + e.ActionStep);
                    _currentControl.LeftHandMove(e.ActionStep);
                    break;
                case KinectGestureType.LeftHandsMoveY:
                    Console.WriteLine("LeftHandsMoveY " + e.ActionStep);
                    _currentControl.LeftHandsMoveY(e.ActionStep);
                    break;
                case KinectGestureType.RightHandsUP:
                    Console.WriteLine("RightHandsUp ActionStep:" + e.ActionStep);
                    _currentControl.RightHandUp(e.ActionStep);
                    break;
                case KinectGestureType.RightHandsMove:
                    Console.WriteLine("RightHandsMove ActionStep:" + e.ActionStep);
                    _currentControl.RightHandMove(e.ActionStep);
                    break;
                case KinectGestureType.RightHandsMoveY:
                    Console.WriteLine("RightHandsMoveY " + e.ActionStep);
                    _currentControl.RightHandsMoveY(e.ActionStep);
                    break;
            }
        }

        void OnSceneOver(object sender, EventArgs e)
        {
            var userControl = sender as UserControl;
            if (userControl != null)
            {
                userControl.Visibility = Visibility.Collapsed;
                var lastControl = userControl as IDirectionMove;
                if (lastControl != null) lastControl.Reset();
                var index = controls.IndexOf(userControl);
                index++;
                var i = index % 5;
                var control = controls[i];
                control.Visibility = Visibility.Visible;
                _currentControl = control as IDirectionMove;
                if (sender as StepFourControl != null)
                    ctrFive.Init();
                Console.WriteLine("NextPage:" + i);
            }
        }

        public void Pause()
        {
            media.Pause();
        }

        public void Play()
        {
            media.Play();
        }
       

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            gestureControl.Stop();
        }
    }
}
