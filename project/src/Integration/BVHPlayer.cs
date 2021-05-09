using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.BVH
{
    using Vector3 = UnityEngine.Vector3;

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
}
