using System.Collections.Generic;
using System;
using UnityEngine;

namespace Cue.BVH
{
    using Vector3 = UnityEngine.Vector3;


    public class Animation : IAnimation
    {
        public BVH.File file = null;
        public bool loop = false;
        public bool rootXZ = false;
        public bool rootY = false;
        public int start = 0;
        public int end = -1;

        public Animation()
        {
        }

        public Animation(string path)
        {
            file = new File(path);
        }

        public Animation(string path, bool loop, bool rootXZ, bool rootY, int start, int end)
        {
            this.file = new File(path);
            this.loop = loop;
            this.rootXZ = rootXZ;
            this.rootY = rootY;
            this.start = start;
            this.end = end;
        }

        public override string ToString()
        {
            string s =
                file.Name + " " +
                start.ToString() + "-" +
                end.ToString();

            if (loop)
                s += " loop";

            return s;
        }
    }


    // Original script by ElkVR
    // Adapted in Synthia by VAMDeluxe
    public class Player
    {
        Atom containingAtom;

        Dictionary<string, FreeControllerV3> controllerMap;

        Dictionary<string, string> cnameToBname = new Dictionary<string, string>() {
        { "hipControl", "hip" },
        //{ "headControl", "head" },
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
        bool reverse = false;
        float elapsed = 0;

        public int frame = 0;
        public bool playing = false;

        float frameTime;

        // Apparently we shouldn't use enums because it causes a compiler crash
        const int translationModeOffsetPlusFrame = 0;
        const int translationModeFrameOnly = 1;
        const int translationModeInitialPlusFrameMinusOffset = 2;
        const int translationModeInitialPlusFrameMinusZero = 3;

        public Vector3 rootMotion;

        public float heelHeight = 0;
        public float heelAngle = 0;

        public Player(Atom atom)
        {
            containingAtom = atom;
            containingAtom.ResetPhysical();
            //containingAtom.ResetRigidbodies();
            CreateShadowSkeleton();
            RecordOffsets();
            CreateControllerMap();
        }

        public override string ToString()
        {
            string s = "BVH.Player: ";

            if (anim == null)
            {
                s += "(none)";
            }
            else
            {
                s += anim.ToString() + " " + frame.ToString();

                if (reverse)
                    s += " rev";
            }

            return s;
		}

		public void Play(Animation a, bool reverse=false)
        {
            //SuperController.LogMessage("restarting");

            anim = a;
            this.reverse = reverse;
            frameTime = anim.file.frameTime;

            if (anim.end < 0)
                anim.end = anim.file.nFrames - 1;

            if (reverse)
                frame = anim.end;
            else
                frame = anim.start;

            CreateControllerMap();

            rootMotion = new Vector3();
            playing = true;
        }

        void CreateControllerMap()
        {
            controllerMap = new Dictionary<string, FreeControllerV3>();
            foreach (FreeControllerV3 controller in containingAtom.freeControllers)
                controllerMap[controller.name] = controller;

            foreach (var item in cnameToBname)
            {
                var c = controllerMap[item.Key];
                c.currentRotationState = FreeControllerV3.RotationState.On;
                c.currentPositionState = FreeControllerV3.PositionState.On;
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
                // SuperController.LogMessage(string.Format("{0}", parent.gameObject.name));
                // SuperController.LogMessage(parent.gameObject.name);
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

        void UpdateModel(BvhTransform[] data)
        {
            foreach (var item in data)
            {
                // Copy on to model
                if (bones.ContainsKey(item.bone.name))
                {
                    bones[item.bone.name].localRotation = item.rotation;
                }
            }
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

        public void FixedUpdate(float s)
        {
            try
            {
                if (anim == null || anim.file.nFrames == 0)
                    return;

                rootMotion = new Vector3();

                FrameAdvance(s);

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

                ApplyRootMotion();
            }
            catch (Exception e)
            {
                SuperController.LogError("Fixed Update: " + e);
            }
        }

        public void FrameAdvance(float s)
        {
            if (playing)
            {
                elapsed += s;
                if (elapsed >= frameTime)
                {
                    elapsed = 0;

                    if (reverse)
                        --frame;
                    else
                        frame++;
                }
            }

            if (reverse)
            {
                if (frame > anim.end)
                {
                    frame = anim.end;
                }

                if (frame <= anim.start)
                {
                    if (anim.loop == false)
                    {
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
                    if (anim.loop == false)
                    {
                        playing = false;
                        return;
                    }
                    else
                    {
                        frame = anim.start;
                    }
                }
            }

            //SuperController.LogMessage(frame + " / " + bvh.nFrames);

            if (reverse)
            {
                if (frame <= anim.start + 1)
                {
                    // Last frame
                    UpdateModel(anim.file.ReadFrame(frame));
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
                    // Last frame
                    UpdateModel(anim.file.ReadFrame(frame));
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
