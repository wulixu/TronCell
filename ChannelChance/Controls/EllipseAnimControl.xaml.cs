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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ChannelChance.Controls
{
    /// <summary>
    /// EllipseAnimControl.xaml 的交互逻辑
    /// </summary>
    public partial class EllipseAnimControl : UserControl
    {
        private const int autoMoveInterval = 4;
        private const double deltaX = 52d;
        private const int ellipseCount = 8;
        private int bigEllipseIndex = 0;
        private bool isAutoComplted = true;
        private DispatcherTimer timer = null;
        public static readonly DependencyProperty FromProperty = DependencyProperty.Register(
    "From", typeof(Thickness), typeof(EllipseAnimControl), new PropertyMetadata(new Thickness(0, 0, 0, 0)));
        public static readonly DependencyProperty ToProperty = DependencyProperty.Register(
    "To", typeof(Thickness), typeof(EllipseAnimControl), new PropertyMetadata(new Thickness(0, 0, 0, 0)));
        private Storyboard sbMannual = null;
        private Storyboard sbAuto = null;
        public Thickness From
        {
            get { return (Thickness)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }
        public Thickness To
        {
            get { return (Thickness)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }
        public EllipseAnimControl()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 3);
            timer.Tick += timer_Tick; timer.Start();
            sbMannual = this.FindResource("sb1") as Storyboard;
            sbAuto = this.FindResource("sb2") as Storyboard;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if(isAutoComplted)
                BeginAutoMove();
        }

        public void BeginAutoMove()
        {
            sbAuto.Begin(e8, true);
            isAutoComplted = false;
        }
        public void StopAutoMove()
        {
            sbAuto.Remove(e8);
            e8.Margin = new Thickness(0, 0, 0, 0);
            e8.Opacity = 1;
            isAutoComplted = true;
        }
        public void MoveToLast()
        {
            if (!isAutoComplted) StopAutoMove();
            this.timer.Stop();
            this.timer.Start();
            if (bigEllipseIndex <= 0) return;
            bigEllipseIndex--;
            sbMannual.Remove(e8);
            From = new Thickness(e8.Margin.Left, 0, 0, 0);
            To = new Thickness(To.Left - deltaX, 0, 0, 0);
            sbMannual.Begin(e8, true);
            //isAnimCompleted = false;
        }
        public void MoveToNext()
        {
            if (!isAutoComplted) StopAutoMove();
            this.timer.Stop();
            this.timer.Start();
            if (bigEllipseIndex >= ellipseCount - 1) return;
            bigEllipseIndex++;
            sbMannual.Remove(e8);
            From = new Thickness(e8.Margin.Left, 0, 0, 0);
            To = new Thickness(To.Left + deltaX, 0, 0, 0);
            sbMannual.Begin(e8, true);
            //isAnimCompleted = false;
        }
        public void Reset()
        {
            sbMannual.Remove(e8);
            sbAuto.Remove(e8);
            e8.Margin = new Thickness(0, 0, 0, 0);
            e8.Opacity = 1;
            From = new Thickness(0, 0, 0, 0);
            isAutoComplted = true;
        }
    }
}
