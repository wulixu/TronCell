using KinectChannel;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChannelChance.Kinect
{
    /// <summary>
    /// 有1个人飞就认为在飞
    /// </summary>
    public class FlyingDetector: GestureDetectorBase
    {
        private  bool IsFlying(KinectPlayer p)
        {
            PlayerJoints nowJoints = p.PlayerJoints.LastOrDefault();

            if (nowJoints != null)
            { 
                if (
                    nowJoints.ShoulderLeft.TrackingState == JointTrackingState.Tracked &&
                    nowJoints.ShoulderRight.TrackingState == JointTrackingState.Tracked &&
                    nowJoints.ElbowLeft.TrackingState == JointTrackingState.Tracked &&
                    nowJoints.ElbowRight.TrackingState == JointTrackingState.Tracked &&
                 nowJoints.ElbowLeft.Z > base.PlayerZDistance &&
                 nowJoints.ElbowRight.Z > base.PlayerZDistance &&
                 Math.Abs(nowJoints.ShoulderLeft.Y - nowJoints.ElbowLeft.Y) < 0.15 &&
                 Math.Abs(nowJoints.ShoulderRight.Y - nowJoints.ElbowRight.Y) < 0.15 &&
                 p.IsAlive)
                {
                    return true;
                };
            }

            return false;
        }

        public bool Detected(Dictionary<int, KinectPlayer> players)
        {
            foreach (var item in players)
            {
                if (!item.Value.IsAlive)
                {
                    continue;
                }
                if (IsFlying(item.Value))
                {
                    return true; ;
                }
            }
            return false;
        }
    }

    public class FlyDetector : GestureDetectorBase
    {
        public FlyDetector()
        {
            base.MinTimeDuration = 2000;
        }

        /// <summary>
        /// todo:只判断了首尾的状态。要中间也是保持状态
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public override bool GetstureDetected(KinectPlayer p)
        {
            PlayerJoints startJoints;
            PlayerJoints nowJoints;
            Get2Joints(p.PlayerJoints, out startJoints, out nowJoints);

            if (nowJoints != null && startJoints != null)
            {

                if (IsFly(startJoints) &&
                    IsFly(nowJoints) &&
                    nowJoints.TimeStamp.Subtract(startJoints.TimeStamp).TotalMilliseconds> base.MinTimeDuration)
                {
                    p.PlayerJoints.Clear();
                    return true;
                }
            }

            return false;
        }

        private bool IsFly(PlayerJoints player)
        {
            if (
                 player.HandRight.Z > base.PlayerZDistance &&
                 player.HandLeft.Z > base.PlayerZDistance &&
                 Math.Abs(player.HandLeft.Y - player.ElbowLeft.Y) < 0.10 &&
                 Math.Abs(player.HandRight.Y - player.ElbowRight.Y) < 0.10 )
            {
                return true;
            }
            return false;
        }

        protected override void Get2Joints(List<PlayerJoints> PlayerJoints, out PlayerJoints startJoints, out PlayerJoints nowJoints)
        {
            startJoints = null;
            nowJoints = PlayerJoints.LastOrDefault();
           
             
            for (int i = PlayerJoints.Count - 1; i > -1; i--)
            {
                if (IsFly(PlayerJoints[i]))
                {
                    startJoints = PlayerJoints[i];
                }
                else
                {
                    break;
                }
            }
           
        }
    }
}
