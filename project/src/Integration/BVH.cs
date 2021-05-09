using SimpleJSON;
using UnityEngine;

// Original script by ElkVR
// Adapted in Synthia by VAMDeluxe
namespace Cue.BVH
{
    using Vector3 = UnityEngine.Vector3;

    class Animation : IAnimation
    {
        public BVH.File file = null;
        public bool rootXZ = false;
        public bool rootY = false;
        public bool reverse = false;
        public int start = 0;
        public int end = -1;
        public bool usePosition = false;
        public bool localRotations = true;
        public bool localPositions = true;


        public Animation(
            string path, bool rootXZ, bool rootY, bool reverse,
            int start, int end, bool usePosition, bool localRot, bool localPos)
        {
            this.file = new File(path);
            this.rootXZ = rootXZ;
            this.rootY = rootY;
            this.reverse = reverse;
            this.start = start;
            this.end = end;
            this.usePosition = usePosition;
            this.localRotations = localRot;
            this.localPositions = localPos;

            if (this.start > file.nFrames)
            {
                Cue.LogError($"bvh {file.Name}: start too big, {this.start} >= {file.nFrames}");
                this.start = 0;
            }

            if (this.end > file.nFrames)
            {
                Cue.LogError($"bvh {file.Name}: end too big, {this.end} >= {file.nFrames}");
                this.end = -1;
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

        public override string ToString()
        {
            string s =
                "bvh " + file.Name + " " +
                start.ToString() + "-" +
                (end == -1 ? file.nFrames.ToString() : end.ToString()) +
                (reverse ? " rev" : "");

            if (rootXZ && rootY)
                s += " rootAll";
            else if (rootXZ)
                s += " rootXZ";
            else if (rootY)
                s += " rootY";

            if (localPositions)
                s += " locPos";
            else
                s += " absPos";

            if (localRotations)
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
