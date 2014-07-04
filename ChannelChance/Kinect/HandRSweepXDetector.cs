﻿using KinectChannel;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Kinect
{
    public class HandRSweepXDetector:GestureDetectorBase
    {
        public HandRSweepXDetector()
        {
        }

        public override bool GetstureDetected(List<PlayerJoints> PlayerJoints)
        {

            PlayerJoints startJoints;
            PlayerJoints nowJoints;
            Get2Joints(PlayerJoints, out startJoints, out nowJoints);

            if (nowJoints != null && startJoints != null)
            {
                JointData startHandRight = startJoints.HandRight;
                JointData nowHandRight = nowJoints.HandRight;
                JointData nowElbowRight = nowJoints.ElbowRight;

                if (startHandRight.TrackingState == JointTrackingState.Tracked &&
                    nowHandRight.TrackingState == JointTrackingState.Tracked &&
                    nowElbowRight.TrackingState == JointTrackingState.Tracked &&
                    nowHandRight.Z > base.PlayerZDistance &&
                    nowHandRight.Y > nowElbowRight.Y &&
                    Math.Abs(nowHandRight.X - startHandRight.X) > base.GestureGateDistance)
                {
                    base.GestureDistance = nowHandRight.X - startHandRight.X;
                    PlayerJoints.Clear();
                    return true;

                }
            }

            return false;
        }
    }
}
