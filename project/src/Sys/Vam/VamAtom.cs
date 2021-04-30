using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Cue.W
{
	class RigidbodyBodyPart : IBodyPart
	{
		private VamAtom atom_;
		private int type_;
		private Rigidbody rb_;

		public RigidbodyBodyPart(VamAtom a, int type, Rigidbody rb)
		{
			atom_ = a;
			type_ = type;
			rb_ = rb;
		}

		public int Type
		{
			get { return type_; }
		}

		public bool Triggering
		{
			get { return false; }
		}

		public Vector3 Position
		{
			get { return W.VamU.FromUnity(rb_.position); }
		}

		public Vector3 Direction
		{
			get { return W.VamU.FromUnity(rb_.rotation.eulerAngles); }
		}
	}


	class TriggerBodyPart : IBodyPart
	{
		private VamAtom atom_;
		private int type_;
		private CollisionTriggerEventHandler h_;
		private Trigger trigger_;
		private Rigidbody rb_;

		public TriggerBodyPart(VamAtom a, int type, CollisionTriggerEventHandler h)
		{
			atom_ = a;
			type_ = type;
			h_ = h;
			trigger_ = h.collisionTrigger.trigger;
			rb_ = h.thisRigidbody;
		}

		public int Type
		{
			get { return type_; }
		}

		public bool Triggering
		{
			get { return trigger_.active; }
		}

		public Vector3 Position
		{
			get
			{
				if (rb_ == null)
					return Vector3.Zero;
				else
					return W.VamU.FromUnity(rb_.position);
			}
		}

		public Vector3 Direction
		{
			get
			{
				if (rb_ == null)
					return Vector3.Zero;
				else
					return W.VamU.FromUnity(rb_.rotation.eulerAngles);
			}
		}
	}


	class VamAtom : IAtom
	{
		private readonly Atom atom_;
		private VamActionParameter setOnlyKeyJointsOn_;

		private Rigidbody head_ = null;
		private float finalBearing_ = BasicObject.NoBearing;
		private bool turning_ = false;
		private NavMeshAgent agent_ = null;
		private float turningElapsed_ = 0;
		private Quaternion turningStart_ = Quaternion.identity;
		private DAZCharacter char_ = null;
		private float pathStuckCheckElapsed_ = 0;
		private Vector3 pathStuckLastPos_ = Vector3.Zero;
		private int stuckCount_ = 0;
		private int enableCollisionsCountdown_ = -1;
		private bool calculatingPath_ = false;
		private bool navEnabled_ = false;

		public VamAtom(Atom atom)
		{
			atom_ = atom;
			setOnlyKeyJointsOn_ = new VamActionParameter(
				atom_, "AllJointsControl", "SetOnlyKeyJointsOn");
		}

		public string ID
		{
			get { return atom_.uid; }
		}

		public bool IsPerson
		{
			get { return atom_.type == "Person"; }
		}

		public int Sex
		{
			get
			{
				if (char_ == null)
					char_ = atom_.GetComponentInChildren<DAZCharacter>();

				if (char_.isMale)
					return Sexes.Male;
				else
					return Sexes.Female;
			}
		}

		public bool Teleporting
		{
			get { return enableCollisionsCountdown_ > 0; }
		}

		public Vector3 Position
		{
			get
			{
				return W.VamU.FromUnity(
					atom_.mainController.transform.position);
			}

			set
			{
				atom_.mainController.MoveControl(W.VamU.ToUnity(value));
			}
		}

		public Vector3 Direction
		{
			get
			{
				var v =
					atom_.mainController.transform.rotation *
					UnityEngine.Vector3.forward;

				return W.VamU.FromUnity(v);
			}

			set
			{
				var r = Quaternion.LookRotation(W.VamU.ToUnity(value));
				atom_.mainController.RotateTo(r);
			}
		}

		public Vector3 HeadPosition
		{
			get
			{
				GetHead();
				if (head_ == null)
					return Vector3.Zero;

				return W.VamU.FromUnity(head_.position);
			}
		}

		public Vector3 HeadDirection
		{
			get
			{
				GetHead();
				if (head_ == null)
					return Vector3.Zero;

				return W.VamU.FromUnity(head_.rotation.eulerAngles);
			}
		}

		public Atom Atom
		{
			get { return atom_; }
		}

		public List<IBodyPart> GetBodyParts()
		{
			var list = new List<IBodyPart>();

			GetRigidbody(list, BodyPartTypes.Head, "head");
			GetTrigger(list, BodyPartTypes.Lips, "LipTrigger");
			GetTrigger(list, BodyPartTypes.Mouth, "MouthTrigger");
			GetTrigger(list, BodyPartTypes.LeftBreast, "lNippleTrigger");
			GetTrigger(list, BodyPartTypes.RightBreast, "rNippleTrigger");
			GetTrigger(list, BodyPartTypes.Labia, "LabiaTrigger");
			GetTrigger(list, BodyPartTypes.Vagina, "VaginaTrigger");
			GetTrigger(list, BodyPartTypes.DeepVagina, "DeepVaginaTrigger");
			GetTrigger(list, BodyPartTypes.DeeperVagina, "DeeperVaginaTrigger");

			return list;
		}

		private void GetRigidbody(List<IBodyPart> list, int id, string name)
		{
			var rb = Cue.Instance.VamSys.FindRigidbody(atom_, name);
			if (rb == null)
				return;

			list.Add(new RigidbodyBodyPart(this, id, rb));
		}

		private void GetTrigger(List<IBodyPart> list, int id, string name)
		{
			var o = Cue.Instance.VamSys.FindChildRecursive(atom_.transform, name);
			if (o == null)
				return;

			var t = o.GetComponentInChildren<CollisionTriggerEventHandler>();
			if (t == null)
				return;

			if (t.thisRigidbody == null)
				return;

			list.Add(new TriggerBodyPart(this, id, t));
		}

		public void SetDefaultControls()
		{
			setOnlyKeyJointsOn_.Fire();
		}

		public DAZMorph FindMorph(string id)
		{
			return Cue.Instance.VamSys.FindMorph(atom_, id);
		}

		public void OnPluginState(bool b)
		{
			// this would prevent both grabbing with a possessed hand and
			// grabbing	from a distance with the pointer, there doesn't seem to
			// be a way to only disable the latter
			//
			//foreach (var rb in atom_.rigidbodies)
			//{
			//	var fc = rb.GetComponent<FreeControllerV3>();
			//	if (fc != null)
			//		fc.interactableInPlayMode = !b;
			//}
			//
			//atom_.mainController.interactableInPlayMode = !b;
		}

		public void Update(float s)
		{
			if (calculatingPath_ && !agent_.pathPending)
			{
				calculatingPath_ = false;

				switch (agent_.pathStatus)
				{
					case NavMeshPathStatus.PathComplete:
					{
						// ok
						Cue.LogInfo("nav: complete path");
						break;
					}

					case NavMeshPathStatus.PathPartial:
					{
						Cue.LogInfo("nav: partial path");
						break;
					}

					case NavMeshPathStatus.PathInvalid:
					{
						Cue.LogError("nav: no path");
						break;
					}
				}
			}

			if (enableCollisionsCountdown_ >= 0)
			{
				if (enableCollisionsCountdown_ == 0)
				{
					atom_.collisionEnabled = true;
					NavEnabled = navEnabled_;
				}

				--enableCollisionsCountdown_;
			}

			UpdateMovement(s);
		}

		private void UpdateMovement(float s)
		{
			if (!turning_ && finalBearing_ != BasicObject.NoBearing)
			{
				if (!IsPathing())
				{
					turningStart_ = atom_.mainController.transform.rotation;
					turning_ = true;
				}
			}

			if (turning_)
				DoTurn(s);
			else if (IsPathing())
				CheckStuck(s);
		}

		private void DoTurn(float s)
		{
			var currentBearing = Vector3.Angle(Vector3.Zero, Direction);
			var direction = Vector3.Rotate(new Vector3(0, 0, 1), finalBearing_);
			var d = Math.Abs(currentBearing - finalBearing_);

			if (d < 5 || d >= 355)
			{
				atom_.mainController.transform.rotation =
					Quaternion.LookRotation(W.VamU.ToUnity(direction));

				turning_ = false;
				finalBearing_ = BasicObject.NoBearing;
			}
			else
			{
				turningElapsed_ += s;

				var newDir = UnityEngine.Vector3.RotateTowards(
					atom_.mainController.transform.forward,
					W.VamU.ToUnity(direction),
					360 * s, 0.0f);

				var newRot = Quaternion.LookRotation(newDir);

				atom_.mainController.transform.rotation = Quaternion.Slerp(
					turningStart_,
					newRot,
					turningElapsed_ / (360 / VamNav.AgentTurnSpeed));
			}
		}

		private void CheckStuck(float s)
		{
			pathStuckCheckElapsed_ += s;

			if (pathStuckCheckElapsed_ >= 1)
			{
				var d = Vector3.Distance(Position, pathStuckLastPos_);

				if (d < 0.05f)
				{
					++stuckCount_;

					if (stuckCount_ >= 3)
					{
						Cue.LogError(atom_.uid + " seems stuck, stopping nav");
						NavStop();
					}
				}
				else
				{
					pathStuckLastPos_ = Position;
				}

				pathStuckCheckElapsed_ = 0;
			}
		}

		public void TeleportTo(Vector3 v, float bearing)
		{
			atom_.collisionEnabled = false;
			atom_.mainController.MoveControl(W.VamU.ToUnity(v));

			if (bearing != BasicObject.NoBearing)
				atom_.mainController.RotateTo(Quaternion.Euler(0, bearing, 0));

			enableCollisionsCountdown_ = 20;
		}

		public bool NavEnabled
		{
			get
			{
				return (agent_ != null);
			}

			set
			{
				navEnabled_ = value;

				if (value)
				{
					if (agent_ == null)
						CreateAgent();
				}
				else
				{
					if (agent_ != null)
					{
						UnityEngine.Object.Destroy(agent_);
						agent_ = null;
					}
				}
			}
		}

		public bool NavPaused
		{
			get
			{
				if (agent_ == null)
					return true;

				return agent_.isStopped;
			}

			set
			{
				if (agent_ != null)
					agent_.isStopped = value;
			}
		}

		public void NavTo(Vector3 v, float bearing)
		{
			if (agent_ == null)
				return;

			Cue.LogInfo(
				ID + ": nav to " +
				v.ToString() + " " +
				(bearing == BasicObject.NoBearing ? "nobearing" : bearing.ToString()));

			if (AlmostThere(v, bearing))
			{
				Cue.LogInfo(ID + ": close enough, teleporting");
				Position = v;
				Direction = Vector3.Rotate(new Vector3(0, 0, 1), bearing);
			}
			else
			{
				DoStartNav(v, bearing);
			}
		}

		public void NavStop()
		{
			if (agent_ == null)
				return;

			DoStopNav();
		}

		public int NavState
		{
			get
			{
				if (IsPathing())
					return NavStates.Moving;

				if (finalBearing_ != BasicObject.NoBearing)
				{
					// todo
					return NavStates.TurningLeft;
				}

				return NavStates.None;
			}
		}

		private void CreateAgent()
		{
			if (enableCollisionsCountdown_ > 0)
			{
				Cue.LogVerbose($"{atom_.uid}: not creating agent, collisions still disabled");
				return;
			}

			Cue.LogVerbose($"{atom_.uid}: creating agent");

			NavMeshHit hit;
			if (NavMesh.SamplePosition(atom_.mainController.transform.position, out hit, 2, NavMesh.AllAreas))
			{
				Cue.LogVerbose(
					$"{atom_.uid}: " +
					$"current={atom_.mainController.transform.position} " +
					$"sampled={hit.position}");

				atom_.mainController.transform.position = hit.position;
			}
			else
			{
				Cue.LogError($"{atom_.uid}: can't move to navmesh");
			}

			agent_ = atom_.mainController.gameObject.AddComponent<NavMeshAgent>();

			Cue.LogVerbose($"{atom_.uid}: agent created");

			agent_.agentTypeID = VamNav.AgentTypeID;
			agent_.height = VamNav.AgentHeight;
			agent_.radius = VamNav.AgentRadius;
			agent_.speed = VamNav.AgentMoveSpeed;
			agent_.angularSpeed = VamNav.AgentTurnSpeed;

			agent_.stoppingDistance = 0;
			agent_.autoBraking = true;
			agent_.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
			agent_.avoidancePriority = 50;
			agent_.autoTraverseOffMeshLink = true;
			agent_.autoRepath = true;
			agent_.areaMask = ~0;

			NavStop();
		}

		private void DoStartNav(Vector3 v, float bearing)
		{
			agent_.destination = W.VamU.ToUnity(v);
			agent_.updatePosition = true;
			agent_.updateRotation = true;
			agent_.updateUpAxis = true;

			finalBearing_ = bearing;
			turningElapsed_ = 0;
			pathStuckCheckElapsed_ = 0;
			pathStuckLastPos_ = Position;
			stuckCount_ = 0;
			calculatingPath_ = true;
		}

		private void DoStopNav()
		{
			agent_.updatePosition = false;
			agent_.updateRotation = false;
			agent_.updateUpAxis = false;
			agent_.ResetPath();
		}

		private bool AlmostThere(Vector3 to, float bearing)
		{
			if (Vector3.Distance(Position, to) < 0.01f)
			{
				var currentBearing = Vector3.Angle(Vector3.Zero, Direction);
				var d = Math.Abs(currentBearing - bearing);
				if (d < 5 || d >= 355)
					return true;
			}

			return false;
		}

		private bool IsPathing()
		{
			if (agent_ == null)
				return false;

			if (agent_.pathPending)
				return true;

			if (agent_.hasPath && agent_.remainingDistance > 0)
				return true;

			return false;
		}

		private void GetHead()
		{
			if (head_ != null)
				return;

			var vsys = ((W.VamSys)Cue.Instance.Sys);
			head_ = vsys.FindRigidbody(atom_, "head");
		}
	}
}
