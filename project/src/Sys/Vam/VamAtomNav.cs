﻿using System;
using UnityEngine;
using UnityEngine.AI;

namespace Cue.W
{
	class VamAtomNav
	{
		private const int NoMove = 0;
		private const int CalculatingPath = 1;
		private const int StartingTurn = 2;
		private const int Moving = 3;
		private const int EndingTurn = 4;

		private VamAtom atom_;
		private Logger log_;
		private int state_ = NoMove;
		private Vector3 finalPosition_ = Vector3.Zero;
		private float startTurnBearing_ = BasicObject.NoBearing;
		private float endTurnBearing_ = BasicObject.NoBearing;
		private NavMeshAgent agent_ = null;
		private float turningElapsed_ = 0;
		private Quaternion turningStart_ = Quaternion.identity;
		private float pathStuckCheckElapsed_ = 0;
		private Vector3 pathStuckLastPos_ = Vector3.Zero;
		private int stuckCount_ = 0;
		private int enableCollisionsCountdown_ = -1;
		private bool navEnabled_ = false;

		public VamAtomNav(VamAtom a)
		{
			atom_ = a;

			log_ = new Logger(() =>
			{
				return "VamAtomNav " + atom_.ID;
			});
		}

		private string BearingToString(float b)
		{
			if (b == BasicObject.NoBearing)
				return "(none)";
			else
				return b.ToString("0.0");
		}

		public bool Teleporting
		{
			get { return enableCollisionsCountdown_ > 0; }
		}

		public void TeleportTo(Vector3 v, float bearing)
		{
			log_.Info($"tping to pos={v} bearing={BearingToString(bearing)}");

			atom_.Atom.collisionEnabled = false;
			atom_.Atom.mainController.MoveControl(W.VamU.ToUnity(v));

			if (bearing != BasicObject.NoBearing)
				atom_.Atom.mainController.RotateTo(Quaternion.Euler(0, bearing, 0));

			enableCollisionsCountdown_ = 100;
		}

		public bool Enabled
		{
			get
			{
				return (agent_ != null);
			}

			set
			{
				log_.Info($"setting enabled={value}");
				navEnabled_ = value;
				CheckAgent();
			}
		}

		public bool Paused
		{
			get
			{
				if (agent_ == null)
					return true;

				return agent_.isStopped;
			}

			set
			{
				log_.Info($"setting paused={value}");
				if (agent_ != null)
					agent_.isStopped = value;
			}
		}

		public void MoveTo(Vector3 v, float bearing, float stoppingDistance)
		{
			if (agent_ == null)
				return;

			log_.Info(
				$"naving to pos={v} bearing={BearingToString(bearing)} " +
				$"sd={stoppingDistance}");

			if (AlmostThere(v, bearing))
			{
				log_.Info("close enough, tping");
				atom_.Position = v;
				atom_.Direction = Vector3.Rotate(new Vector3(0, 0, 1), bearing);
			}
			else
			{
				Stop("starting move to");

				endTurnBearing_ = bearing;
				finalPosition_ = v;
				turningElapsed_ = 0;
				pathStuckCheckElapsed_ = 0;
				pathStuckLastPos_ = atom_.Position;
				stuckCount_ = 0;
				agent_.destination = W.VamU.ToUnity(finalPosition_);
				agent_.stoppingDistance = stoppingDistance;
				agent_.isStopped = true;
				state_ = CalculatingPath;
			}
		}

		public void Stop(string why)
		{
			if (agent_ == null)
				return;

			log_.Info("stopping, " + why);
			agent_.updatePosition = false;
			agent_.updateRotation = false;
			agent_.updateUpAxis = false;
			agent_.ResetPath();
		}

		public int State
		{
			get
			{
				// todo: incorrect turns

				switch (state_)
				{
					case NoMove:
					{
						return NavStates.None;
					}

					case CalculatingPath:
					{
						return NavStates.Calculating;
					}

					case Moving:
					{
						return NavStates.Moving;
					}

					case StartingTurn:
					{
						var initBearing = Vector3.Bearing(
							W.VamU.FromUnity(turningStart_.eulerAngles));

						var a = Vector3.AngleBetweenBearings(
							startTurnBearing_, initBearing);

						if (a < 0)
							return NavStates.TurningRight;
						else
							return NavStates.TurningLeft;
					}

					case EndingTurn:
					{
						var initBearing = Vector3.Bearing(
							W.VamU.FromUnity(turningStart_.eulerAngles));

						var a = Vector3.AngleBetweenBearings(
							endTurnBearing_, initBearing);

						if (a < 0)
							return NavStates.TurningRight;
						else
							return NavStates.TurningLeft;
					}

					default:
					{
						return NavStates.None;
					}
				}
			}
		}

		public void Update(float s)
		{
			CheckCollisionCountdown();

			switch (state_)
			{
				case NoMove:
				{
					break;
				}

				case CalculatingPath:
				{
					if (IsPathCalculated())
					{
						turningStart_ = atom_.Atom.mainController.transform.rotation;
						var nextPos = VamU.FromUnity(agent_.steeringTarget);

						startTurnBearing_ = Vector3.Bearing(nextPos - atom_.Position);

						log_.Info(
							$"ready to turn, pos={atom_.Position} " +
							$"next={nextPos}, " +
							$"will do starting turn from " +
							$"{VamU.Bearing(turningStart_)} " +
							$"to {BearingToString(startTurnBearing_)}") ;

						state_ = StartingTurn;
					}

					break;
				}

				case StartingTurn:
				{
					if (DoStartingTurn(s))
					{
						log_.Info("start turn finished, starting move");
						agent_.updatePosition = true;
						agent_.updateRotation = true;
						agent_.updateUpAxis = true;
						agent_.isStopped = false;
						state_ = Moving;
					}

					break;
				}

				case Moving:
				{
					if (DoMove(s))
					{
						log_.Info("nav finished, starting end turn");
						log_.Info(
							$"target was {finalPosition_}, " +
							$"pos is {atom_.Position}, " +
							$"d={Vector3.Distance(atom_.Position, finalPosition_)}");

						turningStart_ = atom_.Atom.mainController.transform.rotation;
						turningElapsed_ = 0;

						log_.Info(
							$"will do ending turn from " +
							$"{VamU.Bearing(turningStart_)} " +
							$"to {BearingToString(endTurnBearing_)}");

						state_ = EndingTurn;
					}

					break;
				}

				case EndingTurn:
				{
					if (DoEndingTurn(s))
					{
						log_.Info("end turn finished, nav done");
						state_ = NoMove;
					}

					break;
				}
			}
		}

		private bool DoStartingTurn(float s)
		{
			if (startTurnBearing_ == BasicObject.NoBearing)
			{
				log_.Info("no starting turn bearing");
				return true;
			}

			return DoTurn(s, startTurnBearing_);
		}

		private bool DoMove(float s)
		{
			CheckStuck(s);
			return !IsPathing();
		}

		private bool DoEndingTurn(float s)
		{
			if (endTurnBearing_ == BasicObject.NoBearing)
			{
				log_.Info("no ending turn bearing");
				return true;
			}

			return DoTurn(s, endTurnBearing_);
		}

		private bool DoTurn(float s, float targetBearing)
		{
			var targetDirection = Vector3.Direction(targetBearing);
			var currentBearing = atom_.Bearing;
			var d = Math.Abs(currentBearing - targetBearing);

			if (d < 5 || d >= 355)
			{
				log_.Info(
					$"DoTurn: " +
					$"b={currentBearing} " +
					$"tb={targetBearing} td={targetDirection} diff={d}, done");

				atom_.Atom.mainController.transform.rotation =
					Quaternion.LookRotation(W.VamU.ToUnity(targetDirection));

				return true;
			}
			else
			{
				turningElapsed_ += s;

				var newRot = Quaternion.LookRotation(VamU.ToUnity(targetDirection));

				atom_.Atom.mainController.transform.rotation = Quaternion.Slerp(
					turningStart_,
					newRot,
					turningElapsed_ / (360 / VamNav.AgentTurnSpeed));

				return false;
			}
		}

		private bool IsPathCalculated()
		{
			if (agent_.pathPending)
				return false;

			switch (agent_.pathStatus)
			{
				case NavMeshPathStatus.PathComplete:
				{
					log_.Info("path calculated: complete");
					break;
				}

				case NavMeshPathStatus.PathPartial:
				{
					log_.Info("path calculated: partial");
					break;
				}

				case NavMeshPathStatus.PathInvalid:
				{
					log_.Error("path calculated: invalid");
					break;
				}
			}

			log_.Info($"{agent_.path.corners.Length} corners");
			for (int i=0; i<agent_.path.corners.Length; ++i)
				log_.Info($"  - {agent_.path.corners[i]}");

			return true;
		}

		private void CheckCollisionCountdown()
		{
			if (enableCollisionsCountdown_ >= 0)
			{
				if (enableCollisionsCountdown_ == 0)
				{
					log_.Info("countdown finished, enabling collisions");
					atom_.Atom.collisionEnabled = true;
					Enabled = navEnabled_;
				}

				--enableCollisionsCountdown_;
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
						log_.Error("seems stuck, stopping nav");
						log_.Error(
							$"pos={atom_.Position} dir={atom_.Direction} " +
							$"b={atom_.Bearing} " +
							$"target={finalPosition_} d={d}");

						Stop("stuck");
					}
				}
				else
				{
					pathStuckLastPos_ = atom_.Position;
				}

				pathStuckCheckElapsed_ = 0;
			}
		}

		private void CheckAgent()
		{
			if (navEnabled_)
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

		private void CreateAgent()
		{
			try
			{
				if (enableCollisionsCountdown_ > 0)
				{
					log_.Info("not creating agent, collisions still disabled");
					return;
				}

				log_.Info("creating agent");
				MoveToNavMesh();


				agent_ = atom_.Atom.mainController.gameObject.GetComponent<NavMeshAgent>();
				if (agent_ == null)
					agent_ = atom_.Atom.mainController.gameObject.AddComponent<NavMeshAgent>();

				log_.Info($"{atom_.ID}: agent created");

				agent_.agentTypeID = VamNav.AgentTypeID;
				agent_.height = VamNav.AgentHeight;
				agent_.radius = VamNav.AgentAvoidanceRadius;
				agent_.speed = VamNav.AgentMoveSpeed;
				agent_.angularSpeed = VamNav.AgentTurnSpeed;

				agent_.stoppingDistance = 0;
				agent_.autoBraking = true;
				agent_.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
				agent_.avoidancePriority = 50;
				agent_.autoTraverseOffMeshLink = true;
				agent_.autoRepath = true;
				agent_.areaMask = ~0;

				Stop("agent created");
			}
			catch (Exception e)
			{
				log_.Error($"CreateAgent failed");
				log_.Error(e.ToString());
			}
		}

		private void MoveToNavMesh()
		{
			var currentPos = atom_.Atom.mainController.transform.position;

			NavMeshHit hit;
			if (NavMesh.SamplePosition(currentPos, out hit, 2, NavMesh.AllAreas))
			{
				log_.Info($"moved to navmesh at {hit.position}, was {currentPos}");
				atom_.Atom.mainController.transform.position = hit.position;
			}
			else
			{
				log_.Error("can't move to navmesh");
			}
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

			if (agent_.pathPending || state_ == CalculatingPath)
				return true;

			if (agent_.remainingDistance > agent_.stoppingDistance)
				return true;

			if (agent_.hasPath && agent_.velocity.sqrMagnitude > 0)
				return true;

			return false;
		}
	}
}