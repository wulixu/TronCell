using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace KinectChannel
{
    public class KinectPlayer
    {
        public KinectPlayer(Skeleton skeleton)
        {
            this.TrackedId = skeleton.TrackingId;
            this.TimeStamp = DateTime.Now;
            CurrentArmJoints = new ArmJoints(skeleton);
            LastFrameArmJoints = new ArmJoints(skeleton);

            this.Z = skeleton.Position.Z;
        }

        public int TrackedId { get; set; }

        /// <summary>
        /// 人体Z的距离
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// 上一Frame的几个重要关节数据
        /// </summary>
        public ArmJoints LastFrameArmJoints { get; set; }

        /// <summary>
        /// 当前Frame的几个重要关节数据
        /// </summary>
        public ArmJoints CurrentArmJoints { get; set; }

        private DateTime TimeStamp { get; set; }

        //300ms没有更新skeleton，认为over了。
        public bool IsAlive
        {
            get
            { return (DateTime.Now.Subtract(this.TimeStamp).Milliseconds < 300); }
        }

        public void UpdateSketon(Skeleton skeleton)
        {
            //this.LastFrameArmJoints = CurrentArmJoints.Clone() as ArmJoints;

            this.LastFrameArmJoints.ElbowLeft.X = this.CurrentArmJoints.ElbowLeft.X;
            this.LastFrameArmJoints.ElbowLeft.Y = this.CurrentArmJoints.ElbowLeft.Y;
            this.LastFrameArmJoints.ElbowLeft.Z = this.CurrentArmJoints.ElbowLeft.Z;
            this.LastFrameArmJoints.ElbowLeft.TrackingState = this.CurrentArmJoints.ElbowLeft.TrackingState;

            this.LastFrameArmJoints.ElbowRight.X = this.CurrentArmJoints.ElbowRight.X;
            this.LastFrameArmJoints.ElbowRight.Y = this.CurrentArmJoints.ElbowRight.Y;
            this.LastFrameArmJoints.ElbowRight.Z = this.CurrentArmJoints.ElbowRight.Z;
            this.LastFrameArmJoints.ElbowRight.TrackingState = this.CurrentArmJoints.ElbowRight.TrackingState;

            this.LastFrameArmJoints.HandRight.X = this.CurrentArmJoints.HandRight.X;
            this.LastFrameArmJoints.HandRight.Y = this.CurrentArmJoints.HandRight.Y;
            this.LastFrameArmJoints.HandRight.Z = this.CurrentArmJoints.HandRight.Z;
            this.LastFrameArmJoints.HandRight.TrackingState = this.CurrentArmJoints.HandRight.TrackingState;

            this.LastFrameArmJoints.HandLeft.X = this.CurrentArmJoints.HandLeft.X;
            this.LastFrameArmJoints.HandLeft.Y = this.CurrentArmJoints.HandLeft.Y;
            this.LastFrameArmJoints.HandLeft.Z = this.CurrentArmJoints.HandLeft.Z;
            this.LastFrameArmJoints.HandLeft.TrackingState = this.CurrentArmJoints.HandLeft.TrackingState;


            this.CurrentArmJoints = new ArmJoints(skeleton);

            this.TimeStamp = DateTime.Now;
        }
    }

    public class ArmJoints : ICloneable
    {
        private ArmJoints() { }

        public ArmJoints(Skeleton skeleton)
        {
            this.HandLeft = new JointData(skeleton.Joints[JointType.HandLeft]);
            this.HandRight = new JointData(skeleton.Joints[JointType.HandRight]);
            this.ElbowLeft = new JointData(skeleton.Joints[JointType.ElbowLeft]);
            this.ElbowRight = new JointData(skeleton.Joints[JointType.ElbowRight]);
        }

        public JointData HandRight { get; set; }
        public JointData HandLeft { get; set; }

        public JointData ElbowRight { get; set; }
        public JointData ElbowLeft { get; set; }

        public object Clone()
        {
            ArmJoints armJoints = new ArmJoints();
            armJoints.HandLeft = this.HandLeft.Clone() as JointData;
            armJoints.HandRight = this.HandRight.Clone() as JointData;
            armJoints.ElbowLeft = this.ElbowLeft.Clone() as JointData;
            armJoints.ElbowRight = this.ElbowRight.Clone() as JointData;

            return armJoints;
        }
    }

    public class JointData : ICloneable
    {
        private JointData() { }

        public JointData(Joint joint)
        {
            this.X = joint.Position.X;
            this.Y = joint.Position.Y;
            this.Z = joint.Position.Z;
            this.TrackingState = joint.TrackingState;
        }
        public float X { get; set; }
        public float Y { get; set; }

        public float Z { get; set; }

        public JointTrackingState TrackingState { get; set; }

        public object Clone()
        {
            JointData jointData = new JointData();
            jointData.X = this.X;
            jointData.Y = this.Y;
            jointData.Z = this.Z;
            jointData.TrackingState = this.TrackingState;
            return jointData;
        }
    }
}
