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

namespace ChannelChance
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        StepOneControl ctrOne = null;
        StepTwoControl ctrTwo = null;
        StepThreeControl ctrThree = null;
        StepFourControl ctrFour = null;
        StepFiveControl ctrFive = null;
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
                media.Source = new Uri(Appconfig.MP3);
                media.Play();
            };
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
        }

        void OnSceneOver(object sender, EventArgs e)
        {
            if (sender as StepFiveControl != null)
            {
                ctrOne.Visibility = Visibility.Visible;
                ctrOne.media.Visibility = Visibility.Collapsed;
            }
            else
                ctrOne.Visibility = Visibility.Collapsed;
            ctrTwo.Visibility = sender as StepOneControl != null ? Visibility.Visible : Visibility.Collapsed;
            ctrThree.Visibility = sender as StepTwoControl != null ? Visibility.Visible : Visibility.Collapsed;
            ctrFour.Visibility = sender as StepThreeControl != null ? Visibility.Visible : Visibility.Collapsed;
            if (sender as StepFourControl != null)
            {
                ctrFive.Visibility = Visibility.Visible;
                ctrFive.Init();
            }
            else
                ctrFive.Visibility = Visibility.Collapsed;
        }

        public void Pause()
        {
            media.Pause();
        }

        public void Play()
        {
            media.Play();
        }
       
    }
}
