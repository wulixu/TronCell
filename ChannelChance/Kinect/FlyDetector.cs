using KinectChannel;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChannelChance.Kinect
{
    public class FlyDetector : GestureDetectorBase
    {
        public FlyDetector()
        {
            base.MinTimeDuration = 1000;
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
                    DateTime.Now.Subtract(startJoints.TimeStamp).TotalMilliseconds > base.MinTimeDuration)
                {
                    p.PlayerJoints.Clear();
                    return true;
                }
            }

            return false;
        }

        private bool IsFly(PlayerJoints player)
        {
            if (player.HandLeft.TrackingState == JointTrackingState.Tracked &&
                player.HandRight.TrackingState == JointTrackingState.Tracked &&
                player.ElbowLeft.TrackingState == JointTrackingState.Tracked &&
                player.ElbowRight.TrackingState == JointTrackingState.Tracked &&
                player.HandRight.Z > base.PlayerZDistance &&
                player.HandLeft.Z > base.PlayerZDistance &&
                Math.Abs(player.HandLeft.Y - player.ElbowLeft.Y) < 0.15 &&
                Math.Abs(player.HandRight.Y - player.ElbowRight.Y) < 0.15)
            {
                return true;
            }
            return false;
        }

        protected override void Get2Joints(List<PlayerJoints> PlayerJoints, out PlayerJoints startJoints, out PlayerJoints nowJoints)
        {
            startJoints = null;
            nowJoints = null;
            if (PlayerJoints.Count > 1)
            {
                nowJoints = PlayerJoints[PlayerJoints.Count - 1];
            }
            else
            {
                return;
            }
             
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
