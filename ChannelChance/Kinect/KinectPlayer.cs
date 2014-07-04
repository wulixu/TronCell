using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace ChannelChance.Kinect
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

        public float Z { get; set; }
        public ArmJoints LastFrameArmJoints { get; set; }

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
            this.LastFrameArmJoints = CurrentArmJoints.Clone() as ArmJoints;
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
