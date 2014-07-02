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
        StepTwoControl ctrTwo = new StepTwoControl();
        StepThreeControl ctrThree = new StepThreeControl();
        StepFourControl ctrFour = new StepFourControl();

        public MainWindow()
        {
            InitializeComponent();
            ctrOne.SceneOver += OnSceneOver;
            ctrTwo.SceneOver += OnSceneOver;
            ctrThree.SceneOver += OnSceneOver;
            ctrOne.LayoutTransform = ctrTwo.LayoutTransform = ctrThree.LayoutTransform = ctrFour.LayoutTransform = this.LayoutTransform =
                new ScaleTransform(SystemParameters.PrimaryScreenWidth / 1920.0d, SystemParameters.PrimaryScreenHeight / 1080d);
            layoutGrid.Children.Add(ctrOne);
        }

        void OnSceneOver(object sender, EventArgs e)
        {
            layoutGrid.Children.Clear();
            if (sender is StepOneControl)
                layoutGrid.Children.Add(ctrTwo);
            else if (sender is StepTwoControl)
                layoutGrid.Children.Add(ctrThree);
            else if (sender is StepThreeControl)
                layoutGrid.Children.Add(ctrFour);
        }
    }
}
