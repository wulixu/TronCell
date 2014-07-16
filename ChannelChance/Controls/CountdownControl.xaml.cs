using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace ChannelChance.Controls
{
    /// <summary>
    /// CountdownControl.xaml 的交互逻辑
    /// </summary>
    public partial class CountdownControl : UserControl
    {
        public Action CountdownCompleted
        {
            get;
            set;
        }
        private static readonly DependencyProperty NeddleAngleProperty = DependencyProperty.Register("NeddleAngle", typeof(double), typeof(CountdownControl), new PropertyMetadata(0d, new PropertyChangedCallback(NeddleAngleValueChangedCallBack)));
        private double NeddleAngle
        {
            set { this.SetValue(NeddleAngleProperty, value); }
            get { return (double)this.GetValue(NeddleAngleProperty); }
        }
        private static void NeddleAngleValueChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
        private bool isStorying = false;
        private Storyboard sbNeddleStory = null;
        private double totalAngle = 0d;
        public CountdownControl()
        {
            InitializeComponent();
            NeddleAngle = 0;
            lb.Content = "";
            sbNeddleStory = grid.FindResource("sbNeddle") as Storyboard;
            sbNeddleStory.Completed += sbNeddleStory_Completed;
        }

        void sbNeddleStory_Completed(object sender, EventArgs e)
        {
            sbNeddleStory.Remove(this);
            NeddleAngle = totalAngle;
            if (CountdownCompleted != null)
                CountdownCompleted();
            isStorying = false;
        }
        public void BeginCountdown()
        {
            if (!isStorying)
            {
                this.Visibility = Visibility.Visible;
                isStorying = true;
                sbNeddleStory.Begin(this,true);
            }
        }
        //public void StopCountdown()
        //{
        //    if (isStorying)
        //    {
        //        this.Visibility = Visibility.Collapsed;
        //        sbNeddleStory.Stop();
        //        isStorying = false;
        //    }
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bigCircleRadius">大圆半径</param>
        /// <param name="smallCircleRadius">小圆半径</param>
        /// <param name="fontSize">字体大小</param>
        /// <param name="count">计数总量</param>
        public void Initial(double bigCircleRadius, double smallCircleRadius, double fontSize,int count)
        {
            totalAngle = 360 * count;
            sbNeddleStory.Duration = new Duration(TimeSpan.FromSeconds(count + 0.1));
            DoubleAnimation da = sbNeddleStory.Children[0] as DoubleAnimation;
            da.From = 0d;
            da.To = totalAngle;
            da.Duration = new Duration(TimeSpan.FromSeconds(count));
            lb.FontSize = fontSize;
            Binding bindlb = new Binding();
            bindlb.Source = this;
            bindlb.Path = new PropertyPath(NeddleAngleProperty);
            bindlb.Converter = new LabelContentConverter();
            bindlb.ConverterParameter = totalAngle;
            BindingOperations.SetBinding(lb, Label.ContentProperty, bindlb);
            grid.Width = 2 * bigCircleRadius;
            grid.Height = 2 * bigCircleRadius;
            ellipse.StrokeThickness = bigCircleRadius - smallCircleRadius;
            PathFigure pathFigure = new PathFigure { IsClosed = true };
            pathFigure.StartPoint = new Point(bigCircleRadius, bigCircleRadius - smallCircleRadius);
            pathFigure.Segments.Add(new LineSegment { Point = new Point(bigCircleRadius, 0) });
            ArcSegment arc = new ArcSegment
            {
                Point = new Point(2 * bigCircleRadius, bigCircleRadius),
                Size = new Size(bigCircleRadius, bigCircleRadius),
                SweepDirection = SweepDirection.Clockwise,

            };

            Binding bind = new Binding();
            bind.Source = this;
            bind.Path = new PropertyPath(NeddleAngleProperty);
            bind.Converter = new PointTypeConverter(bigCircleRadius);
            bind.ConverterParameter = bigCircleRadius;
            BindingOperations.SetBinding(arc, ArcSegment.PointProperty, bind);
            Binding bindIsLargeCircle = new Binding();
            bindIsLargeCircle.Source = this;
            bindIsLargeCircle.Path = new PropertyPath(NeddleAngleProperty);
            bindIsLargeCircle.Converter = new IsLargeCircleConverter();
            BindingOperations.SetBinding(arc, ArcSegment.IsLargeArcProperty, bindIsLargeCircle);
            pathFigure.Segments.Add(arc
             );
            LineSegment line = new LineSegment { Point = new Point(bigCircleRadius + smallCircleRadius, bigCircleRadius) };
            Binding bind1 = new Binding();
            bind1.Source = this;
            bind1.Path = new PropertyPath(NeddleAngleProperty);
            bind1.Converter = new PointTypeConverter(bigCircleRadius);
            bind1.ConverterParameter = smallCircleRadius;
            BindingOperations.SetBinding(line, LineSegment.PointProperty, bind1);
            pathFigure.Segments.Add(line);
            ArcSegment arc1 = new ArcSegment
            {
                Point = new Point(bigCircleRadius, bigCircleRadius - smallCircleRadius),
                Size = new Size(smallCircleRadius, smallCircleRadius),
                SweepDirection = SweepDirection.Counterclockwise
            };
            Binding bindIsLargeCircle1 = new Binding();
            bindIsLargeCircle1.Source = this;
            bindIsLargeCircle1.Path = new PropertyPath(NeddleAngleProperty);
            bindIsLargeCircle1.Converter = new IsLargeCircleConverter();
            BindingOperations.SetBinding(arc1, ArcSegment.IsLargeArcProperty, bindIsLargeCircle1);
            pathFigure.Segments.Add(arc1);
            path.Data = new PathGeometry(new PathFigureCollection() { pathFigure });

        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    public class LabelContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double currentAngle = (double)value;//当前角度
            double AngleTotal = double.Parse(parameter.ToString());//1080

            int contentValue = (int)(AngleTotal - currentAngle) / 360;
            if (currentAngle % 360 != 0)
                contentValue += 1;
            //if (currentAngle < AngleTotal && currentAngle % 360 == 0)
            //    contentValue -= 1;
            return contentValue.ToString() + "\"";
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
    public class IsLargeCircleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double p = (double)value % 360;
            if (p >= 180 || ((double)value > 0 && p == 0))
                return true;
            else
                return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
    public class PointTypeConverter : IValueConverter
    {
        public double XOffset
        {
            get;
            set;
        }
        public PointTypeConverter(double xOffset)
        {
            XOffset = xOffset;
        }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Point returnP = new Point(0, 0);
            double radius = double.Parse(parameter.ToString());
            double angle = Math.PI * (double)value / 180.0;
            returnP.X = Math.Sin(angle) * radius + XOffset;
            returnP.Y = XOffset - Math.Cos(angle) * radius;
            return returnP;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
