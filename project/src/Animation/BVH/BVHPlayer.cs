﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.BVH
{
    using Vector3 = UnityEngine.Vector3;
    using Quaternion = UnityEngine.Quaternion;

    class Player : IPlayer
    {
        class Controller
        {
            public FreeControllerV3 fc;
            public Vector3 startPos;
            public Quaternion startRot;

            public Controller(FreeControllerV3 fc)
            {
                this.fc = fc;
                startPos = fc.transform.localPosition;
                startRot = fc.transform.localRotation;
            }
        }

        private Person person_;
        private Logger log_;
        private Atom containingAtom_;
        private Dictionary<string, Controller> controllerMap_;
        private Dictionary<string, string> cnameToBname_;
        private Transform shadow_ = null;
        private Animation anim_ = null;
        private int flags_ = 0;
        private float frameTimeElapsed_ = 0;
        private float elapsed_ = 0;
        private int frame_ = 0;
        private bool playing_ = false;
        private float frameTime_;
        private bool paused_ = false;
        private Vector3 rootMotion_;
        private float heelHeight_ = 0;
        private float heelAngle_ = 0;
        private List<Transform> markers_ = null;
        private Dictionary<string, Transform> bones_;
        private Dictionary<string, Vector3> tposeBoneOffsets_ = null;
        private Dictionary<string, Quaternion> tposeBoneRotations_ = null;

        public Player(Person p)
        {
            person_ = p;
            log_ = new Logger(Logger.Animation, p, "bvhPlayer");

            CreateMappings();

            if (p.Atom is Sys.Vam.VamAtom)
            {
                containingAtom_ = ((Sys.Vam.VamAtom)p.Atom).Atom;

                CreateShadowSkeleton();
                RecordOffsets();
                CreateControllerMap();
            }
        }

        private void CreateMappings()
        {
            cnameToBname_ = new Dictionary<string, string>()
            {
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

        public bool IsPlaying(IAnimation a)
        {
            return playing_;
        }

        public IAnimation[] GetPlayingDebug()
        {
            if (playing_)
                return new IAnimation[] { anim_ };
            else
                return new IAnimation[0];
        }

        public string Name
        {
            get { return "bvh"; }
        }

        public bool UsesFrames
        {
            get { return true; }
        }

        private bool Reverse
        {
            get
            {
                //return flags_ & Animator.Reverse;
                return false;
            }
        }

        public override string ToString()
        {
            string s = "bvh ";

            if (anim_ == null)
                s += "(none)";
            else
                s += $"{frame_}/{anim_.LastFrame}";

            if (Reverse)
                s += " rev";

            if ((flags_ & Animator.Loop) != 0)
                s += " loop";

            if (playing_)
                s += " playing";
            else
                s += " stopped";

            return s;
        }

        public void OnPluginState(bool b)
        {
            if (!b)
                HideSkeleton();
        }

        public bool CanPlay(IAnimation a)
        {
            return (a is Animation);
        }

        public bool Play(IAnimation a, int flags, AnimationContext cx)
        {
            var ba = a as Animation;
            if (ba == null)
                return false;

            anim_ = ba;
            anim_.File.Init();

            flags_ = flags;
            frameTime_ = anim_.File.FrameTime;
            frame_ = (int)anim_.InitFrame;
            elapsed_ = 0;

            CreateControllerMap();

            rootMotion_ = new Vector3();
            playing_ = true;
            paused_ = false;

            heelAngle_ = person_.Clothing.HeelsAngle;
            heelHeight_ = person_.Clothing.HeelsHeight;

            log_.Info($"playing {anim_}");

            return true;
        }

        public void StopNow(IAnimation a, int stopFlags = global::Cue.Animation.NoStopFlags)
        {
            if (anim_ == null)
                return;

            //person_.Atom.SetDefaultControls("playing bvh");
            playing_ = false;
            anim_ = null;
        }

        public void RequestStop(IAnimation a, int stopFlags = global::Cue.Animation.NoStopFlags)
        {
            StopNow(a, stopFlags);
        }

		public bool Pause(IAnimation a)
		{
			return false;
		}

		public bool Resume(IAnimation a)
		{
			return false;
		}

		public void MainSyncStopping(IAnimation a, Proc.ISync s)
		{
			// no-op
		}

		void CreateControllerMap()
        {
            controllerMap_ = new Dictionary<string, Controller>();
            foreach (var fc in containingAtom_.freeControllers)
                controllerMap_[fc.name] = new Controller(fc);

            if (anim_ != null)
            {
                foreach (var item in cnameToBname_)
                {
                    var c = controllerMap_[item.Key];
                    bool found = false;

                    for (int i = 0; i < anim_.File.Bones.Length; ++i)
                    {
                        if (anim_.File.Bones[i].name == item.Value)
                        {
                            found = true;
                            break;
                        }
                    }

                    bones_[item.Value].localPosition = tposeBoneOffsets_[item.Value];
                    bones_[item.Value].localRotation = tposeBoneRotations_[item.Value];

                    c.fc.currentRotationState = found ?
                        FreeControllerV3.RotationState.On :
                        FreeControllerV3.RotationState.Off;

                    c.fc.currentPositionState = found ?
                        FreeControllerV3.PositionState.On :
                        FreeControllerV3.PositionState.Off;
                }
            }
        }

        Transform CreateMarker(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            go.parent = parent;
            go.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            go.localPosition = Vector3.zero;
            go.localRotation = Quaternion.identity;

            var r = go.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Battlehub/RTGizmos/Handles"));
            r.material.color = new UnityEngine.Color(0, 0, 1, 0.5f);

            GameObject.Destroy(go.GetComponent<BoxCollider>());
            return go;
        }

        public void ShowSkeleton()
        {
            if (markers_ != null)
                HideSkeleton();
            markers_ = new List<Transform>();
            foreach (var bone in bones_)
                markers_.Add(CreateMarker(bone.Value));
        }

        public void HideSkeleton()
        {
            if (markers_ != null)
            {
                foreach (var marker in markers_)
                    GameObject.Destroy(marker.gameObject);
                markers_ = null;
            }
        }

        void RecordOffsets()
        {
            CreateShadowSkeleton();

            tposeBoneOffsets_ = new Dictionary<string, Vector3>();
            tposeBoneRotations_ = new Dictionary<string, Quaternion>();
            foreach (var item in bones_)
            {
                tposeBoneOffsets_[item.Key] = item.Value.localPosition;
                tposeBoneRotations_[item.Key] = item.Value.localRotation;
            }
        }

        public void CreateShadow(Transform skeleton, Transform shadow)
        {
            bones_[shadow.gameObject.name] = shadow;
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
                bones_ = new Dictionary<string, Transform>();
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

            int xz = anim_.RootXZ ? 1 : 0;
            int y = anim_.RootY ? 1 : 0;

            Vector3 rootMotion2D = new Vector3(rootMotion_.x * xz, rootMotion_.y * y, rootMotion_.z * xz);
            rootMotion2D = Quaternion.AngleAxis(applyYaw, Vector3.up) * rootMotion2D;
            containingAtom_.mainController.transform.Translate(rootMotion2D);
        }

        public void Seek(IAnimation a, float frame)
        {
            log_.Info($"seeking to {frame}");

            frame_ = (int)Math.Round(frame);
            frameTimeElapsed_ = 0;
            elapsed_ = 100;

            rootMotion_ = new Vector3();
            UpdateModel(anim_.File.ReadFrame(frame_));
            UpdateControllers();
            ApplyRootMotion();
        }

        public void FixedUpdate(float s)
        {
            if (containingAtom_ == null || paused_)
                return;

            try
            {
                if (anim_ == null || anim_.File.FrameCount == 0)
                    return;

                if (playing_)
                    elapsed_ += s;

                FrameAdvance(s);

                rootMotion_ = new Vector3();

                UpdateModel();
                UpdateControllers();
                ApplyRootMotion();
            }
            catch (Exception e)
            {
                log_.Error("bvh fixed update: " + e);
            }
        }

        void UpdateModel(BvhTransform[] data)
        {
            foreach (var item in data)
            {
                if (!bones_.ContainsKey(item.bone.name))
                    continue;

                var bone = bones_[item.bone.name];

                if (anim_.LocalRotations)
                    bone.localRotation = item.rotation;
                else
                    bone.rotation = item.rotation;

                if (item.bone.hasPosition && anim_.UsePositions)
                {
                    if (anim_.LocalPositions)
                        bone.localPosition = item.position;
                    else
                        bone.position = item.position;
                }
                else
                {
                    if (anim_.LocalPositions)
                        bone.localPosition = tposeBoneOffsets_[item.bone.name];
                }
            }
        }

        private void UpdateControllers()
        {
            foreach (var item in cnameToBname_)
            {
                var c = controllerMap_[item.Key];
                var t = c.fc.transform;

                if (!anim_.useHead_ && item.Key.Contains("head"))
                    continue;

                var pos = bones_[item.Value].position + new Vector3(0, heelHeight_, 0);
                var rot = bones_[item.Value].rotation;

                if (item.Key.Contains("Foot"))
                    rot = Quaternion.Euler(rot.eulerAngles + new Vector3(heelAngle_, 0, 0));

                if (item.Key.Contains("Toe"))
                    rot = Quaternion.Euler(rot.eulerAngles + new Vector3(-heelAngle_, 0, 0));

                if (elapsed_ < 1)
                {
                    pos = Vector3.Lerp(c.startPos, pos, elapsed_);
                    rot = Quaternion.Lerp(c.startRot, rot, elapsed_);
                }

                t.localPosition = pos;
                t.localRotation = rot;
            }
        }

        public void Update(float s)
        {
            // no-op
        }

        public void FrameAdvance(float s)
        {
            if (playing_)
            {
                frameTimeElapsed_ += s;
                if (frameTimeElapsed_ >= frameTime_)
                {
                    frameTimeElapsed_ = 0;

                    if (Reverse)
                        --frame_;
                    else
                        frame_++;
                }
            }

            if (CheckAtEnd())
            {
                //person_.Atom.SetDefaultControls("bvh finished");
                playing_ = false;
                return;
            }
        }

        private bool CheckAtEnd()
        {
            if (Reverse)
            {
                if (frame_ > anim_.InitFrame)
                    frame_ = (int)anim_.InitFrame;

                if (frame_ <= anim_.FirstFrame)
                {
                    if (Bits.IsSet(flags_, Animator.Loop))
                        frame_ = (int)anim_.LastFrame;
                    else
                        return true;
                }
            }
            else
            {
                if (frame_ < anim_.InitFrame)
                    frame_ = (int)anim_.InitFrame;

                if (frame_ >= anim_.LastFrame)
                {
                    if (Bits.IsSet(flags_, Animator.Loop))
                        frame_ = (int)anim_.FirstFrame;
                    else
                        return true;
                }
            }

            return false;
        }

        private void UpdateModel()
        {
            if (Reverse)
            {
                if (frame_ <= anim_.FirstFrame + 1)
                {
                    if (Bits.IsSet(flags_, Animator.Loop))
                    {
                        // Interpolate
                        var frm = anim_.File.ReadFrame(frame_);
                        var to = anim_.File.ReadFrame((int)anim_.LastFrame);

                        float t = frameTimeElapsed_ / frameTime_;
                        UpdateModel(Interpolate(frm, to, t));
                    }
                    else
                    {
                        // Last frame
                        UpdateModel(anim_.File.ReadFrame(frame_));
                    }
                }
                else
                {
                    // Interpolate
                    var frm = anim_.File.ReadFrame(frame_);
                    var to = anim_.File.ReadFrame(frame_ - 1);

                    float t = frameTimeElapsed_ / frameTime_;
                    UpdateModel(Interpolate(frm, to, t));
                }
            }
            else
            {
                if (frame_ >= anim_.LastFrame - 1)
                {
                    if (Bits.IsSet(flags_, Animator.Loop))
                    {
                        // Interpolate
                        var frm = anim_.File.ReadFrame(frame_);
                        var to = anim_.File.ReadFrame((int)anim_.FirstFrame);

                        float t = frameTimeElapsed_ / frameTime_;
                        UpdateModel(Interpolate(frm, to, t));
                    }
                    else
                    {
                        // Last frame
                        UpdateModel(anim_.File.ReadFrame(frame_));
                    }
                }
                else
                {
                    // Interpolate
                    var frm = anim_.File.ReadFrame(frame_);
                    var to = anim_.File.ReadFrame(frame_ + 1);

                    float t = frameTimeElapsed_ / frameTime_;
                    UpdateModel(Interpolate(frm, to, t));
                }
            }
        }

        public void OnDestroy()
        {
            if (shadow_ != null)
            {
                UnityEngine.Object.Destroy(shadow_.gameObject);
                shadow_ = null;
            }
        }
    }
}
