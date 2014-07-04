using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Samples.Kinect.WpfViewers;

namespace ChannelChance.Kinect
{
    public class KinectGestureControl : KinectControl
    {

        public event EventHandler<KinectGestureEventArgs> OnKinectGestureDetected;

        KinectSensorChooser sensorChooser;
        private Skeleton[] skeletonData;
        private Dictionary<int, KinectPlayer> players;

        public KinectGestureControl()
        {
            sensorChooser = new KinectSensorChooser();
            players = new Dictionary<int, KinectPlayer>();
            this.KinectSensorManager = new Microsoft.Samples.Kinect.WpfViewers.KinectSensorManager();
            sensorChooser.Start();

            // Bind the KinectSensor from the sensorChooser to the KinectSensorManager
            var kinectSensorBinding = new Binding("Kinect") { Source = sensorChooser };
            BindingOperations.SetBinding(KinectSensorManager, KinectSensorManager.KinectSensorProperty, kinectSensorBinding);
        }
        public void Stop()
        {
            sensorChooser.Stop();
        }

        protected override void OnKinectSensorChanged(object sender, KinectSensorManagerEventArgs<KinectSensor> args)
        {
            base.OnKinectSensorChanged(sender, args);
            if (null != args.OldValue)
            {
                this.UninitializeKinectServices(args.OldValue);
            }

            if (null != args.NewValue)
            {
                this.InitializeKinectServices(KinectSensorManager, args.NewValue);
            }
        }

        #region Kinect discovery + setup

        // Kinect enabled apps should customize which Kinect services it initializes here.
        private void InitializeKinectServices(KinectSensorManager kinectSensorManager, KinectSensor sensor)
        {
            // Application should enable all streams first.
            kinectSensorManager.ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;
            kinectSensorManager.ColorStreamEnabled = true;

            sensor.SkeletonFrameReady += this.SkeletonsReady;
            kinectSensorManager.TransformSmoothParameters = new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            };
            kinectSensorManager.SkeletonStreamEnabled = true;
            kinectSensorManager.KinectSensorEnabled = true;
        }

        // Kinect enabled apps should uninitialize all Kinect services that were initialized in InitializeKinectServices() here.
        private void UninitializeKinectServices(KinectSensor sensor)
        {
            sensor.SkeletonFrameReady -= this.SkeletonsReady;
        }

        #endregion Kinect discovery + setup


        #region Kinect Skeleton processing
        float stepDistance = 0.03f;//一部的距离
        float minDistance = 1.2f;
        private void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    if ((this.skeletonData == null) || (this.skeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }
                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);


                    //start by wyf
                    foreach (Skeleton sskeleton in this.skeletonData)//6个人
                    {
                        if (SkeletonTrackingState.Tracked == sskeleton.TrackingState)
                        {
                            KinectPlayer pplayer;
                            if (this.players.ContainsKey(sskeleton.TrackingId))
                            {
                                pplayer = this.players[sskeleton.TrackingId];
                                pplayer.UpdateSketon(sskeleton);
                            }
                            else
                            {
                                pplayer = new KinectPlayer(sskeleton);
                                this.players.Add(sskeleton.TrackingId, pplayer);
                            }
                        }
                    }
                    //foreach (var player in this.players)
                    //{
                    //    if (!player.Value.IsAlive)
                    //    {
                    //        this.players.Remove(player.Value.TrackedId);
                    //        break;
                    //    }
                    //}
                    foreach (var item in players)
                    {
                        KinectPlayer p = item.Value;
                        if (!p.IsAlive)
                        {
                            continue;
                        }
                        JointData lastHandRight = p.LastFrameArmJoints.HandRight;
                        JointData lastHandLeft = p.LastFrameArmJoints.HandLeft;
                        JointData lastElbowRight = p.LastFrameArmJoints.ElbowRight;
                        JointData lastElbowLeft = p.LastFrameArmJoints.ElbowLeft;

                        JointData nowHandRight = p.CurrentArmJoints.HandRight;
                        JointData nowHandLeft = p.CurrentArmJoints.HandLeft;
                        JointData nowElbowRight = p.CurrentArmJoints.ElbowRight;
                        JointData nowElbowLeft = p.CurrentArmJoints.ElbowLeft;

                        //right hand up
                        if (lastHandRight.TrackingState == JointTrackingState.Tracked &&
                            lastElbowRight.TrackingState == JointTrackingState.Tracked &&
                            nowHandRight.TrackingState == JointTrackingState.Tracked &&
                            nowElbowRight.TrackingState == JointTrackingState.Tracked &&
                            lastHandRight.Y < lastElbowRight.Y && (nowHandRight.Y - nowElbowRight.Y) > 0.001
                            && p.Z > minDistance && nowHandRight.Z > minDistance)
                        {
                            this.RaiseEvent(new KinectGestureEventArgs()
                            {
                                GestureType = KinectGestureType.RightHandsUP,
                                ActionStep = Convert.ToInt16(Math.Ceiling((nowHandRight.Y - nowElbowRight.Y) / stepDistance)),
                                Distance = nowHandRight.Y - nowElbowRight.Y
                            });
                        }
                        //left hand up
                        if (lastHandLeft.TrackingState == JointTrackingState.Tracked &&
                            lastElbowLeft.TrackingState == JointTrackingState.Tracked &&
                            nowHandLeft.TrackingState == JointTrackingState.Tracked &&
                            nowElbowLeft.TrackingState == JointTrackingState.Tracked &&
                            lastHandLeft.Y < lastElbowLeft.Y && (nowHandLeft.Y - nowElbowLeft.Y) > 0.001 &&
                            p.Z > minDistance && nowHandLeft.Z > minDistance)
                        {
                            this.RaiseEvent(new KinectGestureEventArgs()
                            {
                                GestureType = KinectGestureType.LeftHandsUP,
                                ActionStep = Convert.ToInt16(Math.Ceiling((nowHandLeft.Y - nowElbowLeft.Y) / stepDistance)),
                                Distance = nowHandLeft.Y - nowElbowLeft.Y
                            });
                        }

                        //right hand move
                        if (lastHandRight.TrackingState == JointTrackingState.Tracked &&
                            nowHandRight.TrackingState == JointTrackingState.Tracked &&
                            nowElbowRight.TrackingState == JointTrackingState.Tracked &&
                            p.Z > minDistance && nowHandRight.Z > minDistance &&
                            Math.Abs(nowHandRight.Y - lastHandRight.Y) < 0.05 &&
                            nowHandRight.Y > nowElbowRight.Y &&
                            Math.Abs(nowHandRight.X - lastHandRight.X) > 0.03)
                        {
                            this.RaiseEvent(new KinectGestureEventArgs()
                            {
                                GestureType = KinectGestureType.RightHandsMove,
                                ActionStep = Convert.ToInt16(Math.Ceiling((nowHandRight.X - lastHandRight.X) / stepDistance)),
                                Distance = nowHandRight.X - lastHandRight.X
                            });
                        }
                        //left hand move
                        if (lastHandLeft.TrackingState == JointTrackingState.Tracked &&
                            nowHandLeft.TrackingState == JointTrackingState.Tracked &&
                            nowElbowLeft.TrackingState == JointTrackingState.Tracked &&
                            p.Z > minDistance && nowHandLeft.Z > minDistance &&
                            Math.Abs(nowHandLeft.Y - lastHandLeft.Y) < 0.05 &&
                            nowHandLeft.Y > nowElbowLeft.Y &&
                            Math.Abs(nowHandLeft.X - lastHandLeft.X) > 0.03)
                        {
                            this.RaiseEvent(new KinectGestureEventArgs()
                            {
                                GestureType = KinectGestureType.LeftHandsMove,
                                ActionStep = Convert.ToInt16(Math.Ceiling((nowHandLeft.Y - lastHandLeft.Y) / stepDistance)),
                                Distance = nowHandLeft.Y - lastHandLeft.Y
                            });
                        }
                    }


                }
            }
        }


        private void RaiseEvent(KinectGestureEventArgs args)
        {
            if (OnKinectGestureDetected != null)
            {
                OnKinectGestureDetected(null, args);
            }
        }

        #endregion Kinect Skeleton processing
    }
}
