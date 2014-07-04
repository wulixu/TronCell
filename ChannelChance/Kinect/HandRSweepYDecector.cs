using KinectChannel;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Kinect
{
    public class HandRSweepYDecector:GestureDetectorBase
    {
        public override bool GetstureDetected(List<PlayerJoints> PlayerJoints)
        {
            PlayerJoints startJoints;
            PlayerJoints nowJoints;
            Get2Joints(PlayerJoints, out startJoints, out nowJoints);

            if (nowJoints != null && startJoints != null)
            {
                JointData startHand = startJoints.HandRight;
                JointData nowHand = nowJoints.HandRight;

                if (startHand.TrackingState == JointTrackingState.Tracked &&
                    nowHand.TrackingState == JointTrackingState.Tracked &&
                    nowHand.Z > base.PlayerZDistance &&
                    Math.Abs(nowHand.Y - startHand.Y) > base.GestureGateDistance)
                {
                    base.GestureDistance = nowHand.Y - startHand.Y;
                    PlayerJoints.Clear();
                    return true;

                }
            }

            return false;
        }
    }
}
