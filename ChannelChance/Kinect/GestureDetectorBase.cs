using KinectChannel;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelChance.Kinect
{
    public abstract class GestureDetectorBase
    {
        /// <summary>
        /// 人所在的最近距离
        /// </summary>
        public float PlayerZDistance { get; set; }
        //动作持续最少时间ms
        public int MinTimeDuration { get; set; }
        /// <summary>
        /// 动作持续最长时间ms
        /// </summary>
        public int MaxTimeDuration { get; set; }
        /// <summary>
        /// 动作持续总共行程
        /// </summary>
        public float GestureDistance { get; set; }

        /// <summary>
        /// 动作阈值
        /// </summary>
        public float GestureGateDistance { get; set; }

        /// <summary>
        /// 动作的方向
        /// </summary>
        public GestureOrentation Orentation { get; set; }

        public GestureDetectorBase()
        {
            this.MinTimeDuration = 30;
            this.MaxTimeDuration = 300;
            this.PlayerZDistance = 1.0f;
            this.GestureGateDistance = 0.035f;
            this.Orentation = GestureOrentation.Right;
        }

        /// <summary>
        /// 是否识别动作
        /// </summary>
        /// <param name="PlayerJoints"></param>
        /// <returns></returns>
        public virtual bool GetstureDetected( KinectPlayer p)
        {
            PlayerJoints startJoints;
            PlayerJoints nowJoints;
            Get2Joints(p.PlayerJoints, out startJoints, out nowJoints);

            if (nowJoints != null && startJoints != null)
            {
                switch (Orentation)
                {
                    case GestureOrentation.Left:
                        return IsGesture(p, startJoints.HandLeft, nowJoints.HandLeft) ||
                               IsGesture(p, startJoints.WristLeft, nowJoints.WristLeft);
                    case GestureOrentation.Right:
                        return IsGesture(p, startJoints.HandRight, nowJoints.HandRight) ||
                               IsGesture(p, startJoints.WristRight, nowJoints.WristRight);;
                }
                
            }

            return false;
        }

        protected bool IsGesture(KinectPlayer p, JointData startJoint, JointData nowJoint)
        {
            if (startJoint.TrackingState == JointTrackingState.Tracked &&
                nowJoint.TrackingState == JointTrackingState.Tracked &&
                startJoint.Z > this.PlayerZDistance &&
                nowJoint.Z > this.PlayerZDistance &&
                p.Z > this.PlayerZDistance )
            {
               
                this.GestureDistance = MovedDistance(startJoint, nowJoint);
                if (nowJoint.X < startJoint.X)
                {
                    this.GestureDistance *= -1;
                }
                if (Math.Abs(this.GestureDistance) > GestureGateDistance)
                {
                    p.PlayerJoints.Clear();
                    return true;
                } 
            }
            return false;
        }

        protected float MovedDistance(JointData start, JointData end)
        {
            float X = start.X - end.X;
            float Y = start.Y - end.Y;
            float Z = start.Z - end.Z;
            return (float)Math.Sqrt(X* X + Y * Y + Z * Z);
        }

       

        protected virtual void Get2Joints(List<PlayerJoints> PlayerJoints, out PlayerJoints startJoints, out PlayerJoints nowJoints)
        {
            startJoints = null;
            nowJoints = null;
            if (PlayerJoints.Count >1)
            {
                nowJoints = PlayerJoints[PlayerJoints.Count - 1];
            }
            else
            {
                return;
            }
            foreach (var item in PlayerJoints)
            {
                if (DateTime.Now.Subtract(item.TimeStamp).TotalMilliseconds < this.MaxTimeDuration &&
                    DateTime.Now.Subtract(item.TimeStamp).TotalMilliseconds > this.MinTimeDuration)
                {
                    startJoints = item;
                    break;
                }
            }
           
        }
    }

    public enum GestureOrentation
    {
        Left,
        Right
    }

}
