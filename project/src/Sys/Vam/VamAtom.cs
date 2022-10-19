using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamCorruptionDetector
	{
		public delegate void Handler();
		public event Handler Corrupted;

		private const float UICheckInterval = 1;

		private Atom atom_;
		private UnityEngine.UI.Slider slider_ = null;
		private float elapsed_ = UICheckInterval;
		private float last_ = 0;

		public VamCorruptionDetector(Atom a, Handler h = null)
		{
			atom_ = a;

			if (h != null)
				Corrupted += h;
		}

		public void Update(float s)
		{
			if (slider_ == null)
			{
				elapsed_ += s;
				if (elapsed_ >= UICheckInterval)
				{
					elapsed_ = 0;

					var aui = atom_.UITransform
						?.GetComponentInChildren<AtomUI>(includeInactive: true);

					slider_ = aui?.resetPhysicsProgressSlider;
				}

				if (slider_ == null)
					return;

				last_ = slider_.value;
			}

			if (slider_.value > 0 && slider_.value != last_)
				Corrupted?.Invoke();
		}
	}


	public abstract class VamBasicAtom : IAtom
	{
		public abstract string ID { get; }
		public abstract bool Visible { get; set; }
		public abstract bool IsPerson { get; }
		public abstract bool IsMale { get; }
		public abstract bool Possessed { get; }
		public abstract bool Selected { get; }
		public abstract bool Grabbed { get; }
		public abstract bool Collisions { get; set; }
		public abstract bool Physics { get; set; }
		public abstract bool Hidden { get; set; }
		public abstract float Scale { get; set; }
		public abstract bool AutoBlink { get; set; }
		public abstract Vector3 Position { get; set; }
		public abstract Quaternion Rotation { get; set; }
		public abstract string Warning { get; }
		public abstract IBody Body { get; }
		public abstract IHair Hair { get; }
		public abstract Atom Atom { get; }


		public abstract void Destroy();
		public abstract IMorph GetMorph(string id, float eyesClosed);
		public abstract void Init();
		public abstract void LateUpdate(float s);
		public abstract void OnPluginState(bool b);
		public abstract void SetBodyDamping(int e);
		public abstract void SetCollidersForKiss(bool b, IAtom other);
		public abstract void SetPose(Pose p);
		public abstract void SetParentLink(IBodyPart bp);
		public abstract void Update(float s);
		public abstract string DebugString();

		public abstract VamBodyPart RealBodyPart(VamBodyPart bp);
	}


	public class VamAtom : VamBasicAtom
	{
		struct Damping
		{
			public FreeControllerV3 controller;
			public float normalPosition, normalRotation;
			public float sexPosition, sexRotation;

			public void Set(int e)
			{
				switch (e)
				{
					case BodyDamping.SexReceiver:
					{
						controller.RBHoldPositionDamper = sexPosition;
						controller.RBHoldRotationDamper = sexRotation;
						break;
					}

					case BodyDamping.Normal:
					default:
					{
						controller.RBHoldPositionDamper = normalPosition;
						controller.RBHoldRotationDamper = normalRotation;
						break;
					}
				}
			}
		}

		private readonly Atom atom_;
		private Logger log_;
		private ActionParameter setOnlyKeyJointsOn_;
		private FreeControllerV3 head_ = null;
		private DAZCharacter char_ = null;
		private DAZCharacterSelector selector_ = null;
		private VamBody body_ = null;
		private VamHair hair_ = null;

		private bool autoBlink_ = true;
		private List<float> eyesClosed_ = new List<float>();
		private float lastEyesClosed_ = 0;

		private BoolParameter collisions_;
		private BoolParameter physics_;
		private FloatParameter scale_;
		private BoolParameter blink_;
		private VamCorruptionDetector cd_;
		private List<Collider> allColliders_;
		private Damping[] damping_;

		public VamAtom(Atom atom)
		{
			atom_ = atom;
			log_ = new Logger(Logger.Sys, this, "vamAtom");
			setOnlyKeyJointsOn_ = new ActionParameter(
				this, "AllJointsControl", "SetOnlyKeyJointsOn");

			GetAllColliders();

			char_ = atom_.GetComponentInChildren<DAZCharacter>();
			if (char_ != null)
			{
				body_ = new VamBody(this);
				hair_ = new VamHair(this);
			}

			selector_ = atom.GetComponentInChildren<DAZCharacterSelector>();

			collisions_ = new BoolParameter(this, "AtomControl", "collisionEnabled");
			physics_ = new BoolParameter(this, "control", "physicsEnabled");
			scale_ = FindScale();
			blink_ = new BoolParameter(this, "EyelidControl", "blinkEnabled");
			cd_ = new VamCorruptionDetector(atom, OnCorruption);
		}

		public override string Warning
		{
			get { return ""; }
		}

		public bool AdvancedColliders
		{
			get { return selector_ != null && selector_.useAdvancedColliders; }
		}

		private FloatParameter FindScale()
		{
			if (IsPerson)
				return new FloatParameter(this, "rescaleObject", "scale");
			else
				return new FloatParameter(this, "scale", "scale");
		}

		public override void Init()
		{
			VamFixes.Run(atom_);
			body_.Init();
			CreateDampings();
		}

		public override void Destroy()
		{
			atom_.SetUID(atom_.uid + $"_cue_destroy");
			SuperController.singleton.RemoveAtom(atom_);
		}

		public Logger Log
		{
			get { return log_; }
		}

		public override string ID
		{
			get { return atom_.uid; }
		}

		public override bool Visible
		{
			get { return atom_.on; }
			set { atom_.SetOn(value); }
		}

		public override bool IsPerson
		{
			get { return atom_.type == "Person"; }
		}

		public override bool IsMale
		{
			get
			{
				if (char_ == null)
				{
					log_.Error($"VamAtom.Sex: atom {ID} is not a person");
					return true;
				}

				return char_.isMale;
			}
		}

		public override bool Selected
		{
			get
			{
				return (SuperController.singleton.GetSelectedAtom() == atom_);
			}
		}

		public override bool AutoBlink
		{
			get
			{
				return autoBlink_;
			}

			set
			{
				autoBlink_ = value;
				if (!value)
					lastEyesClosed_ = 0;
			}
		}

		public override IBody Body
		{
			get { return body_; }
		}

		public override IHair Hair
		{
			get { return hair_; }
		}

		public override bool Grabbed
		{
			get { return atom_.mainController.isGrabbing; }
		}

		public override Vector3 Position
		{
			get { return U.FromUnity(atom_.mainController.transform.position); }
			set { atom_.mainController.MoveControl(U.ToUnity(value)); }
		}

		public override Quaternion Rotation
		{
			get { return U.FromUnity(atom_.mainController.transform.rotation); }
			set { atom_.mainController.transform.rotation = U.ToUnity(value); }
		}

		public override bool Collisions
		{
			get { return collisions_.Value; }
			set { collisions_.Value = value; }
		}

		public override bool Physics
		{
			get { return physics_.Value; }
			set { physics_.Value = value; }
		}

		public override bool Hidden
		{
			get { return atom_.hidden; }
			set { atom_.hidden = value; }
		}

		public override float Scale
		{
			get { return scale_.Value; }
			set { scale_.Value = value; }
		}

		public override Atom Atom
		{
			get { return atom_; }
		}

		public override bool Possessed
		{
			get
			{
				GetHead();
				return head_.possessed;
			}
		}

		public override VamBodyPart RealBodyPart(VamBodyPart bp)
		{
			return bp;
		}

		public override void SetPose(Pose p)
		{
			// this breaks possession, it stays enabled but control is lost
			if (!Possessed)
			{
				if (p.type == "keyJoints")
				{
					log_.Verbose($"setting key joints");
					setOnlyKeyJointsOn_.Fire();
				}
				else
				{
					log_.Error($"{ID}: unknown pose type '{p.type}'");
				}

				foreach (var c in p.controllers)
				{
					if (c.receiver == "all")
					{
						foreach (var fc in atom_.freeControllers)
						{
							foreach (var param in c.ps)
								SetController(fc, param.name, param.value);
						}
					}
					else
					{
						var r = atom_.GetStorableByID(c.receiver);
						if (r == null)
						{
							log_.Error($"receiver '{c.receiver}' not found");
							continue;
						}

						foreach (var param in c.ps)
							SetController(r, param.name, param.value);
					}
				}
			}

			ResetLinkToRB();

			// todo
			if (Cue.Instance.Options.DevMode)
				SetNotInteractable();
		}

		private void SetController(JSONStorable r, string target, string value)
		{
			var param = r.GetParam(target);
			if (param == null)
			{
				log_.Error($"param '{target}' doesn't exist in receiver '{r.name}'");
				return;
			}

			if (SetControllerFloat(param, value))
				return;
			else if (SetControllerSC(param, value))
				return;

			log_.Error($"unsupported param type for '{target}' in receiver '{r.name}'");
		}

		private bool SetControllerFloat(JSONStorableParam param, string value)
		{
			var fp = param as JSONStorableFloat;
			if (fp == null)
				return false;

			if (value == "default")
			{
				log_.Verbose($"setting {param.storable.name}.{param.name} to default");
				fp.SetValToDefault();
			}
			else
			{
				var f = float.Parse(value);
				log_.Verbose($"setting {param.storable.name}.{param.name} to {f:0.00}");
				fp.val = f;
			}

			return true;
		}

		private bool SetControllerSC(JSONStorableParam param, string value)
		{
			var scp = param as JSONStorableStringChooser;
			if (scp == null)
				return false;

			if (value == "default")
			{
				log_.Verbose($"setting {param.storable.name}.{param.name} to default");
				scp.SetValToDefault();
			}
			else
			{
				log_.Verbose($"setting {param.storable.name}.{param.name} to {value}");
				scp.val = value;
			}

			return true;
		}

		private void ResetLinkToRB()
		{
			foreach (var fc in atom_.freeControllers)
			{
				var ps = fc.currentPositionState;
				var rs = fc.currentRotationState;

				if (ps != FreeControllerV3.PositionState.ParentLink &&
					ps != FreeControllerV3.PositionState.PhysicsLink &&
					rs != FreeControllerV3.RotationState.ParentLink &&
					rs != FreeControllerV3.RotationState.PhysicsLink)
				{
					fc.linkToRB = null;
				}
			}
		}

		private void SetNotInteractable()
		{
			foreach (var fc in atom_.freeControllers)
			{
				var rb = fc.GetComponent<Rigidbody>();
				if (rb == null)
					continue;

				if (!ShouldBeInteractable(fc, rb))
					fc.interactableInPlayMode = false;
			}
		}

		private bool ShouldBeInteractable(FreeControllerV3 fc, Rigidbody rb)
		{
			if (fc.name == "control")
				return true;

			if (fc.name.Contains("hairTool") || fc.name.Contains("hairScalp"))
				return true;

			if (fc.currentPositionState != FreeControllerV3.PositionState.Off &&
				fc.currentRotationState != FreeControllerV3.RotationState.Off)
			{
				return true;
			}

			return false;
		}

		public override void SetParentLink(IBodyPart bp)
		{
			var v = bp as VamBodyPart;
			if (v == null)
			{
				log_.Error($"SetParentLink: {bp} not a VamBodyPart");
				return;
			}

			var rb = v.Rigidbody;
			if (rb == null)
			{
				log_.Error($"SetParentLink: {bp} has no associated rigidbody");
				return;
			}

			atom_.mainController.SelectLinkToRigidbody(rb);
		}

		private void CreateDampings()
		{
			var list = new List<Damping>();

			float strongPos = 250;
			float strongRot = 30;

			float weakPos = 100;
			float weakRot = 10;

			AddDamping(list, "hipControl",    strongPos, strongRot);
			AddDamping(list, "chestControl",  strongPos, strongRot);
			AddDamping(list, "headControl",   weakPos, weakRot);
			AddDamping(list, "lThighControl", weakPos, weakRot);
			AddDamping(list, "rThighControl", weakPos, weakRot);
			AddDamping(list, "lFootControl",  weakPos, weakRot);
			AddDamping(list, "rFootControl",  weakPos, weakRot);
			AddDamping(list, "lKneeControl",  weakPos, weakRot);
			AddDamping(list, "rKneeControl",  weakPos, weakRot);

			damping_ = list.ToArray();
		}

		private void AddDamping(List<Damping> list, string controller, float sexPosition, float sexRotation)
		{
			var c = U.FindController(atom_, controller);
			if (c == null)
			{
				Log.Error($"Damping: controller {controller} not found");
				return;
			}

			float normalPos = 35;
			float normalRot = 5;

			{
				var holdPosParam = c.GetFloatJSONParam("holdPositionDamper");
				if (holdPosParam == null)
					Log.Error($"Damping: param holdPositionDamper not found for controlller {c.name}");
				else
					normalPos = holdPosParam.defaultVal;
			}

			{
				var holdRotParam = c.GetFloatJSONParam("holdRotationDamper");
				if (holdRotParam == null)
					Log.Error($"Damping: param holdRotationDamper not found for controlller {c.name}");
				else
					normalRot = holdRotParam.defaultVal;
			}

			var d = new Damping();
			d.controller = c;
			d.normalPosition = normalPos;
			d.normalRotation = normalRot;
			d.sexPosition = sexPosition;
			d.sexRotation = sexRotation;

			list.Add(d);
		}

		public override void SetBodyDamping(int e)
		{
			for (int i = 0; i < damping_.Length; ++i)
				damping_[i].Set(e);
		}

		public override void SetCollidersForKiss(bool b, IAtom other)
		{
			body_.SetCollidersForKiss(b, (other as VamAtom).body_);
			(other as VamAtom).body_.SetCollidersForKiss(b, body_);
		}


		public int AddEyesClosed()
		{
			eyesClosed_.Add(0);
			return eyesClosed_.Count - 1;
		}

		public void SetEyesClosed(int i, float f)
		{
			eyesClosed_[i] = f;
		}

		private void CheckEyesClosed()
		{
			if (AutoBlink)
			{
				float f = 0;

				for (int i = 0; i < eyesClosed_.Count; ++i)
					f += eyesClosed_[i];

				lastEyesClosed_ = f;
				blink_.Value = (f < 0.5f);
			}
		}

		void SetControllerForMoving(string id, bool b)
		{
			var fc = U.FindController(atom_, id);

			fc.currentPositionState = (b ?
				FreeControllerV3.PositionState.Off :
				FreeControllerV3.PositionState.On);

			fc.currentRotationState = (b ?
				FreeControllerV3.RotationState.Off :
				FreeControllerV3.RotationState.On);
		}

		public void SetControlsForMoving(bool b)
		{
			SetControllerForMoving("chestControl", true);
			SetControllerForMoving("hipControl", b);
			SetControllerForMoving("lFootControl", b);
			SetControllerForMoving("rFootControl", b);
		}

		public override IMorph GetMorph(string name, float eyesClosed)
		{
			return new VamMorph(this, name, eyesClosed);
		}

		public override void OnPluginState(bool b)
		{
			body_?.OnPluginState(b);
			hair_?.OnPluginState(b);
		}

		public override void Update(float s)
		{
			hair_?.Update(s);
			cd_.Update(s);
			CheckEyesClosed();
		}

		public override void LateUpdate(float s)
		{
			body_?.LateUpdate(s);
		}

		private void GetAllColliders()
		{
			if (allColliders_ != null)
				return;

			allColliders_ = new List<Collider>();

			foreach (var c in Atom.GetComponentsInChildren<Collider>())
				allColliders_.Add(c);

			foreach (var c in Atom.GetComponentsInChildren<AutoCollider>())
			{
				if (c.hardCollider != null)
					allColliders_.Add(c.hardCollider);

				if (c.jointCollider != null)
					allColliders_.Add(c.jointCollider);
			}
		}

		public Collider FindCollider(string pathstring)
		{
			GetAllColliders();
			return U.FindCollider(allColliders_, pathstring);
		}

		public override string ToString()
		{
			return ID;
		}

		public override string DebugString()
		{
			return $"eyesClosed={lastEyesClosed_:0.00}";
		}

		private void GetHead()
		{
			if (head_ != null)
				return;

			head_ = U.FindController(atom_, "headControl");
		}

		private void OnCorruption()
		{
			Log.Error(
				"cue: VaM detected corruption, disabling plugin");

			var p = Cue.Instance.FindPerson(ID);
			if (p != null)
				p.Animator.DumpAllForces();

			Cue.Instance.DisablePlugin();
		}
	}
}
