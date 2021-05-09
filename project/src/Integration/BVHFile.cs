using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.BVH
{
    using Vector3 = UnityEngine.Vector3;

    public class File
    {
        private BvhBone[] bones_;
        private float[][] frames_;
        private int nFrames_;
        private float frameTime_;
        private string path_;
        private bool isTranslationLocal_;
        private string name_;

        public File(string _path)
        {
            path_ = _path;
            Load(path_);
        }

        public string Name
        {
            get { return name_; }
        }

        public string Path
        {
            get { return path_; }
        }

        public int FrameCount
        {
            get { return nFrames_; }
        }

        public float FrameTime
        {
            get { return frameTime_; }
        }

        public BvhBone[] Bones
        {
            get { return bones_; }
        }

        public void Load(string path)
        {
            int i = path.Length - 1;
            while (i > 0)
            {
                if (path[i] == '/' || path[i] == '\\')
                {
                    name_ = path.Substring(i + 1);
                    break;
                }

                --i;
            }

            if (name_ == "")
                name_ = path;

            char[] delims = { '\r', '\n' };
            var rawText = Cue.Instance.Sys.ReadFileIntoString(path);
            if (rawText.Length == 0)
                Cue.LogError("bvh: empty file " + path);

            var raw = rawText.Split(delims, System.StringSplitOptions.RemoveEmptyEntries);

            bones_ = ReadHierarchy(raw);
            frames_ = ReadMotion(raw);
            frameTime_ = ReadFrameTime(raw);
            nFrames_ = frames_.Length;
            isTranslationLocal_ = IsEstimatedLocalTranslation();
            ReadZeroPos();
        }

        void ReadZeroPos()
        {
            if (nFrames_ > 0)
            {
                foreach (var tf in ReadFrame(0))
                {
                    if (tf.bone.hasPosition)
                        tf.bone.posZero = tf.position;
                }
            }
        }

        bool IsEstimatedLocalTranslation()
        {
            BvhBone hip = null;
            foreach (var bone in bones_)
                if (bone.isHipBone)
                    hip = bone;
            if (hip == null)
                return true;    // best estimate without a hip bone
            var index = hip.frameOffset + 1;
            // Use hip 'y' to estimate the translation mode (local or "absolute")
            float sum = 0;
            for (var i = 0; i < nFrames_; i++)
            {
                var data = frames_[i];
                sum += data[index];
            }
            float average = sum / nFrames_;
            float absScore = Mathf.Abs(hip.offset.y - average);    // absolute will have average close to offset
            float locScore = Mathf.Abs(average);    // lowest score wins
            return locScore < absScore;
        }

        public void LogHierarchy()
        {
            foreach (var bone in bones_)
            {
                Debug.Log(bone.ToDebugString());
            }
        }

        float ReadFrameTime(string[] lines)
        {
            foreach (var line in lines)
            {
                if (line.StartsWith("Frame Time:"))
                {
                    var parts = line.Split(':');
                    return float.Parse(parts[1]);
                }
            }
            return (1f / 30);   // default to 30 FPS
        }

        int GetRotationOrder(string c1, string c2, string c3)
        {
            c1 = c1.ToLower().Substring(0, 1); c2 = c2.ToLower().Substring(0, 1); c3 = c3.ToLower().Substring(0, 1);
            if (c1 == "x" && c2 == "y" && c3 == "z") return RotationOrder.XYZ;
            if (c1 == "x" && c2 == "z" && c3 == "y") return RotationOrder.XZY;
            if (c1 == "y" && c2 == "x" && c3 == "z") return RotationOrder.YXZ;
            if (c1 == "y" && c2 == "z" && c3 == "x") return RotationOrder.YZX;
            if (c1 == "z" && c2 == "x" && c3 == "y") return RotationOrder.ZXY;
            if (c1 == "z" && c2 == "y" && c3 == "x") return RotationOrder.ZYX;
            return RotationOrder.ZXY;
        }

        BvhBone[] ReadHierarchy(string[] lines)
        {
            char[] delims = { ' ', '\t' };
            var boneList = new List<BvhBone>();
            BvhBone current = null;
            int frameOffset = 0;
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "MOTION")
                    break;
                var parts = lines[i].Split(delims, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && (parts[0] == "JOINT" || parts[0] == "ROOT"))
                {
                    current = new BvhBone();
                    current.name = parts[1];
                    current.offset = Vector3.zero;
                    current.frameOffset = frameOffset;
                    if (current.name == "hip")
                        current.isHipBone = true;
                    boneList.Add(current);
                }
                if (parts.Length >= 4 && parts[0] == "OFFSET" && current != null)
                    current.offset = new Vector3(-float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])) * 0.01f;
                if (parts.Length >= 2 && parts[0] == "CHANNELS" && current != null)
                {
                    var nChannels = int.Parse(parts[1]);
                    frameOffset += nChannels;
                    // XXX: examples may exist that are not covered here (but I think they're rare) -- Found some!
                    // We now support 6 channels with X,Y,Zpos in first 3 and any rotation order
                    // Or 3 channels with any rotation order
                    if (nChannels == 3)
                    {
                        current.hasPosition = false;
                        current.hasRotation = true;
                        current.rotationOrder = GetRotationOrder(parts[2], parts[3], parts[4]);
                    }
                    else if (nChannels == 6)
                    {
                        current.hasPosition = true;
                        current.hasRotation = true;
                        current.rotationOrder = GetRotationOrder(parts[5], parts[6], parts[7]);
                    }
                    else
                    {
                        Cue.LogError(string.Format("Unexpect number of channels in BVH Hierarchy {1} {0}", nChannels, current.name));
                    }
                }
                if (parts.Length >= 2 && parts[0] == "End" && parts[1] == "Site")
                    current = null;
            }
            return boneList.ToArray();
        }

        float[][] ReadMotion(string[] lines)
        {
            char[] delims = { ' ', '\t' };
            var output = new List<float[]>();
            var i = 0;
            for (; i < lines.Length; i++)
            {
                if (lines[i] == "MOTION")
                    break;
            }
            i++;
            for (; i < lines.Length; i++)
            {
                var raw = lines[i].Split(delims, System.StringSplitOptions.RemoveEmptyEntries);
                if (raw[0].StartsWith("F")) // Frame Time: and Frames:
                    continue;
                var frame = new float[raw.Length];
                for (var j = 0; j < raw.Length; j++)
                    frame[j] = float.Parse(raw[j]);
                output.Add(frame);
            }
            return output.ToArray();
        }

        public BvhTransform[] ReadFrame(int frame)
        {
            if (frame >= frames_.Length)
                Cue.LogError($"bad frame {frame} >= {frames_.Length}");

            try
            {
                var data = frames_[frame];
                var ret = new BvhTransform[bones_.Length];
                for (var i = 0; i < bones_.Length; i++)
                {
                    if (i >= bones_.Length)
                        Cue.LogError($"bad bone {i} >= {bones_.Length}");

                    var tf = new BvhTransform();
                    var bone = bones_[i];
                    tf.bone = bone;
                    var offset = bone.frameOffset;

                    if ((offset + 2) >= data.Length)
                        Cue.LogError($"bad offset {offset}+2 >= {data.Length}");

                    if (bone.hasPosition)
                    {
                        // Use -'ve X to convert RH->LH
                        tf.position = new Vector3(-data[offset], data[offset + 1], data[offset + 2]) * 0.01f;
                        offset += 3;
                    }
                    float v1 = data[offset], v2 = data[offset + 1], v3 = data[offset + 2];

                    Quaternion qx, qy, qz;
                    switch (bone.rotationOrder)
                    {
                        case RotationOrder.XYZ:
                            qx = Quaternion.AngleAxis(-v1, Vector3.left);
                            qy = Quaternion.AngleAxis(-v2, Vector3.up);
                            qz = Quaternion.AngleAxis(-v3, Vector3.forward);
                            tf.rotation = qx * qy * qz;
                            break;
                        case RotationOrder.XZY:
                            qx = Quaternion.AngleAxis(-v1, Vector3.left);
                            qy = Quaternion.AngleAxis(-v3, Vector3.up);
                            qz = Quaternion.AngleAxis(-v2, Vector3.forward);
                            tf.rotation = qx * qz * qy;
                            break;
                        case RotationOrder.YXZ:
                            qx = Quaternion.AngleAxis(-v2, Vector3.left);
                            qy = Quaternion.AngleAxis(-v1, Vector3.up);
                            qz = Quaternion.AngleAxis(-v3, Vector3.forward);
                            tf.rotation = qy * qx * qz;
                            break;
                        case RotationOrder.YZX:
                            qx = Quaternion.AngleAxis(-v3, Vector3.left);
                            qy = Quaternion.AngleAxis(-v1, Vector3.up);
                            qz = Quaternion.AngleAxis(-v2, Vector3.forward);
                            tf.rotation = qy * qz * qx;
                            break;
                        case RotationOrder.ZXY:
                            qx = Quaternion.AngleAxis(-v2, Vector3.left);
                            qy = Quaternion.AngleAxis(-v3, Vector3.up);
                            qz = Quaternion.AngleAxis(-v1, Vector3.forward);
                            tf.rotation = qz * qx * qy;
                            break;
                        case RotationOrder.ZYX:
                            qx = Quaternion.AngleAxis(-v3, Vector3.left);
                            qy = Quaternion.AngleAxis(-v2, Vector3.up);
                            qz = Quaternion.AngleAxis(-v1, Vector3.forward);
                            tf.rotation = qz * qy * qx;
                            break;
                    }

                    ret[i] = tf;
                }
                return ret;
            }
            catch (Exception e)
            {
                Cue.LogError($"ReadFrame: frame={frame}, {e}");
                return new BvhTransform[0];
            }
        }
    }
}
