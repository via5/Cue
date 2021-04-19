using System;
using UnityEngine;
using UnityEngine.AI;

namespace Cue.W
{
	class VamAtom : IAtom
	{
		private readonly Atom atom_;
		private Rigidbody head_ = null;
		private float finalBearing_ = BasicObject.NoBearing;
		private bool turning_ = false;
		private NavMeshAgent agent_ = null;
		private float turningElapsed_ = 0;
		private Quaternion turningStart_ = Quaternion.identity;
		private const float turnSpeed_ = 360;
		private DAZCharacter char_ = null;
		private float pathStuckCheckElapsed_ = 0;
		private Vector3 pathStuckLastPos_ = Vector3.Zero;
		private int stuckCount_ = 0;
		private int enableCollisionsCountdown_ = -1;

		public VamAtom(Atom atom)
		{
			atom_ = atom;
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

				if (char_.name.Contains("female"))
					return Sexes.Female;
				else
					return Sexes.Male;
			}
		}

		public Vector3 Position
		{
			get { return Vector3.FromUnity(atom_.mainController.transform.position); }
			set { atom_.mainController.MoveControl(Vector3.ToUnity(value)); }
		}

		public Vector3 Direction
		{
			get
			{
				var v =
					atom_.mainController.transform.rotation *
					UnityEngine.Vector3.forward;

				return Vector3.FromUnity(v);
			}

			set
			{
				var r = Quaternion.LookRotation(Vector3.ToUnity(value));
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

				return Vector3.FromUnity(head_.position);
			}
		}

		public Atom Atom
		{
			get { return atom_; }
		}

		public void SetDefaultControls()
		{
			var a = ((W.VamSys)Cue.Instance.Sys).GetActionParameter(
				atom_, "AllJointsControl", "SetOnlyKeyJointsOn");

			a?.actionCallback?.Invoke();
		}

		public void OnPluginState(bool b)
		{
			foreach (var rb in atom_.rigidbodies)
			{
				var fc = rb.GetComponent<FreeControllerV3>();
				if (fc != null)
					fc.interactableInPlayMode = !b;
			}

			atom_.mainController.interactableInPlayMode = !b;
		}

		public void Update(float s)
		{
			if (enableCollisionsCountdown_ >= 0)
			{
				if (enableCollisionsCountdown_ == 0)
					atom_.collisionEnabled = true;

				--enableCollisionsCountdown_;
			}

			if (!turning_ && finalBearing_ != BasicObject.NoBearing)
			{
				if (!IsPathing())
				{
					turningStart_ = atom_.mainController.transform.rotation;
					turning_ = true;
				}
			}

			if (turning_)
			{
				var currentBearing = Vector3.Angle(Vector3.Zero, Direction);
				var direction = Vector3.Rotate(new Vector3(0, 0, 1), finalBearing_);
				var d = Math.Abs(currentBearing - finalBearing_);

				if (d < 5 || d >= 355)
				{
					atom_.mainController.transform.rotation =
						Quaternion.LookRotation(Vector3.ToUnity(direction));

					turning_ = false;
					finalBearing_ = BasicObject.NoBearing;
				}
				else
				{
					turningElapsed_ += s;

					var newDir = UnityEngine.Vector3.RotateTowards(
						atom_.mainController.transform.forward,
						Vector3.ToUnity(direction),
						50 * s, 0.0f);

					var newRot = Quaternion.LookRotation(newDir);

					atom_.mainController.transform.rotation = Quaternion.Slerp(
						turningStart_,
						newRot,
						turningElapsed_ / (360 / turnSpeed_));
					//Time.deltaTime * 2f);
				}
			}
			else if (IsPathing())
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
		}

		public void TeleportTo(Vector3 v, float bearing)
		{
			atom_.collisionEnabled = false;
			atom_.mainController.MoveControl(Vector3.ToUnity(v));

			if (bearing != BasicObject.NoBearing)
				atom_.mainController.RotateTo(Quaternion.Euler(0, bearing, 0));

			enableCollisionsCountdown_ = 5;
		}

		public bool NavEnabled
		{
			get
			{
				return (agent_ != null);
			}

			set
			{
				if (value)
				{
					if (agent_ == null)
					{
						agent_ = atom_.mainController.gameObject.AddComponent<NavMeshAgent>();
						agent_.agentTypeID = 1;
						agent_.height = 2.0f;
						agent_.radius = 0.1f;
						agent_.speed = 2;
						agent_.angularSpeed = 120;
						agent_.stoppingDistance = 0;
						agent_.autoBraking = true;
						agent_.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
						agent_.avoidancePriority = 50;
						agent_.autoTraverseOffMeshLink = true;
						agent_.autoRepath = true;
						agent_.areaMask = ~0;
						NavStop();
					}
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

			if (AlmostThere(v, bearing))
			{
				Position = v;
				Direction = Vector3.Rotate(new Vector3(0, 0, 1), bearing);
			}
			else
			{
				agent_.destination = Vector3.ToUnity(v);
				agent_.updatePosition = true;
				agent_.updateRotation = true;
				agent_.updateUpAxis = true;
				finalBearing_ = bearing;
				turningElapsed_ = 0;
				pathStuckCheckElapsed_ = 0;
				pathStuckLastPos_ = Position;
				stuckCount_ = 0;
			}
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

		public void NavStop()
		{
			if (agent_ == null)
				return;

			agent_.updatePosition = false;
			agent_.updateRotation = false;
			agent_.updateUpAxis = false;
			agent_.ResetPath();
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
			head_ = vsys.FindRigidbody(atom_, "headControl");
		}
	}
}
