using System;
using UnityEngine;
using UnityEngine.AI;

namespace Cue.W
{
	class VamAtomNav
	{
		private VamAtom atom_;
		private float finalBearing_ = BasicObject.NoBearing;
		private bool turning_ = false;
		private NavMeshAgent agent_ = null;
		private float turningElapsed_ = 0;
		private Quaternion turningStart_ = Quaternion.identity;
		private float pathStuckCheckElapsed_ = 0;
		private Vector3 pathStuckLastPos_ = Vector3.Zero;
		private int stuckCount_ = 0;
		private int enableCollisionsCountdown_ = -1;
		private bool calculatingPath_ = false;
		private bool navEnabled_ = false;

		public VamAtomNav(VamAtom a)
		{
			atom_ = a;
		}

		public bool Teleporting
		{
			get { return enableCollisionsCountdown_ > 0; }
		}

		public void TeleportTo(Vector3 v, float bearing)
		{
			atom_.Atom.collisionEnabled = false;
			atom_.Atom.mainController.MoveControl(W.VamU.ToUnity(v));

			if (bearing != BasicObject.NoBearing)
				atom_.Atom.mainController.RotateTo(Quaternion.Euler(0, bearing, 0));

			enableCollisionsCountdown_ = 100;
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
				atom_.ID + ": nav to " +
				v.ToString() + " " +
				(bearing == BasicObject.NoBearing ? "nobearing" : bearing.ToString()));

			if (AlmostThere(v, bearing))
			{
				Cue.LogInfo(atom_.ID + ": close enough, teleporting");
				atom_.Position = v;
				atom_.Direction = Vector3.Rotate(new Vector3(0, 0, 1), bearing);
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
					atom_.Atom.collisionEnabled = true;
					NavEnabled = navEnabled_;
				}

				--enableCollisionsCountdown_;
			}

			UpdateMovement(s);
		}

		private void UpdateMovement(float s)
		{
			if (!IsPathing())
			{
				if (!turning_ && finalBearing_ != BasicObject.NoBearing)
				{
					turningStart_ = atom_.Atom.mainController.transform.rotation;
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
			var currentBearing = Vector3.Angle(Vector3.Zero, atom_.Direction);
			var direction = Vector3.Rotate(new Vector3(0, 0, 1), finalBearing_);
			var d = Math.Abs(currentBearing - finalBearing_);

			if (d < 5 || d >= 355)
			{
				atom_.Atom.mainController.transform.rotation =
					Quaternion.LookRotation(W.VamU.ToUnity(direction));

				turning_ = false;
				finalBearing_ = BasicObject.NoBearing;
			}
			else
			{
				turningElapsed_ += s;

				var newDir = UnityEngine.Vector3.RotateTowards(
					atom_.Atom.mainController.transform.forward,
					W.VamU.ToUnity(direction),
					360 * s, 0.0f);

				var newRot = Quaternion.LookRotation(newDir);

				atom_.Atom.mainController.transform.rotation = Quaternion.Slerp(
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
				var d = Vector3.Distance(atom_.Position, pathStuckLastPos_);

				if (d < 0.05f)
				{
					++stuckCount_;

					if (stuckCount_ >= 3)
					{
						Cue.LogError(atom_.ID + " seems stuck, stopping nav");
						NavStop();
					}
				}
				else
				{
					pathStuckLastPos_ = atom_.Position;
				}

				pathStuckCheckElapsed_ = 0;
			}
		}

		private void CreateAgent()
		{
			try
			{
				if (enableCollisionsCountdown_ > 0)
				{
					Cue.LogVerbose($"{atom_.ID}: not creating agent, collisions still disabled");
					return;
				}

				Cue.LogVerbose($"{atom_.ID}: creating agent");

				NavMeshHit hit;
				if (NavMesh.SamplePosition(atom_.Atom.mainController.transform.position, out hit, 2, NavMesh.AllAreas))
				{
					Cue.LogVerbose(
						$"{atom_.ID}: " +
						$"current={atom_.Atom.mainController.transform.position} " +
						$"sampled={hit.position}");

					atom_.Atom.mainController.transform.position = hit.position;
				}
				else
				{
					Cue.LogError($"{atom_.ID}: can't move to navmesh");
				}

				agent_ = atom_.Atom.mainController.gameObject.GetComponent<NavMeshAgent>();
				if (agent_ == null)
					agent_ = atom_.Atom.mainController.gameObject.AddComponent<NavMeshAgent>();

				Cue.LogVerbose($"{atom_.ID}: agent created");

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
			catch (Exception e)
			{
				Cue.LogError($"CreateAgent failed for {atom_.ID}");
				Cue.LogError(e.ToString());
			}
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
			pathStuckLastPos_ = atom_.Position;
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
			if (Vector3.Distance(atom_.Position, to) < 0.01f)
			{
				if (bearing == BasicObject.NoBearing)
					return true;

				var currentBearing = Vector3.Angle(Vector3.Zero, atom_.Direction);
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

			if (agent_.pathPending || calculatingPath_)
				return true;

			if (agent_.remainingDistance > agent_.stoppingDistance)
				return true;

			if (agent_.hasPath && agent_.velocity.sqrMagnitude > 0)
				return true;

			return false;
		}
	}
}
