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
    /// StepFiveControl.xaml 的交互逻辑
    /// </summary>
    public partial class StepFiveControl : UserControl, IDirectionMove
    {
        public StepFiveControl(MainWindow window)
        {
            InitializeComponent();
            media.MediaEnded += OnSceneOver;
            Window = window;
            imgCode.Source = new BitmapImage(new Uri(Appconfig.TowDimensionPic));
        }

        public event EventHandler SceneOver;

        private void OnSceneOver(object s, EventArgs e)
        {
            if (SceneOver != null)
                SceneOver(this, e);
            _IsMediaPlaying = false;
            Window.Play();
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            ShowMedia(Appconfig.V_A_MP4);
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            ShowMedia(Appconfig.V_B_MP4);
        }

        private void ShowMedia(string mediaUri)
        {
            _IsMediaPlaying = true;
            Window.Pause();
            media.Visibility = Visibility.Visible;
            media.Source = new Uri(mediaUri);
        }

        public void Init()
        {
            imgLeft.Visibility = SeesawManager.Instance.HandDirection == HandDirection.L ? Visibility.Visible : Visibility.Collapsed;
            imgRight.Visibility = SeesawManager.Instance.HandDirection == HandDirection.R ? Visibility.Visible : Visibility.Collapsed;
            SeesawManager.Instance.HandDirection = HandDirection.None;
            SeesawManager.Instance.LeftHandTimes = SeesawManager.Instance.RightHandTimes = 0;
        }

        public MainWindow Window { get; set; }

        public void LeftHandMove(int count)
        {
            
        }

        public void RightHandMove(int count)
        {
            
        }

        public void LeftHandUp(int count)
        {
            
        }

        public void RightHandUp(int count)
        {
            
        }

        public void LeftHandsMoveY(int count)
        {
            if(count<0)
                ShowMedia(Appconfig.V_A_MP4);
        }

        public void RightHandsMoveY(int count)
        {
            if(count>0)
                ShowMedia(Appconfig.V_B_MP4);
        }
        private bool _IsMediaPlaying;
        public bool IsMediaPlaying
        {
            get { return _IsMediaPlaying; }
        }
    }
}