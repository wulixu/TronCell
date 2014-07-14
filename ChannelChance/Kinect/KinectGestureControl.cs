using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Samples.Kinect.WpfViewers;
using KinectChannel;
using System.Threading;
using System.Configuration;

namespace ChannelChance.Kinect
{
    public class KinectGestureControl : KinectControl
    {

        public event EventHandler<KinectGestureEventArgs> OnKinectGestureDetected;

        KinectSensorChooser sensorChooser = new KinectSensorChooser();
        private Skeleton[] skeletonData;
        private Dictionary<int, KinectPlayer> players = new Dictionary<int, KinectPlayer>();

        public KinectGestureControl()
        {
            sensorChooser.Start();
             
            this.KinectSensorManager = new Microsoft.Samples.Kinect.WpfViewers.KinectSensorManager();
            var kinectSensorBinding = new Binding("Kinect") { Source = sensorChooser };
            BindingOperations.SetBinding(KinectSensorManager, KinectSensorManager.KinectSensorProperty, kinectSensorBinding);
            this.KinectSensorManager.ElevationAngle = Convert.ToInt16(ConfigurationManager.AppSettings["Angle"]);


            Thread thread = new Thread(ClearUnActivePlayer);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        public void Stop()
        {
            sensorChooser.Stop();
            players.Clear();
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

        HandRSweepXDetector handRSweepXDetector = new HandRSweepXDetector();
        HandRSweepYDecector handRSweepYDecector = new HandRSweepYDecector();
        HandLSweepXDetector handLSweepXDetector = new HandLSweepXDetector();
        HandLSweepYDetector handLSweepYDetector = new HandLSweepYDetector();
        HandsRUPDetector handsRUPDetector = new HandsRUPDetector();
        HandsLUPDetector handsLUPDetector = new HandsLUPDetector();
        FlyingDetector flyingDetector = new FlyingDetector();

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
                            if (sskeleton.Joints.Count > 0)
                            {
                                KinectPlayer pplayer;
                                if (this.players.ContainsKey(sskeleton.TrackingId))
                                {
                                    pplayer = this.players[sskeleton.TrackingId];
                                }
                                else
                                {
                                    pplayer = new KinectPlayer(sskeleton);
                                    this.players.Add(sskeleton.TrackingId, pplayer);
                                }
                                pplayer.AddSketon(sskeleton);
                            }
                        }
                    }
                    
                    foreach (var item in players)
                    {
                        KinectPlayer p = item.Value;
                        if (!p.IsAlive)
                        {
                            continue;
                        }

                        //右手举了
                        if (handsRUPDetector.GetstureDetected(p))
                        {
                            this.RaiseEvent(new KinectGestureEventArgs()
                            {
                                GestureType = KinectGestureType.RightHandsUP
                            });
                        }
                        //左手举了
                        if (handsLUPDetector.GetstureDetected(p))
                        {
                            this.RaiseEvent(new KinectGestureEventArgs()
                            {
                                GestureType = KinectGestureType.LeftHandsUP
                            });
                        }

                        //right hand move on X
                        if (handRSweepXDetector.GetstureDetected(p))
                        {
                            this.RaiseEvent(new KinectGestureEventArgs()
                            {
                                GestureType = KinectGestureType.RightHandsMove,
                                ActionStep = Convert.ToInt16(Math.Ceiling(handRSweepXDetector.GestureDistance / handRSweepXDetector.GestureGateDistance)),
                                Distance = handRSweepXDetector.GestureDistance 
                            });
                        }

                        //left hand move on X
                        if (handLSweepXDetector.GetstureDetected(p))
                        {

                            this.RaiseEvent(new KinectGestureEventArgs()
                            {
                                GestureType = KinectGestureType.LeftHandsMove,
                                ActionStep = - Convert.ToInt16(Math.Ceiling(handLSweepXDetector.GestureDistance / handLSweepXDetector.GestureGateDistance)),
                                Distance = handLSweepXDetector.GestureGateDistance
                            });
                        }

                        ////right hand move on Y
                        if (handRSweepYDecector.GetstureDetected(p))
                        {
                            this.RaiseEvent(new KinectGestureEventArgs()
                            {
                                GestureType = KinectGestureType.RightHandsMoveY,
                                ActionStep = Convert.ToInt16(Math.Ceiling(handRSweepYDecector.GestureDistance / handRSweepYDecector.GestureGateDistance)),
                                Distance = handRSweepYDecector.GestureDistance
                            });
                        }
                        

                        ////left hand move on Y
                        if (handLSweepYDetector.GetstureDetected(p))
                        {
                            this.RaiseEvent(new KinectGestureEventArgs()
                           {
                               GestureType = KinectGestureType.LeftHandsMoveY,
                               ActionStep = Convert.ToInt16(Math.Ceiling(handLSweepYDetector.GestureDistance / handLSweepYDetector.GestureGateDistance)),
                               Distance = handLSweepYDetector.GestureDistance
                           });
                        } 
                    }

                    //flying
                    if (flyingDetector.Detected(players))
                    {
                        this.RaiseEvent(new KinectGestureEventArgs()
                        {
                            GestureType = KinectGestureType.Flying,
                            ActionStep = 0,
                            Distance = 0
                        });
                    }
                    else
                    {
                        this.RaiseEvent(new KinectGestureEventArgs()
                        {
                            GestureType = KinectGestureType.FlyEnd,
                            ActionStep = 0,
                            Distance = 0
                        }); 
                    }
                }
            }
        }

        private void ClearUnActivePlayer()
        {
            foreach (var player in this.players)
            {
                if (!player.Value.IsAlive)
                {
                    this.players.Remove(player.Value.TrackedId);
                    break;
                }
            }
            Thread.Sleep(3000);
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
