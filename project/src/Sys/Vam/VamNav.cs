using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Cue.W
{
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
				for (int i = 0; i < path.corners.Length; ++i)
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
