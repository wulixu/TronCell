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

namespace ChannelChance
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        StepOneControl ctrOne = new StepOneControl();
        StepTwoControl ctrTwo = new StepTwoControl() { Visibility = Visibility.Collapsed };
        StepThreeControl ctrThree = new StepThreeControl() { Visibility = Visibility.Collapsed };
        StepFourControl ctrFour = new StepFourControl() { Visibility = Visibility.Collapsed };

        public MainWindow()
        {
            InitializeComponent();
            ctrOne.SceneOver += OnSceneOver;
            ctrTwo.SceneOver += OnSceneOver;
            ctrThree.SceneOver += OnSceneOver;
            ctrOne.LayoutTransform = ctrTwo.LayoutTransform = ctrThree.LayoutTransform = ctrFour.LayoutTransform = this.LayoutTransform =
                new ScaleTransform(SystemParameters.PrimaryScreenWidth / 1920.0d, SystemParameters.PrimaryScreenHeight / 1080d);
            layoutGrid.Children.Add(ctrOne);
            layoutGrid.Children.Add(ctrTwo);
            layoutGrid.Children.Add(ctrThree);
            layoutGrid.Children.Add(ctrFour);
        }

        void OnSceneOver(object sender, EventArgs e)
        {
            ctrOne.Visibility = Visibility.Collapsed;
            ctrTwo.Visibility = sender as StepOneControl != null ? Visibility.Visible : Visibility.Collapsed;
            ctrThree.Visibility = sender as StepTwoControl != null ? Visibility.Visible : Visibility.Collapsed;
            ctrFour.Visibility = sender as StepThreeControl != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
