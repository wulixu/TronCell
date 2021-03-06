﻿using System;
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
            this.PlayerJoints = new List<PlayerJoints>();
            this.Z = skeleton.Position.Z;
        }

        public int TrackedId { get; set; }

        /// <summary>
        /// 人体Z的距离
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// Player的几个重要关节数据
        /// </summary>
        public List<PlayerJoints> PlayerJoints { get; set; }

        private DateTime TimeStamp { get; set; }

        //300ms没有更新skeleton，认为over了。
        public bool IsAlive
        {
            get
            { return (DateTime.Now.Subtract(this.TimeStamp).Milliseconds < 300); }
        }

        public void AddSketon(Skeleton skeleton)
        {
            this.PlayerJoints.Add(new PlayerJoints(skeleton));
            this.TimeStamp = DateTime.Now;
            if (PlayerJoints.Count > 35)
            {
                PlayerJoints jointsToRemove = PlayerJoints[0];
                PlayerJoints.Remove(jointsToRemove);
            }
        }
    }

    public class PlayerJoints
    {
        public PlayerJoints(Skeleton skeleton)
        {
            //todo:快速修正
            this.HandLeft = new JointData(skeleton.Joints[JointType.HandLeft]);
            this.HandRight = new JointData(skeleton.Joints[JointType.HandRight]);

            this.WristLeft = new JointData(skeleton.Joints[JointType.WristLeft]);
            this.WristRight = new JointData(skeleton.Joints[JointType.WristRight]); 

            this.ElbowLeft = new JointData(skeleton.Joints[JointType.ElbowLeft]);
            this.ElbowRight = new JointData(skeleton.Joints[JointType.ElbowRight]);
            this.ShoulderRight = new JointData(skeleton.Joints[JointType.ShoulderRight]);
            this.ShoulderLeft = new JointData(skeleton.Joints[JointType.ShoulderLeft]);
            this.TimeStamp = DateTime.Now;
        }

        public JointData HandRight { get; set; }
        public JointData HandLeft { get; set; }

        public JointData ElbowRight { get; set; }
        public JointData ElbowLeft { get; set; }

        public JointData ShoulderRight { get; set; }
        public JointData ShoulderLeft { get; set; }

        public JointData WristLeft { get; set; }
        public JointData WristRight { get; set; }

        public DateTime TimeStamp { get; set; }
    }

    public class JointData
    {
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

    }
}
