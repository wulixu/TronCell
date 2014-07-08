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
using System.Configuration;

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
            string[] kv = ConfigurationManager.AppSettings["SkeletonInfo"].Split(',');
            double kWidth = Convert.ToDouble(kv[0]);
            double kHeight = Convert.ToDouble(kv[1]);
            double kLeft = Convert.ToDouble(kv[2]);
            double kTop = Convert.ToDouble(kv[3]);
            double kAlpha = Convert.ToDouble(kv[4]);

            KinectColorViewer kc2 = new KinectColorViewer();
            kc2.Opacity = kAlpha;
            kc2.Width = kWidth;
            kc2.Height = kHeight;
            Canvas.SetLeft(kc2, kLeft);
            Canvas.SetTop(kc2, kTop);
            kc2.KinectSensorManager = gestureControl.KinectSensorManager;
            this.root.Children.Add(kc2);

            KinectSkeletonViewer ks = new KinectSkeletonViewer();
            ks.Opacity = kAlpha;
            ks.Width = kWidth;
            ks.Height = kHeight;
            Canvas.SetLeft(ks, kLeft);
            Canvas.SetTop(ks, kTop);
            ks.KinectSensorManager = gestureControl.KinectSensorManager;
            this.root.Children.Add(ks); 
           
        }
        void gestureControl_OnKinectGestureDetected(object sender, KinectGestureEventArgs e)
        {
            if (_currentControl == null || _currentControl.IsMediaPlaying)
                return;

            switch (e.GestureType)
            {
                case KinectGestureType.LeftHandsUP:
                    _currentControl.LeftHandUp(e.ActionStep);
                    break;
                case KinectGestureType.LeftHandsMove:
                    _currentControl.LeftHandMove(e.ActionStep);
                    break;
                case KinectGestureType.LeftHandsMoveY:
                    _currentControl.LeftHandsMoveY(e.ActionStep);
                    break;
                case KinectGestureType.RightHandsUP:
                    _currentControl.RightHandUp(e.ActionStep);
                    break;
                case KinectGestureType.RightHandsMove:
                    _currentControl.RightHandMove(e.ActionStep);
                    break;
                case KinectGestureType.RightHandsMoveY:
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
                if (_currentControl != null)
                    _currentControl.Initial();
                if (sender as StepFourControl != null)
                    ctrFive.Init();
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
        protected override void OnKeyDown(KeyEventArgs e)
        {
            var gestureEventArgs = new KinectGestureEventArgs();
            switch (e.Key)
            {
                case Key.Left:
                    gestureEventArgs.ActionStep = 7;
                    gestureEventArgs.GestureType = KinectGestureType.LeftHandsMove;
                    break;
                case Key.Right:
                    gestureEventArgs.ActionStep = 7;
                    gestureEventArgs.GestureType = KinectGestureType.RightHandsMove;
                    break;
                case Key.Up:
                    if (_currentControl.PageIndex == 3)
                    {
                        var isleftCtrl = e.KeyboardDevice.IsKeyDown(Key.LeftCtrl);
                        if (isleftCtrl)
                        {
                            gestureEventArgs.ActionStep = 7;
                            gestureEventArgs.GestureType = KinectGestureType.LeftHandsMoveY;
                        }
                        var isrightCtrl = e.KeyboardDevice.IsKeyDown(Key.RightCtrl);
                        if (isrightCtrl)
                        {
                            gestureEventArgs.ActionStep = 7;
                            gestureEventArgs.GestureType = KinectGestureType.RightHandsMoveY;
                        }
                    }
                    else
                    {
                        var isleftCtrl = e.KeyboardDevice.IsKeyDown(Key.LeftCtrl);
                        if (isleftCtrl)
                        {
                            gestureEventArgs.ActionStep = 7;
                            gestureEventArgs.GestureType = KinectGestureType.LeftHandsUP;
                        }
                        var isrightCtrl = e.KeyboardDevice.IsKeyDown(Key.RightCtrl);
                        if (isrightCtrl)
                        {
                            gestureEventArgs.ActionStep = 7;
                            gestureEventArgs.GestureType = KinectGestureType.RightHandsUP;
                        }
                    }
                    break;
                case Key.Down:
                    var isleftCtrl1 = e.KeyboardDevice.IsKeyDown(Key.LeftCtrl);
                    if (isleftCtrl1)
                    {
                        gestureEventArgs.ActionStep = -7;
                        gestureEventArgs.GestureType = KinectGestureType.LeftHandsMoveY;
                    }
                    var isrightCtrl2 = e.KeyboardDevice.IsKeyDown(Key.RightCtrl);
                    if (isrightCtrl2)
                    {
                        gestureEventArgs.ActionStep = -7;
                        gestureEventArgs.GestureType = KinectGestureType.RightHandsMoveY;
                    }
                    break;
                default:
                    gestureEventArgs = null;
                    break;
            }
            if (gestureEventArgs == null)
                return;

            if (_currentControl.PageIndex == 3)
            {
                for (int i = 0; i < 3; i++)
                    gestureControl_OnKinectGestureDetected(null, gestureEventArgs);
            }
            gestureControl_OnKinectGestureDetected(null, gestureEventArgs);
            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            ctrOne.SceneOver -= OnSceneOver;
            ctrTwo.SceneOver -= OnSceneOver;
            ctrThree.SceneOver -= OnSceneOver;
            ctrFour.SceneOver -= OnSceneOver;
            ctrFive.SceneOver -= OnSceneOver;
            base.OnClosed(e);
            gestureControl.Stop();
        }
    }
}
