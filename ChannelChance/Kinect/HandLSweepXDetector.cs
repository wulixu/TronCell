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
            base.GestureGateDistance = 0.04f;
        }

        public override bool GetstureDetected(KinectPlayer p)
        {

            PlayerJoints startJoints;
            PlayerJoints nowJoints;
            Get2Joints(p.PlayerJoints, out startJoints, out nowJoints);

            if (nowJoints != null && startJoints != null)
            {
                JointData startHandLeft = startJoints.HandLeft;
                JointData nowHandLeft = nowJoints.HandLeft;
                JointData nowElbowLeft = nowJoints.ElbowLeft;

                if (startHandLeft.TrackingState == JointTrackingState.Tracked &&
                    nowHandLeft.TrackingState == JointTrackingState.Tracked &&
                    nowElbowLeft.TrackingState == JointTrackingState.Tracked &&
                    nowHandLeft.Z > base.PlayerZDistance &&
                    p.Z > base.PlayerZDistance &&
                    nowHandLeft.Y - nowElbowLeft.Y > -0.1f &&
                    Math.Abs(nowHandLeft.X - startHandLeft.X) > base.GestureGateDistance)
                {
                    base.GestureDistance = nowHandLeft.X - startHandLeft.X;
                    p.PlayerJoints.Clear();
                    return true;
                }
            }

            return false;
        }
    }
}
