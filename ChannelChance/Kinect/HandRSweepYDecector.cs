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
                JointData startHandLeft = startJoints.HandLeft;
                JointData nowHandLeft = nowJoints.HandLeft;

                if (startHandLeft.TrackingState == JointTrackingState.Tracked &&
                    nowHandLeft.TrackingState == JointTrackingState.Tracked &&
                    nowHandLeft.Z > base.PlayerZDistance &&
                    Math.Abs(nowHandLeft.Y - startHandLeft.Y) > base.GestureGateDistance)
                {
                    base.GestureDistance = nowHandLeft.Y - startHandLeft.Y;
                    PlayerJoints.Clear();
                    return true;

                }
            }

            return false;
        }
    }
}
