using KinectChannel;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Kinect
{
    public class HandLSweepXDetector:GestureDetectorBase
    {
        public HandLSweepXDetector()
        {
            
        }

        public override bool GetstureDetected(List<PlayerJoints> PlayerJoints)
        {

            PlayerJoints startJoints;
            PlayerJoints nowJoints;
            Get2Joints(PlayerJoints, out startJoints, out nowJoints);

            if (nowJoints != null && startJoints != null)
            {
                JointData startHandLeft = startJoints.HandLeft;
                JointData nowHandLeft = nowJoints.HandLeft;
                JointData nowElbowLeft = nowJoints.ElbowLeft;

                if (startHandLeft.TrackingState == JointTrackingState.Tracked &&
                    nowHandLeft.TrackingState == JointTrackingState.Tracked &&
                    nowElbowLeft.TrackingState == JointTrackingState.Tracked &&
                    nowHandLeft.Z > base.PlayerZDistance &&
                    nowHandLeft.Y > nowElbowLeft.Y &&
                    Math.Abs(nowHandLeft.X - startHandLeft.X) > base.GestureGateDistance)
                {
                    base.GestureDistance = nowHandLeft.X - startHandLeft.X;
                    PlayerJoints.Clear();
                    return true;
                }
            }

            return false;
        }
    }
}
