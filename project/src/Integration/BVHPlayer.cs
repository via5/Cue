using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.BVH
{
    using Vector3 = UnityEngine.Vector3;

    class Player : IPlayer
    {
        private Person person_;
        private Atom containingAtom_;
        private Dictionary<string, FreeControllerV3> controllerMap_;
        private Dictionary<string, string> cnameToBname_;
        private Transform shadow_ = null;
        private Animation anim_ = null;
        private int flags_ = 0;
        private float elapsed_ = 0;
        private int frame_ = 0;
        private bool playing_ = false;
        private float frameTime_;
        private bool paused_ = false;
        private Vector3 rootMotion_;
        private float heelHeight_ = 0;
        private float heelAngle_ = 0;

        public Player(Person p)
        {
            person_ = p;
            CreateMappings();

            if (p.Atom is W.VamAtom)
            {
                containingAtom_ = ((W.VamAtom)p.Atom).Atom;
                containingAtom_.ResetPhysical();

                //containingAtom.ResetRigidbodies();
                CreateShadowSkeleton();
                RecordOffsets();
                CreateControllerMap();
            }
        }

        private void CreateMappings()
        {
            cnameToBname_ = new Dictionary<string, string>() {
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
        }

        public bool Playing
        {
            get { return playing_; }
        }

        public override string ToString()
        {
            string s = "bvh ";

            if (anim_ == null)
            {
                s += "(none)";
            }
            else
            {
                s += $"{frame_}/";

                if (anim_.end == -1)
                    s += $"{anim_.file.FrameCount}";
                else
                    s += $"{anim_.end}";
            }

            if ((flags_ & Animator.Reverse) != 0)
                s += " rev";

            if ((flags_ & Animator.Loop) != 0)
                s += " loop";

            if (playing_)
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

            anim_ = ba;
            this.flags_ = flags;
            frameTime_ = anim_.file.FrameTime;

            if (anim_.end < 0)
                anim_.end = anim_.file.FrameCount - 1;

            if ((flags & Animator.Reverse) != 0)
                frame_ = anim_.end;
            else
                frame_ = anim_.start;

            CreateControllerMap();

            rootMotion_ = new Vector3();
            playing_ = true;
            paused_ = false;

            heelAngle_ = person_.Clothing.HeelsAngle;
            heelHeight_ = person_.Clothing.HeelsHeight;

            return true;
        }

        public void Stop(bool rewind)
        {
            if (anim_ == null)
                return;

            if (rewind)
            {
                if ((flags_ & Animator.Reverse) == 0)
                    flags_ |= Animator.Reverse;
                else
                    flags_ &= ~Animator.Reverse;

                flags_ &= ~Animator.Loop;

                int fs = 0;
                int max = anim_.file.FrameCount * 2;
                while (playing_)
                {
                    FixedUpdate(anim_.file.FrameTime);
                    ++fs;

                    if (fs >= max)
                    {
                        Cue.LogError(
                            "bvh: failed to rewind, " +
                            "fs=" + fs.ToString() + " " +
                            "n=" + anim_.file.FrameCount.ToString() + " " +
                            "ft=" + anim_.file.FrameTime.ToString() + " " +
                            "max=" + max.ToString() + " " +
                            "f=" + frame_.ToString());

                        break;
                    }
                }
            }

            person_.Atom.SetDefaultControls("playing bvh");
            playing_ = false;
            anim_ = null;
        }

        void CreateControllerMap()
        {
            controllerMap_ = new Dictionary<string, FreeControllerV3>();
            foreach (FreeControllerV3 controller in containingAtom_.freeControllers)
                controllerMap_[controller.name] = controller;

            if (anim_ != null)
            {
                foreach (var item in cnameToBname_)
                {
                    var c = controllerMap_[item.Key];
                    bool found = false;

                    for (int i = 0; i < anim_.file.Bones.Length; ++i)
                    {
                        if (anim_.file.Bones[i].name == item.Value)
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
            foreach (var parent in containingAtom_.gameObject.GetComponentsInChildren<DAZBones>())
            {
                if (shadow_ != null)
                    GameObject.Destroy(shadow_.gameObject);
                bones = new Dictionary<string, Transform>();
                shadow_ = new GameObject("Shadow").transform;
                shadow_.position = parent.transform.position;
                CreateShadow(parent.gameObject.transform, shadow_);
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
                    rootMotion_ = (bt.position - at.position) * t;
                }
            }
            return ret;
        }

        public void ApplyRootMotion()
        {
            float applyYaw = 0;

            int xz = anim_.rootXZ ? 1 : 0;
            int y = anim_.rootY ? 1 : 0;

            Vector3 rootMotion2D = new Vector3(rootMotion_.x * xz, rootMotion_.y * y, rootMotion_.z * xz);
            rootMotion2D = Quaternion.AngleAxis(applyYaw, Vector3.up) * rootMotion2D;
            containingAtom_.mainController.transform.Translate(rootMotion2D);
        }

        public bool Paused
        {
            get { return paused_; }
            set { paused_ = value; }
        }

        public void FixedUpdate(float s)
        {
            if (containingAtom_ == null || paused_)
                return;

            try
            {
                if (anim_ == null || anim_.file.FrameCount == 0)
                    return;

                rootMotion_ = new Vector3();

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
                    if (anim_.localRotations)
                        bones[item.bone.name].localRotation = item.rotation;
                    else
                        bones[item.bone.name].rotation = item.rotation;

                    if (item.bone.hasPosition && anim_.usePosition)
                    {
                        if (anim_.localPositions)
                            bones[item.bone.name].localPosition = item.position;
                        else
                            bones[item.bone.name].position = item.position;
                    }
                    else
                    {
                        if (anim_.localPositions)
                            bones[item.bone.name].localPosition = tposeBoneOffsets[item.bone.name];
                    }
                }
            }
        }

        private void UpdateControllers()
        {
            foreach (var item in cnameToBname_)
            {
                controllerMap_[item.Key].transform.localPosition = bones[item.Value].position;
                controllerMap_[item.Key].transform.localRotation = bones[item.Value].rotation;
                controllerMap_[item.Key].transform.localPosition += new Vector3(0, heelHeight_, 0);

                if (item.Key.Contains("Foot"))
                {
                    controllerMap_[item.Key].transform.localEulerAngles += new Vector3(heelAngle_, 0, 0);
                }

                if (item.Key.Contains("Toe"))
                {
                    //controllerMap[item.Key].jointRotationDriveXTargetAdditional = heelAngle * 0.5f;
                    controllerMap_[item.Key].transform.localEulerAngles += new Vector3(-heelAngle_, 0, 0);
                }
            }
        }

        public void Update(float s)
        {
            // no-op
        }

        public void Seek(int f)
        {
            frame_ = f;
            elapsed_ = 0;
            rootMotion_ = new Vector3();

            UpdateModel(anim_.file.ReadFrame(frame_));
            UpdateControllers();
            ApplyRootMotion();
        }

        public void FrameAdvance(float s)
        {
            if (playing_)
            {
                elapsed_ += s;
                if (elapsed_ >= frameTime_)
                {
                    elapsed_ = 0;

                    if ((flags_ & Animator.Reverse) != 0)
                        --frame_;
                    else
                        frame_++;
                }
            }

            if ((flags_ & Animator.Reverse) != 0)
            {
                if (frame_ > anim_.end)
                {
                    frame_ = anim_.end;
                }

                if (frame_ <= anim_.start)
                {
                    if ((flags_ & Animator.Loop) == 0)
                    {
                        person_.Atom.SetDefaultControls("bvh finished");
                        playing_ = false;
                        return;
                    }
                    else
                    {
                        frame_ = anim_.end;
                    }
                }
            }
            else
            {
                if (frame_ < anim_.start)
                {
                    frame_ = anim_.start;
                }

                if (frame_ >= anim_.end)
                {
                    if ((flags_ & Animator.Loop) == 0)
                    {
                        person_.Atom.SetDefaultControls("bvh finished");
                        playing_ = false;
                        return;
                    }
                    else
                    {
                        frame_ = anim_.start;
                    }
                }
            }

            if ((flags_ & Animator.Reverse) != 0)
            {
                if (frame_ <= anim_.start + 1)
                {
                    if ((flags_ & Animator.Loop) != 0)
                    {
                        // Interpolate
                        var frm = anim_.file.ReadFrame(frame_);
                        var to = anim_.file.ReadFrame(anim_.end);

                        float t = elapsed_ / frameTime_;
                        UpdateModel(Interpolate(frm, to, t));
                    }
                    else
                    {
                        // Last frame
                        UpdateModel(anim_.file.ReadFrame(frame_));
                    }
                }
                else
                {
                    // Interpolate
                    var frm = anim_.file.ReadFrame(frame_);
                    var to = anim_.file.ReadFrame(frame_ - 1);

                    float t = elapsed_ / frameTime_;
                    UpdateModel(Interpolate(frm, to, t));
                }
            }
            else
            {
                if (frame_ >= anim_.end - 1)
                {
                    if ((flags_ & Animator.Loop) != 0)
                    {
                        // Interpolate
                        var frm = anim_.file.ReadFrame(frame_);
                        var to = anim_.file.ReadFrame(anim_.start);

                        float t = elapsed_ / frameTime_;
                        UpdateModel(Interpolate(frm, to, t));
                    }
                    else
                    {
                        // Last frame
                        UpdateModel(anim_.file.ReadFrame(frame_));
                    }
                }
                else
                {
                    // Interpolate
                    var frm = anim_.file.ReadFrame(frame_);
                    var to = anim_.file.ReadFrame(frame_ + 1);

                    float t = elapsed_ / frameTime_;
                    UpdateModel(Interpolate(frm, to, t));
                }
            }
        }

        public void OnDestroy()
        {
            if (shadow_ != null)
            {
                GameObject.Destroy(shadow_.gameObject);
            }
        }
    }
}
