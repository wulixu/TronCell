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
        public HandsRUPDetector()
        {
            base.Orentation = GestureOrentation.Right;
        }

        public override bool GetstureDetected(KinectPlayer p)
        {
            //PlayerJoints startJoints;
            //PlayerJoints nowJoints;
            //Get2Joints(p.PlayerJoints, out startJoints, out nowJoints);

            //if (nowJoints != null && startJoints != null)
            //{
            //    JointData startHandRight = startJoints.HandRight;
            //    JointData startElbowRight = startJoints.ElbowRight;

            //    JointData nowHandRight = nowJoints.HandRight; 
            //    JointData nowElbowRight = nowJoints.ElbowRight;
                 
            //    //right hand up
            //    if (startHandRight.TrackingState == JointTrackingState.Tracked &&
            //        startElbowRight.TrackingState == JointTrackingState.Tracked &&
            //        nowHandRight.TrackingState == JointTrackingState.Tracked &&
            //        nowElbowRight.TrackingState == JointTrackingState.Tracked &&
            //        startHandRight.Y < startElbowRight.Y && 
            //        nowHandRight.Y > nowElbowRight.Y  &&
            //        p.Z > base.PlayerZDistance &&
            //        nowHandRight.Z > base.PlayerZDistance)
            //    {
            //        base.GestureDistance = 0f;
            //        p.PlayerJoints.Clear();
            //        return true;
            //    }
            //}

            //return false;
            return base.GetstureDetected(p);
        }
    }
}
