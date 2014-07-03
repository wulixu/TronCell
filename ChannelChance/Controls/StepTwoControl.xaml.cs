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
using ChannelChance.Common;

namespace ChannelChance.Controls
{
    /// <summary>
    /// StepTwoControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepTwoControl : UserControl
    {
        private DispatcherTimer timer;
        public StepTwoControl()
        {
            InitializeComponent();
            ElementAnimControl.LeftCount = new int[5] { 5, 5, 5, 5, 5 };
            ElementAnimControl.RightCount = new int[5] { 5, 5, 5, 5, 5 };
            ElementAnimControl.Initial(Appconfig.CutImagesDirName);
            ElementAnimControl.PlayRightNextPage += i =>
            {
                OnSceneOver(this, null);
            };
            ElementAnimControl.PlayLeftNextPage += i =>
            {
                OnSceneOver(this, null);
            };

            //timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromSeconds(3);
            //timer.Tick += (o, args) =>
            //{
            //    ElementAnimControl.HandUpAndDown(Hand.Right);
            //    timer.Stop();
            //};
        }

        public event EventHandler SceneOver;
        private void OnSceneOver(object s, EventArgs e)
        {
            if (SceneOver != null)
                SceneOver(s, e);
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                ElementAnimControl.ChangeLeftIndex(1);
            }
            // OnSceneOver(this, e);
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            //ElementAnimControl.HandUpAndDown(Hand.Right);
            for (int i = 0; i < 5; i++)
            {
                ElementAnimControl.ChangeRightIndex(1);
            }

            //timer.Start();

            // OnSceneOver(this, e);
        }
    }
}
