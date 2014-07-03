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
    /// StepTwoControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepTwoControl : UserControl
    {
        public StepTwoControl()
        {
            InitializeComponent();
            media.MediaEnded += OnSceneOver;
        }

        public event EventHandler SceneOver;

        private void OnSceneOver(object s, EventArgs e)
        {
            if (SceneOver != null)
                SceneOver(this, e);
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            ShowMedia(Appconfig.II_B_MP4);
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            ShowMedia(Appconfig.II_A_MP4);
        }

        private void ShowMedia(string mediaUri)
        {
            media.Visibility = Visibility.Visible;
            media.Source = new Uri(mediaUri);
        }
    }
}
