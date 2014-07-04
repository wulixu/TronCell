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
using System.Windows.Threading;
using ChannelChance.Common;

namespace ChannelChance.Controls
{
    /// <summary>
    /// StepTwoControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepTwoControl : UserControl, IDirectionMove
    {
        private DispatcherTimer timer;
        public StepTwoControl()
        {
            InitializeComponent();
            media.MediaEnded += OnSceneOver;
            ElementAnimControl.LeftCount = new int[5] { 5, 5, 5, 5, 5 };
            ElementAnimControl.RightCount = new int[5] { 5, 5, 5, 5, 5 };
            ElementAnimControl.Initial(Appconfig.CutImagesDirName);
            ElementAnimControl.PlayRightNextPage += i =>
            {
                ShowMedia(Appconfig.II_A_MP4);
            };
            ElementAnimControl.PlayLeftNextPage += i =>
            {
                ShowMedia(Appconfig.II_B_MP4);
            };

        }

        public event EventHandler SceneOver;

        private void OnSceneOver(object s, EventArgs e)
        {
            if (SceneOver != null)
                SceneOver(this, e);
        }

        private void ShowMedia(string mediaUri)
        {
            media.Visibility = Visibility.Visible;
            media.Source = new Uri(mediaUri);
        }

        public void LeftHandMove(int count)
        {
            for (int i = 0; i < count; i++)
            {
                ElementAnimControl.ChangeLeftIndex(1);
            }

        }

        public void RightHandMove(int count)
        {
            for (int i = 0; i < count; i++)
            {
                ElementAnimControl.ChangeRightIndex(1);
            }

        }

        public void LeftHandUp(int count)
        {
            ElementAnimControl.HandUpAndDown(HandDirection.L);
        }

        public void RightHandUp(int count)
        {
            ElementAnimControl.HandUpAndDown(HandDirection.R);
        }
    }
}
