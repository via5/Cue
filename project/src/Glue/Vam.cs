using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Cue.W
{
	class U
	{
		public static UnityEngine.Vector3 Convert(Vector3 v)
		{
			return new UnityEngine.Vector3(v.X, v.Y, v.Z);
		}

		public static Vector3 Convert(UnityEngine.Vector3 v)
		{
			return new Vector3(v.x, v.y, v.z);
		}
	}

	class NavMeshRenderer : MonoBehaviour
	{
		private Material mat_ = null;
		private NavMeshTriangulation tris_;

		public void Update()
		{
			tris_ = UnityEngine.AI.NavMesh.CalculateTriangulation();
		}

		public void OnPostRender()
		{
			if (mat_ == null)
			{
				mat_ = new Material(Shader.Find("Battlehub/RTGizmos/Handles"));
				mat_.color = new Color(1, 1, 1, 0.1f);
				mat_.SetFloat("_Offset", 1f);
				mat_.SetFloat("_MinAlpha", 1f);
			}

			var walkableColor = new Color(0, 1, 0, 1);
			var nonWalkableColor = new Color(1, 0, 0, 1);
			var unknownColor = new Color(0, 0, 1, 1);

			GL.PushMatrix();

			mat_.SetPass(0);
			//GL.wireframe = true;
			GL.Begin(GL.TRIANGLES);
			for (int i = 0; i < tris_.indices.Length; i += 3)
			{
				var triangleIndex = i / 3;
				var i1 = tris_.indices[i];
				var i2 = tris_.indices[i + 1];
				var i3 = tris_.indices[i + 2];
				var p1 = tris_.vertices[i1];
				var p2 = tris_.vertices[i2];
				var p3 = tris_.vertices[i3];
				var areaIndex = tris_.areas[triangleIndex];
				Color color;
				switch (areaIndex)
				{
					case 0:
						color = walkableColor; break;
					case 1:
						color = nonWalkableColor; break;
					default:
						color = unknownColor; break;
				}
				GL.Color(color);
				GL.Vertex(p1);
				GL.Vertex(p2);
				GL.Vertex(p3);
			}
			GL.End();
		//	GL.wireframe = false;

			GL.PopMatrix();
		}
	}

	class VamSys : ISys
	{
		private static VamSys instance_ = null;
		private readonly MVRScript script_ = null;
		private readonly VamTime time_ = new VamTime();
		private readonly VamLog log_ = new VamLog();
		private readonly VamNav nav_ = new VamNav();
		private string pluginPath_ = "";

		public VamSys(MVRScript s)
		{
			instance_ = this;
			script_ = s;
		}

		static public VamSys Instance
		{
			get { return instance_; }
		}

		public ITime Time
		{
			get { return time_; }
		}

		public ILog Log
		{
			get { return log_; }
		}

		public INav Nav
		{
			get { return nav_; }
		}

		public IAtom GetAtom(string id)
		{
			var a = SuperController.singleton.GetAtomByUid(id);
			if (a == null)
				return null;

			return new VamAtom(a);
		}

		public List<IAtom> GetAtoms(bool alsoOff=false)
		{
			var list = new List<IAtom>();

			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (a.on || alsoOff)
					list.Add(new VamAtom(a));
			}

			return list;
		}

		public IAtom ContainingAtom
		{
			get { return new VamAtom(script_.containingAtom); }
		}

		public bool Paused
		{
			get { return SuperController.singleton.freezeAnimation; }
		}

		public void OnPluginState(bool b)
		{
			nav_.OnPluginState(b);
		}

		public void OnReady(Action f)
		{
			SuperController.singleton.StartCoroutine(DeferredInit(f));
		}

		public string ReadFileIntoString(string path)
		{
			try
			{
				return SuperController.singleton.ReadFileIntoString(path);
			}
			catch (Exception e)
			{
				Cue.LogError("failed to read '" + path + "', " + e.Message);
				return "";
			}
		}

		public string GetResourcePath(string path)
		{
			// based on MacGruber, which was based on VAMDeluxe, which was
			// in turn based on Alazi

			if (pluginPath_ == "")
			{
				var self = Cue.Instance;
				string id = self.name.Substring(0, self.name.IndexOf('_'));
				string filename = self.manager.GetJSON()["plugins"][id].Value;

				pluginPath_ = filename.Substring(
					0, filename.LastIndexOfAny(new char[] { '/', '\\' }));

				pluginPath_ = pluginPath_.Replace('/', '\\');
				if (pluginPath_.EndsWith("\\"))
					pluginPath_ = pluginPath_.Substring(0, pluginPath_.Length - 1);
			}

			path = path.Replace('/', '\\');

			if (path.StartsWith("\\"))
				return pluginPath_ + "\\res" + path;
			else
				return pluginPath_ + "\\res\\" + path;
		}

		private IEnumerator DeferredInit(Action f)
		{
			yield return new WaitForEndOfFrame();
			f?.Invoke();
		}

		public JSONStorableFloat GetFloatParameter(
			IObject o, string storable, string param)
		{
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetFloatJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public JSONStorableBool GetBoolParameter(
			IObject o, string storable, string param)
		{
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetBoolJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public JSONStorableString GetStringParameter(
			IObject o, string storable, string param)
		{
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetStringJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public JSONStorableStringChooser GetStringChooserParameter(
			IObject o, string storable, string param)
		{
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetStringChooserJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public JSONStorableAction GetActionParameter(
			IObject o, string storable, string param)
		{
			return GetActionParameter(((W.VamAtom)o.Atom).Atom, storable, param);
		}

		public JSONStorableAction GetActionParameter(
			Atom a, string storable, string param)
		{
			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetAction(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' action param");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public Rigidbody FindRigidbody(IObject o, string name)
		{
			return FindRigidbody(((W.VamAtom)o.Atom).Atom, name);
		}

		public Rigidbody FindRigidbody(Atom a, string name)
		{
			foreach (var rb in a.rigidbodies)
			{
				if (rb.name == name)
					return rb.GetComponent<Rigidbody>();
			}

			return null;
		}
	}

	class VamTime : ITime
	{
		public float deltaTime
		{
			get { return Time.deltaTime; }
		}
	}

	class VamLog : ILog
	{
		public void Verbose(string s)
		{
			SuperController.LogError(s);
		}

		public void Info(string s)
		{
			SuperController.LogError(s);
		}

		public void Error(string s)
		{
			SuperController.LogError(s);
		}
	}

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
			get { return U.Convert(atom_.mainController.transform.position); }
			set { atom_.mainController.MoveControl(U.Convert(value)); }
		}

		public Vector3 Direction
		{
			get
			{
				var v =
					atom_.mainController.transform.rotation *
					UnityEngine.Vector3.forward;

				return U.Convert(v);
			}

			set
			{
				var r = Quaternion.LookRotation(U.Convert(value));
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

		public void NavStop()
		{
			if (agent_ == null)
				return;

			agent_.updatePosition = false;
			agent_.updateRotation = false;
			agent_.updateUpAxis = false;
			agent_.ResetPath();
		}

		public bool NavActive
		{
			get
			{
				if (IsPathing())
					return true;

				if (finalBearing_ != BasicObject.NoBearing)
					return true;

				return false;
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

	class VamNav : INav
	{
		private NavMeshRenderer nmr_ = null;
		private bool render_ = false;
		private readonly List<NavMeshBuildSource> sources_ = new List<NavMeshBuildSource>();

		//public void AddBox(float x, float z, float w, float h)
		//{
		//	var src = new NavMeshBuildSource();
		//	src.transform = Matrix4x4.Translate(new UnityEngine.Vector3(x, 0, z));
		//	src.shape = NavMeshBuildSourceShape.Box;
		//	src.size = new UnityEngine.Vector3(w, 0, h);
		//
		//	sources_.Add(src);
		//	Rebuild();
		//}
		//
		//public void AddBox(Vector3 center, Vector3 size)
		//{
		//	var src = new NavMeshBuildSource();
		//	src.transform = Matrix4x4.Translate(Vector3.ToUnity(center));
		//	src.shape = NavMeshBuildSourceShape.Box;
		//	src.size = Vector3.ToUnity(size);
		//
		//	sources_.Add(src);
		//	Rebuild();
		//}

		public void Update()
		{
			NavMesh.RemoveAllNavMeshData();

			var d = new NavMeshData(1);
			var s = new NavMeshBuildSettings();
			s.agentTypeID = 1;
			s.agentRadius = 0.1f;
			s.agentHeight = 2;
			s.agentClimb = 0.1f;
			s.agentSlope = 60;

			NavMesh.AddNavMeshData(d);

			//NavMeshBuilder.UpdateNavMeshData(
			//	d, s, sources_,
			//	new Bounds(
			//		new UnityEngine.Vector3(0, 0, 0),
			//		new UnityEngine.Vector3(100, 0.1f, 100)));
			//

			var srcs = new List<NavMeshBuildSource>();
			var markups = new List<NavMeshBuildMarkup>();

			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (a.type == "Person" ||
					a.type == "Empty" ||
					a.type == "PlayerNavigationPanel" ||
					a.type == "WindowCamera")
				{
					var m = new NavMeshBuildMarkup();
					m.root = a.transform;
					m.ignoreFromBuild = true;
					markups.Add(m);
				}

				foreach (var sc in a.GetComponentsInChildren<SphereCollider>())
				{
					if (sc.name == "control")
					{
						var m = new NavMeshBuildMarkup();
						m.root = sc.transform;
						m.ignoreFromBuild = true;
						markups.Add(m);
					}
				}
			}

			NavMeshBuilder.CollectSources(
				new Bounds(
					new UnityEngine.Vector3(0, 0, 0),
					new UnityEngine.Vector3(100, 0.2f, 100)),
				~0, NavMeshCollectGeometry.PhysicsColliders, 0,
				markups, srcs);

			//foreach (var ss in srcs)
			//{
			//	if (ss.sourceObject != null)
			//		Cue.LogError(ss.sourceObject.ToString());
			//	else if (ss.component != null)
			//		Cue.LogError(ss.component.ToString());
			//	else
			//		Cue.LogError("?");
			//}

			//Cue.LogError(srcs.Count.ToString());

			NavMeshBuilder.UpdateNavMeshData(
				d, s, srcs,
				new Bounds(
					new UnityEngine.Vector3(0, 0, 0),
					new UnityEngine.Vector3(100, 0.2f, 100)));

			CheckRender();
		}

		public List<Vector3> Calculate(Vector3 from, Vector3 to)
		{
			var list = new List<Vector3>();

			var path = new NavMeshPath();
			var f = new NavMeshQueryFilter();
			f.areaMask = NavMesh.AllAreas;
			f.agentTypeID = 1;

			bool b = NavMesh.CalculatePath(
				Vector3.ToUnity(from),
				Vector3.ToUnity(to),
				f, path);

			if (b)
			{
				for (int i=0; i<path.corners.Length; ++i)
					list.Add(Vector3.FromUnity(path.corners[i]));
			}

			return list;
		}

		public void OnPluginState(bool b)
		{
			if (b)
			{
				Update();
			}
			else
			{
				if (nmr_ != null)
				{
					UnityEngine.Object.Destroy(nmr_);
					nmr_ = null;
				}
			}
		}

		public bool Render
		{
			get
			{
				return render_;
			}

			set
			{
				render_ = value;
				CheckRender();
			}
		}

		private void CheckRender()
		{
			if (render_)
			{
				if (nmr_ == null)
					nmr_ = Camera.main.gameObject.AddComponent<NavMeshRenderer>();

				nmr_.Update();
			}
			else
			{
				if (nmr_ != null)
				{
					UnityEngine.Object.Destroy(nmr_);
					nmr_ = null;
				}
			}
		}
	}
}
