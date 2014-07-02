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
    /// StepOneControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepOneControl : UserControl
    {
        public StepOneControl()
        {
            InitializeComponent();
        }

        public event EventHandler SceneOver;
        private void OnSceneOver(object s,EventArgs e)
        {
            if (SceneOver != null)
                SceneOver(s, e);
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            OnSceneOver(this, e);
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            OnSceneOver(this, e);
        }
    }
}
