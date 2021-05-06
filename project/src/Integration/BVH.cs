﻿using SimpleJSON;
using System;
using System.Collections.Generic;
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


    class Player : IPlayer
    {
        Atom containingAtom;

        Dictionary<string, FreeControllerV3> controllerMap;

        Dictionary<string, string> cnameToBname = new Dictionary<string, string>() {
        { "hipControl", "hip" },
        { "headControl", "head" },
        { "chestControl", "chest" },
        { "lHandControl", "lHand" },
        { "rHandControl", "rHand" },
        { "lFootControl", "lFoot" },
        { "rFootControl", "rFoot" },
        { "lKneeControl", "lShin" },
        { "rKneeControl", "rShin" },
        //{ "neckControl", "neck" },
        { "lElbowControl", "lForeArm" },
        { "rElbowControl", "rForeArm" },
        { "lArmControl", "lShldr" },
        { "rArmControl", "rShldr" },
        // Additional bones
        { "lShoulderControl", "lCollar" },
        { "rShoulderControl", "rCollar" },
        { "abdomenControl", "abdomen" },
        { "abdomen2Control", "abdomen2" },
        { "pelvisControl", "pelvis" },
        { "lThighControl", "lThigh" },
        { "rThighControl", "rThigh" },
        // { "lToeControl", "lToe" },
        // { "rToeControl", "rToe" },
    };

        Transform shadow = null;

        Animation anim = null;
        int flags = 0;
        float elapsed = 0;

        int frame = 0;
        bool playing = false;

        float frameTime;
        bool paused = false;

        // Apparently we shouldn't use enums because it causes a compiler crash
        const int translationModeOffsetPlusFrame = 0;
        const int translationModeFrameOnly = 1;
        const int translationModeInitialPlusFrameMinusOffset = 2;
        const int translationModeInitialPlusFrameMinusZero = 3;

        Vector3 rootMotion;

        float heelHeight = 0;
        float heelAngle = 0;

        private Person person_;

        public Player(Person p)
        {
            person_ = p;

            if (p.Atom is W.VamAtom)
            {
                containingAtom = ((W.VamAtom)p.Atom).Atom;
                containingAtom.ResetPhysical();

                //containingAtom.ResetRigidbodies();
                CreateShadowSkeleton();
                RecordOffsets();
                CreateControllerMap();
            }
        }

        public bool Playing
        {
            get { return playing; }
        }

        public override string ToString()
        {
            string s = "bvh ";

            if (anim == null)
            {
                s += "(none)";
            }
            else
            {
                s += $"{frame}/";

                if (anim.end == -1)
                    s += $"{anim.file.nFrames}";
                else
                    s += $"{anim.end}";
            }

            if ((flags & Animator.Reverse) != 0)
                s += " rev";

            if ((flags & Animator.Loop) != 0)
                s += " loop";

            if (playing)
                s += " playing";
            else
                s += " stopped";

            return s;
        }

        public bool Play(IAnimation a, int flags)
        {
            var ba = a as Animation;
            if (ba == null)
                return false;

            if (ba.reverse)
            {
                if ((flags & Animator.Reverse) == 0)
                    flags |= Animator.Reverse;
                else
                    flags &= ~Animator.Reverse;
            }

            anim = ba;
            this.flags = flags;
            frameTime = anim.file.frameTime;

            if (anim.end < 0)
                anim.end = anim.file.nFrames - 1;

            if ((flags & Animator.Reverse) != 0)
                frame = anim.end;
            else
                frame = anim.start;

            CreateControllerMap();

            rootMotion = new Vector3();
            playing = true;
            paused = false;

            heelAngle = person_.Clothing.HeelsAngle;
            heelHeight = person_.Clothing.HeelsHeight;

            return true;
        }

        public void Stop(bool rewind)
        {
            if (anim == null)
                return;

            if (rewind)
            {
                if ((flags & Animator.Reverse) == 0)
                    flags |= Animator.Reverse;
                else
                    flags &= ~Animator.Reverse;

                flags &= ~Animator.Loop;

                int fs = 0;
                int max = anim.file.nFrames * 2;
                while (playing)
                {
                    FixedUpdate(anim.file.frameTime);
                    ++fs;

                    if (fs >= max)
                    {
                        Cue.LogError(
                            "bvh: failed to rewind, " +
                            "fs=" + fs.ToString() + " " +
                            "n=" + anim.file.nFrames.ToString() + " " +
                            "ft=" + anim.file.frameTime.ToString() + " " +
                            "max=" + max.ToString() + " " +
                            "f=" + frame.ToString());

                        break;
                    }
                }
            }

            person_.Atom.SetDefaultControls("playing bvh");
            playing = false;
            anim = null;
        }

        void CreateControllerMap()
        {
            controllerMap = new Dictionary<string, FreeControllerV3>();
            foreach (FreeControllerV3 controller in containingAtom.freeControllers)
                controllerMap[controller.name] = controller;

            if (anim != null)
            {
                foreach (var item in cnameToBname)
                {
                    var c = controllerMap[item.Key];
                    bool found = false;

                    for (int i = 0; i < anim.file.bones.Length; ++i)
                    {
                        if (anim.file.bones[i].name == item.Value)
                        {
                            found = true;
                            break;
                        }
                    }

                    c.currentRotationState = found ? FreeControllerV3.RotationState.On : FreeControllerV3.RotationState.Off;
                    c.currentPositionState = found ? FreeControllerV3.PositionState.On : FreeControllerV3.PositionState.Off;
                }
            }
        }

        Transform CreateMarker(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            go.parent = parent;
            go.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            go.localPosition = Vector3.zero;
            go.localRotation = Quaternion.identity;
            GameObject.Destroy(go.GetComponent<BoxCollider>());
            return go;
        }

        List<Transform> markers = null;

        public void ShowSkeleton()
        {
            if (markers != null)
                HideSkeleton();
            markers = new List<Transform>();
            foreach (var bone in bones)
                markers.Add(CreateMarker(bone.Value));
        }

        public void HideSkeleton()
        {
            foreach (var marker in markers)
                GameObject.Destroy(marker.gameObject);
            markers = null;
        }

        Dictionary<string, Transform> bones;
        Dictionary<string, Vector3> tposeBoneOffsets = null;

        void RecordOffsets()
        {
            CreateShadowSkeleton();
            tposeBoneOffsets = new Dictionary<string, Vector3>();
            foreach (var item in bones)
                tposeBoneOffsets[item.Key] = item.Value.localPosition;
        }

        public void CreateShadow(Transform skeleton, Transform shadow)
        {
            bones[shadow.gameObject.name] = shadow;
            shadow.localPosition = skeleton.localPosition;
            shadow.localRotation = skeleton.localRotation;
            for (var i = 0; i < skeleton.childCount; i++)
            {
                var child = skeleton.GetChild(i);
                if (child.gameObject.GetComponent<DAZBone>() != null)
                {
                    var n = new GameObject(child.gameObject.name).transform;
                    n.parent = shadow;
                    CreateShadow(child, n);
                }
            }
        }

        void CreateShadowSkeleton()
        {
            foreach (var parent in containingAtom.gameObject.GetComponentsInChildren<DAZBones>())
            {
                if (shadow != null)
                    GameObject.Destroy(shadow.gameObject);
                bones = new Dictionary<string, Transform>();
                shadow = new GameObject("Shadow").transform;
                shadow.position = parent.transform.position;
                CreateShadow(parent.gameObject.transform, shadow);
            }
        }

        BvhTransform[] Interpolate(BvhTransform[] a, BvhTransform[] b, float t)
        {
            var ret = new BvhTransform[a.Length];
            for (var i = 0; i < a.Length; i++)
            {
                var at = a[i];
                var bt = b[i];

                var res = new BvhTransform();
                res.bone = at.bone;
                res.position = Vector3.Lerp(at.position, bt.position, t);
                res.rotation = Quaternion.Lerp(at.rotation, bt.rotation, t);

                ret[i] = res;

                if (res.bone.isHipBone)
                {
                    rootMotion = (bt.position - at.position) * t;
                }
            }
            return ret;
        }

        public void ApplyRootMotion()
        {
            float applyYaw = 0;

            int xz = anim.rootXZ ? 1 : 0;
            int y = anim.rootY ? 1 : 0;

            Vector3 rootMotion2D = new Vector3(rootMotion.x * xz, rootMotion.y * y, rootMotion.z * xz);
            rootMotion2D = Quaternion.AngleAxis(applyYaw, Vector3.up) * rootMotion2D;
            containingAtom.mainController.transform.Translate(rootMotion2D);
        }

        public bool Paused
        {
            get { return paused; }
            set { paused = value; }
        }

        public void FixedUpdate(float s)
        {
            if (containingAtom == null || paused)
                return;

            try
            {
                if (anim == null || anim.file.nFrames == 0)
                    return;

                rootMotion = new Vector3();

                FrameAdvance(s);
                UpdateControllers();
                ApplyRootMotion();
            }
            catch (Exception e)
            {
                Cue.LogError("bvh fixed update: " + e);
            }
        }

        void UpdateModel(BvhTransform[] data)
        {
            foreach (var item in data)
            {
                // Copy on to model
                if (bones.ContainsKey(item.bone.name))
                {
                    if (anim.localRotations)
                        bones[item.bone.name].localRotation = item.rotation;
                    else
                        bones[item.bone.name].rotation = item.rotation;

                    if (item.bone.hasPosition && anim.usePosition)
                    {
                        if (anim.localPositions)
                            bones[item.bone.name].localPosition = item.position;
                        else
                            bones[item.bone.name].position = item.position;
                    }
                    else
                    {
                        if (anim.localPositions)
                            bones[item.bone.name].localPosition = tposeBoneOffsets[item.bone.name];
                    }
                }
            }
        }

        private void UpdateControllers()
        {
            foreach (var item in cnameToBname)
            {
                controllerMap[item.Key].transform.localPosition = bones[item.Value].position;
                controllerMap[item.Key].transform.localRotation = bones[item.Value].rotation;
                controllerMap[item.Key].transform.localPosition += new Vector3(0, heelHeight, 0);

                if (item.Key.Contains("Foot"))
                {
                    controllerMap[item.Key].transform.localEulerAngles += new Vector3(heelAngle, 0, 0);
                }

                if (item.Key.Contains("Toe"))
                {
                    //controllerMap[item.Key].jointRotationDriveXTargetAdditional = heelAngle * 0.5f;
                    controllerMap[item.Key].transform.localEulerAngles += new Vector3(-heelAngle, 0, 0);
                }
            }
        }

        public void Update(float s)
        {
            // no-op
        }

        public void Seek(int f)
        {
            frame = f;
            elapsed = 0;
            rootMotion = new Vector3();

            UpdateModel(anim.file.ReadFrame(frame));
            UpdateControllers();
            ApplyRootMotion();
        }

        public void FrameAdvance(float s)
        {
            if (playing)
            {
                elapsed += s;
                if (elapsed >= frameTime)
                {
                    elapsed = 0;

                    if ((flags & Animator.Reverse) != 0)
                        --frame;
                    else
                        frame++;
                }
            }

            if ((flags & Animator.Reverse) != 0)
            {
                if (frame > anim.end)
                {
                    frame = anim.end;
                }

                if (frame <= anim.start)
                {
                    if ((flags & Animator.Loop) == 0)
                    {
                        person_.Atom.SetDefaultControls("bvh finished");
                        playing = false;
                        return;
                    }
                    else
                    {
                        frame = anim.end;
                    }
                }
            }
            else
            {
                if (frame < anim.start)
                {
                    frame = anim.start;
                }

                if (frame >= anim.end)
                {
                    if ((flags & Animator.Loop) == 0)
                    {
                        person_.Atom.SetDefaultControls("bvh finished");
                        playing = false;
                        return;
                    }
                    else
                    {
                        frame = anim.start;
                    }
                }
            }

            if ((flags & Animator.Reverse) != 0)
            {
                if (frame <= anim.start + 1)
                {
                    if ((flags & Animator.Loop) != 0)
                    {
                        // Interpolate
                        var frm = anim.file.ReadFrame(frame);
                        var to = anim.file.ReadFrame(anim.end);

                        float t = elapsed / frameTime;
                        UpdateModel(Interpolate(frm, to, t));
                    }
                    else
                    {
                        // Last frame
                        UpdateModel(anim.file.ReadFrame(frame));
                    }
                }
                else
                {
                    // Interpolate
                    var frm = anim.file.ReadFrame(frame);
                    var to = anim.file.ReadFrame(frame - 1);

                    float t = elapsed / frameTime;
                    UpdateModel(Interpolate(frm, to, t));
                }
            }
            else
            {
                if (frame >= anim.end - 1)
                {
                    if ((flags & Animator.Loop) != 0)
                    {
                        // Interpolate
                        var frm = anim.file.ReadFrame(frame);
                        var to = anim.file.ReadFrame(anim.start);

                        float t = elapsed / frameTime;
                        UpdateModel(Interpolate(frm, to, t));
                    }
                    else
                    {
                        // Last frame
                        UpdateModel(anim.file.ReadFrame(frame));
                    }
                }
                else
                {
                    // Interpolate
                    var frm = anim.file.ReadFrame(frame);
                    var to = anim.file.ReadFrame(frame + 1);

                    float t = elapsed / frameTime;
                    UpdateModel(Interpolate(frm, to, t));
                }
            }
        }

        public void OnDestroy()
        {
            if (shadow != null)
            {
                GameObject.Destroy(shadow.gameObject);
            }
        }
    }

    public class BvhTransform
    {
        public BvhBone bone;
        public UnityEngine.Vector3 position;
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

    public class File
    {
        public BvhBone[] bones;
        float[][] frames;
        public int nFrames;
        public float frameTime;
        public string path;
        public bool isTranslationLocal;
        string name;

        public File(string _path)
        {
            path = _path;
            Load(path);
        }

        public string Name
        {
            get { return name; }
        }

        public string Path
        {
            get { return path; }
        }

        public void Load(string path)
        {
            int i = path.Length - 1;
            while (i > 0)
            {
                if (path[i] == '/' || path[i] == '\\')
                {
                    name = path.Substring(i + 1);
                    break;
                }

                --i;
            }

            if (name == "")
                name = path;

            char[] delims = { '\r', '\n' };
            var rawText = Cue.Instance.Sys.ReadFileIntoString(path);
            if (rawText.Length == 0)
                Cue.LogError("bvh: empty file " + path);

            var raw = rawText.Split(delims, System.StringSplitOptions.RemoveEmptyEntries);

            bones = ReadHierarchy(raw);
            frames = ReadMotion(raw);
            frameTime = ReadFrameTime(raw);
            nFrames = frames.Length;
            isTranslationLocal = IsEstimatedLocalTranslation();
            ReadZeroPos();
        }

        void ReadZeroPos()
        {
            if (nFrames > 0)
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
            foreach (var bone in bones)
                if (bone.isHipBone)
                    hip = bone;
            if (hip == null)
                return true;    // best estimate without a hip bone
            var index = hip.frameOffset + 1;
            // Use hip 'y' to estimate the translation mode (local or "absolute")
            float sum = 0;
            for (var i = 0; i < nFrames; i++)
            {
                var data = frames[i];
                sum += data[index];
            }
            float average = sum / nFrames;
            float absScore = Mathf.Abs(hip.offset.y - average);    // absolute will have average close to offset
            float locScore = Mathf.Abs(average);    // lowest score wins
            return locScore < absScore;
        }

        public void LogHierarchy()
        {
            foreach (var bone in bones)
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
            if (frame >= frames.Length)
                Cue.LogError($"bad frame {frame} >= {frames.Length}");

            try
            {
                var data = frames[frame];
                var ret = new BvhTransform[bones.Length];
                for (var i = 0; i < bones.Length; i++)
                {
                    if (i >= bones.Length)
                        Cue.LogError($"bad bone {i} >= {bones.Length}");

                    var tf = new BvhTransform();
                    var bone = bones[i];
                    tf.bone = bone;
                    var offset = bone.frameOffset;

                    if ((offset+2) >= data.Length)
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
