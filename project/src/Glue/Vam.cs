﻿using System.Collections.Generic;
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
			GL.wireframe = true;
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
			GL.wireframe = false;

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

		public VamAtom(Atom atom)
		{
			atom_ = atom;
		}

		public bool IsPerson
		{
			get { return atom_.type == "Person"; }
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

		public Atom Atom
		{
			get { return atom_; }
		}
	}

	class VamNav : INav
	{
		private NavMeshRenderer nmr_ = null;
		private readonly List<NavMeshBuildSource> sources_ = new List<NavMeshBuildSource>();

		public void AddBox(float x, float z, float w, float h)
		{
			var src = new NavMeshBuildSource();
			src.transform = Matrix4x4.Translate(new UnityEngine.Vector3(x, 0, z));
			src.shape = NavMeshBuildSourceShape.Box;
			src.size = new UnityEngine.Vector3(w, 0, h);

			sources_.Add(src);
			Rebuild();
		}

		private void Rebuild()
		{
			NavMesh.RemoveAllNavMeshData();

			var d = new NavMeshData();
			var s = new NavMeshBuildSettings();

			NavMesh.AddNavMeshData(d);

			NavMeshBuilder.UpdateNavMeshData(
				d, s, sources_,
				new Bounds(
					new UnityEngine.Vector3(0, 0, 0),
					new UnityEngine.Vector3(100, 0.1f, 100)));

			if (nmr_ == null)
				nmr_ = Camera.main.gameObject.AddComponent<NavMeshRenderer>();

			nmr_.Update();
		}

		public List<Vector3> Calculate(Vector3 from, Vector3 to)
		{
			var list = new List<Vector3>();

			var path = new NavMeshPath();
			bool b = NavMesh.CalculatePath(
				Vector3.ToUnity(from),
				Vector3.ToUnity(to),
				NavMesh.AllAreas, path);

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
				Rebuild();
			}
			else
			{
				Object.Destroy(nmr_);
				nmr_ = null;
			}
		}
	}
}
