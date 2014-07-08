using KinectChannel;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Kinect
{
    public class HandLSweepYDetector:GestureDetectorBase
    {
        public HandLSweepYDetector()
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

                if (startHandLeft.TrackingState == JointTrackingState.Tracked &&
                    nowHandLeft.TrackingState == JointTrackingState.Tracked &&
                    nowHandLeft.Z > base.PlayerZDistance &&
                    p.Z  >  base.PlayerZDistance &&
                    Math.Abs(nowHandLeft.Y - startHandLeft.Y) > base.GestureGateDistance)
                {
                    base.GestureDistance = nowHandLeft.Y - startHandLeft.Y;
                    p.PlayerJoints.Clear();
                    return true;

                }
            }

            return false;
        }
    }
}
