﻿using ChannelChance.Common;
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
using ChannelChance.Common;

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
            media.MediaEnded += OnSceneOver;

            ElementAnimControl.LeftCount = new int[5] { 5, 5, 5, 5, 2 };
            ElementAnimControl.RightCount = new int[5] { 2, 5, 5, 5, 5 };
            ElementAnimControl.Initial(Appconfig.TiltImagesDirName);
            ElementAnimControl.PlayRightNextPage += i =>
            {
                ShowMedia(Appconfig.I_A_MP4);
            };
            ElementAnimControl.PlayLeftNextPage += i =>
            {
                ShowMedia(Appconfig.I_B_MP4);
            };
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
                leftEllipseAnimControl.MoveToNext();
            }
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                ElementAnimControl.ChangeRightIndex(1);
                rightEllipseAnimControl.MoveToNext();
            }
        }

        private void ShowMedia(string mediaUri)
        {
            media.Visibility = Visibility.Visible;
            media.Source = new Uri(mediaUri);
        }
    }
}
