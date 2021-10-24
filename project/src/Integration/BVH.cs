using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

// Original script by ElkVR
// Adapted in Synthia by VAMDeluxe
namespace Cue.BVH
{
    using Vector3 = UnityEngine.Vector3;
    using Quaternion = UnityEngine.Quaternion;

    class Animation : IAnimation
    {
        private BVH.File file_ = null;
        private bool rootXZ_ = false;
        private bool rootY_ = false;
        private bool reverse_ = false;
        private int init_ = -1;
        private int start_ = 0;
        private int end_ = -1;
        private bool usePosition_ = false;
        private bool localRotations_ = true;
        private bool localPositions_ = true;
        public bool useHead_ = true;

        public Animation(
            string path, bool rootXZ, bool rootY, bool reverse,
            int init, int start, int end,
            bool usePosition, bool localRot, bool localPos, bool useHead)
        {
            file_ = new File(path);
            rootXZ_ = rootXZ;
            rootY_ = rootY;
            reverse_ = reverse;
            init_ = init;
            start_ = start;
            end_ = end;
            usePosition_ = usePosition;
            localRotations_ = localRot;
            localPositions_ = localPos;
            useHead_ = useHead;

            if (end_ < 0)
                end_ = file_.FrameCount - 1;

            if (start_ > file_.FrameCount)
            {
                Cue.LogError(
                    $"bvh {file_.Name}: start too big, " +
                    "{start_} >= {file_.FrameCount}");

                start_ = 0;
            }

            if (end_ > file_.FrameCount)
            {
                Cue.LogError(
                    $"bvh {file_.Name}: end too big, " +
                    "{end_} >= {file_.FrameCount}");

                end_ = -1;
            }

            if (start_ > end_)
            {
                Cue.LogError(
                    $"bvh {file_.Name}: start {start_} " +
                    "after end {end_}, swapping");

                int temp = start_;
                start_ = end_;
                end_ = temp;
            }

            if (init_ == -1)
            {
                if (reverse_)
                    init_ = end_;
                else
                    init_ = start_;
            }

            if (reverse_ && init_ < end_)
            {
                Cue.LogError(
                    $"bvh {file_.Name}: anim is reverse but " +
                    $"init {init_} is before {end_}, will be ignored");

                init_ = end_;
            }
        }

        public static Animation Create(JSONClass o)
        {
            var path = o["file"].Value;

            if (path.StartsWith("/") || path.StartsWith("\\"))
                path = path.Substring(1);
            else
                path = Cue.Instance.Sys.GetResourcePath(path);

            return new Animation(
                path,
                (o.HasKey("rootXZ") ? o["rootXZ"].AsBool : true),
                (o.HasKey("rootY") ? o["rootY"].AsBool : true),
                (o.HasKey("reverse") ? o["reverse"].AsBool : false),
                (o.HasKey("init") ? o["init"].AsInt : -1),
                (o.HasKey("start") ? o["start"].AsInt : 0),
                (o.HasKey("end") ? o["end"].AsInt : -1),
                (o.HasKey("usePosition") ? o["usePosition"].AsBool : false),
                (o.HasKey("localRotations") ? o["localRotations"].AsBool : true),
                (o.HasKey("localPositions") ? o["localPositions"].AsBool : true),
                (o.HasKey("useHead") ? o["useHead"].AsBool : true));
        }

        public string Name
        {
            get { return file_.Name; }
        }

        public File File
        {
            get { return file_; }
        }

        public float InitFrame
        {
            get { return init_; }
        }

        public float FirstFrame
        {
            get { return start_; }
        }

        public float LastFrame
        {
            get { return end_; }
        }

        public bool Reverse
        {
            get { return reverse_; }
        }

        public bool RootXZ
        {
            get { return rootXZ_; }
        }

        public bool RootY
        {
            get { return rootY_; }
        }

        public bool HasMovement
        {
            get { return true; }
        }

        public bool LocalRotations
        {
            get { return localRotations_; }
        }

        public bool LocalPositions
        {
            get { return localPositions_; }
        }

        public bool UsePositions
        {
            get { return usePosition_; }
        }

        public bool UseHead
        {
            get { return useHead_; }
        }

        public string[] GetAllForcesDebug()
        {
            return null;
        }

        public string[] Debug()
        {
            return null;
        }

        public override string ToString()
        {
            string s = $"bvh {file_.Name} {init_}/{start_}/{end_}";

            if (reverse_)
                s += " rev";

            if (rootXZ_ && rootY_)
                s += " rootAll";
            else if (rootXZ_)
                s += " rootXZ";
            else if (rootY_)
                s += " rootY";

            if (localPositions_)
                s += " locPos";
            else
                s += " absPos";

            if (localRotations_)
                s += " locRot";
            else
                s += " absRot";

            if (!useHead_)
                s += " nohead";

            return s;
        }

        public string ToDetailedString()
        {
            return ToString();
        }
    }

    public class BvhTransform
    {
        public BvhBone bone;
        public Vector3 position;
        public Quaternion rotation;
    }

    // enums are not allowed in scripts (they crash VaM)
    public class RotationOrder
    {
        public const int XYZ = 0, XZY = 1;
        public const int YXZ = 2, YZX = 3;
        public const int ZXY = 4, ZYX = 5;
    }

    public class BvhBone
    {
        public string name;
        public BvhBone parent;
        public bool hasPosition, hasRotation;
        public int frameOffset;
        public Vector3 offset, posZero = Vector3.zero;
        public bool isHipBone = false;
        public int rotationOrder = RotationOrder.ZXY;

        public string ToDebugString()
        {
            return string.Format("{0} {1} {2} fo:{3} par:{4}", name, hasPosition ? "position" : "", hasRotation ? "rotation" : "", frameOffset, parent != null ? parent.name : "(null)");
        }
    }
}
