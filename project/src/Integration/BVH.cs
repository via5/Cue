using SimpleJSON;
using UnityEngine;

// Original script by ElkVR
// Adapted in Synthia by VAMDeluxe
namespace Cue.BVH
{
    using Vector3 = UnityEngine.Vector3;

    class Animation : IAnimation
    {
        private BVH.File file_ = null;
        private bool rootXZ_ = false;
        private bool rootY_ = false;
        private bool reverse_ = false;
        private int start_ = 0;
        private int end_ = -1;
        private bool usePosition_ = false;
        private bool localRotations_ = true;
        private bool localPositions_ = true;

        public Animation(
            string path, bool rootXZ, bool rootY, bool reverse,
            int start, int end, bool usePosition, bool localRot, bool localPos)
        {
            file_ = new File(path);
            rootXZ_ = rootXZ;
            rootY_ = rootY;
            reverse_ = reverse;
            start_ = start;
            end_ = end;
            usePosition_ = usePosition;
            localRotations_ = localRot;
            localPositions_ = localPos;

            if (end_ < 0)
                end_ = file_.FrameCount - 1;

            if (start_ > file_.FrameCount)
            {
                Cue.LogError($"bvh {file_.Name}: start too big, {start_} >= {file_.FrameCount}");
                start_ = 0;
            }

            if (end_ > file_.FrameCount)
            {
                Cue.LogError($"bvh {file_.Name}: end too big, {end_} >= {file_.FrameCount}");
                end_ = -1;
            }
        }

        public static Animation Create(JSONClass o)
        {
            var path = o["file"].Value;

            if (path.StartsWith("/") || path.StartsWith("\\"))
                path = path.Substring(1);
            else
                path = Cue.Instance.Sys.GetResourcePath("animations/" + path);

            return new BVH.Animation(
                path,
                (o.HasKey("rootXZ") ? o["rootXZ"].AsBool : true),
                (o.HasKey("rootY") ? o["rootY"].AsBool : true),
                (o.HasKey("reverse") ? o["reverse"].AsBool : false),
                (o.HasKey("start") ? o["start"].AsInt : 0),
                (o.HasKey("end") ? o["end"].AsInt : -1),
                (o.HasKey("usePosition") ? o["usePosition"].AsBool : false),
                (o.HasKey("localRotations") ? o["localRotations"].AsBool : true),
                (o.HasKey("localPositions") ? o["localPositions"].AsBool : true));
        }

        public File File
        {
            get { return file_; }
        }

        public int FirstFrame
        {
            get { return start_; }
        }

        public int LastFrame
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

        public override string ToString()
        {
            string s =
                "bvh " + file_.Name + " " +
                start_.ToString() + "-" +
                (end_ == -1 ? file_.FrameCount.ToString() : end_.ToString()) +
                (reverse_ ? " rev" : "");

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

            return s;
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
