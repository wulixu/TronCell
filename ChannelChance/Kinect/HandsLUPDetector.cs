using KinectChannel;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Kinect
{
    public class HandsLUPDetector:GestureDetectorBase
    {
        public override bool GetstureDetected(List<PlayerJoints> PlayerJoints)
        {
            PlayerJoints startJoints;
            PlayerJoints nowJoints;
            Get2Joints(PlayerJoints, out startJoints, out nowJoints);

            if (nowJoints != null && startJoints != null)
            {
                JointData startHandLeft = startJoints.HandLeft;
                JointData startElbowLeft = startJoints.ElbowLeft;

                JointData nowHandLeft = nowJoints.HandLeft;
                JointData nowElbowLeft = nowJoints.ElbowLeft;

                //right hand up
                if (startHandLeft.TrackingState == JointTrackingState.Tracked &&
                    startElbowLeft.TrackingState == JointTrackingState.Tracked &&
                    nowHandLeft.TrackingState == JointTrackingState.Tracked &&
                    nowElbowLeft.TrackingState == JointTrackingState.Tracked &&
                    startHandLeft.Y < startElbowLeft.Y &&
                    nowHandLeft.Y > nowElbowLeft.Y &&
                    nowHandLeft.Z > base.PlayerZDistance)
                {
                    base.GestureDistance = 0f;
                    PlayerJoints.Clear();
                    return true;
                }
            }

            return false;
        }    
    }
}
