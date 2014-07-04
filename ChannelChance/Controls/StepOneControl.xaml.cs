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
    public partial class StepOneControl : UserControl, IDirectionMove
    {
        private bool _isMediaPlaying;

        public StepOneControl()
        {
            InitializeComponent();
            media.MediaEnded += OnSceneOver;

            ElementAnimControl.LeftCount = new int[5] { 5, 5, 5, 5, 2 };
            ElementAnimControl.RightCount = new int[5] { 2, 5, 5, 5, 5 };
            ElementAnimControl.Initial(Appconfig.TiltImagesDirName);
            ElementAnimControl.PlayRightNextPage += i =>
            {
                _isMediaPlaying = true;
                ShowMedia(Appconfig.I_A_MP4);
            };
            ElementAnimControl.PlayLeftNextPage += i =>
            {
                _isMediaPlaying = true;
                ShowMedia(Appconfig.I_B_MP4);
            };
        }

        public event EventHandler SceneOver;

        private void OnSceneOver(object s, EventArgs e)
        {
            if (SceneOver != null)
                SceneOver(this, e);
            media.Visibility = Visibility.Collapsed;
            _isMediaPlaying = false;
        }

        private void ShowMedia(string mediaUri)
        {
            media.Visibility = Visibility.Visible;
            media.Source = new Uri(mediaUri);
        }

        public void LeftHandMove(int count)
        {
            var length = Math.Abs(count);
            for (int i = 0; i < length; i++)
            {
                ElementAnimControl.ChangeLeftIndex(Appconfig.ToRorL(count));
                if (count > 0)
                    leftEllipseAnimControl.MoveToNext();
                else
                    leftEllipseAnimControl.MoveToLast();
            }
        }

        public void RightHandMove(int count)
        {
            var length = Math.Abs(count);
            for (int i = 0; i < length; i++)
            {
                ElementAnimControl.ChangeRightIndex(Appconfig.ToRorL(count));
                if (count > 0)
                    rightEllipseAnimControl.MoveToNext();
                else
                    rightEllipseAnimControl.MoveToLast();
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

        public void LeftHandsMoveY(int count)
        {
            
        }

        public void RightHandsMoveY(int count)
        {
            
        }

        public bool IsMediaPlaying
        {
            get { return _isMediaPlaying; }
        }
    }
}
