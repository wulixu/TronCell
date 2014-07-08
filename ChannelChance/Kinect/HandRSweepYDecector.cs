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
        public HandRSweepYDecector()
        {
            base.GestureGateDistance = 0.04f;
        }

        public override bool GetstureDetected(KinectPlayer p)
        {
            PlayerJoints startJoints;
            PlayerJoints nowJoints;
            Get2Joints(p.PlayerJoints, out startJoints, out nowJoints);

            if (nowJoints != null && startJoints != null)
            {
                JointData startHand = startJoints.HandRight;
                JointData nowHand = nowJoints.HandRight;

                if (startHand.TrackingState == JointTrackingState.Tracked &&
                    nowHand.TrackingState == JointTrackingState.Tracked &&
                    nowHand.Z > base.PlayerZDistance &&
                    p.Z > base.PlayerZDistance &&
                    Math.Abs(nowHand.Y - startHand.Y) > base.GestureGateDistance)
                {
                    base.GestureDistance = nowHand.Y - startHand.Y;
                    p.PlayerJoints.Clear();
                    return true;

                }
            }

            return false;
        }
    }
}
