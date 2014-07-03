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

namespace ChannelChance.Controls
{
    /// <summary>
    /// StepFourControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepFourControl : UserControl
    {
        public StepFourControl()
        {
            InitializeComponent();
            media.MediaEnded += OnSceneOver;
        }

        public event EventHandler SceneOver;

        private void OnSceneOver(object s, EventArgs e)
        {
            if (SeesawManager.Instance.IsFinish)
            {
                SeesawManager.Instance.HandDirection = HandDirection.None;
                SeesawManager.Instance.LeftHandTimes = SeesawManager.Instance.RightHandTimes = 0;
                MessageBox.Show("The End!");
            }
            else
                media.Visibility = Visibility.Collapsed;
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            SeesawManager.Instance.HandDirection = HandDirection.L;
            SeesawManager.Instance.LeftHandTimes++;
            if (SeesawManager.Instance.CanPlayMP4)
                ShowMedia();
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            SeesawManager.Instance.HandDirection = HandDirection.R;
            SeesawManager.Instance.RightHandTimes++;
            if (SeesawManager.Instance.CanPlayMP4)
                ShowMedia();
        }

        private void ShowMedia()
        {
            media.Visibility = Visibility.Visible;
            media.Source = new Uri(SeesawManager.Instance.MP4Path);
        }
    }
}