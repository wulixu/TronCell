using KinectChannel;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Kinect
{
    public class HandsRUPDetector : GestureDetectorBase
    {
        public override bool GetstureDetected(List<PlayerJoints> PlayerJoints)
        {
            PlayerJoints startJoints;
            PlayerJoints nowJoints;
            Get2Joints(PlayerJoints, out startJoints, out nowJoints);

            if (nowJoints != null && startJoints != null)
            {
                JointData startHandRight = startJoints.HandRight;
                JointData startElbowRight = startJoints.ElbowRight;

                JointData nowHandRight = nowJoints.HandRight; 
                JointData nowElbowRight = nowJoints.ElbowRight;
                 
                //right hand up
                if (startHandRight.TrackingState == JointTrackingState.Tracked &&
                    startElbowRight.TrackingState == JointTrackingState.Tracked &&
                    nowHandRight.TrackingState == JointTrackingState.Tracked &&
                    nowElbowRight.TrackingState == JointTrackingState.Tracked &&
                    startHandRight.Y < startElbowRight.Y && 
                    nowHandRight.Y > nowElbowRight.Y  &&
                    nowHandRight.Z > base.PlayerZDistance)
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
