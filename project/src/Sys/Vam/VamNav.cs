using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Cue.W
{
	using Color = UnityEngine.Color;

	class NavMeshRenderer : MonoBehaviour
	{
		private Material mat_ = null;
		private NavMeshTriangulation tris_;

		public void Update()
		{
			tris_ = NavMesh.CalculateTriangulation();
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

			GL.PopMatrix();
		}
	}


	class VamNav : INav
	{
		public const int AgentTypeID = 1;
		public const float AgentHeight = 2.0f;
		public const float AgentRadius = 0.1f;
		public const float AgentMoveSpeed = 1.0f;
		public const float AgentTurnSpeed = 360.0f;

		private NavMeshRenderer nmr_ = null;
		private bool render_ = false;

		public void Update()
		{
			NavMesh.RemoveAllNavMeshData();

			var d = new NavMeshData(AgentTypeID);
			var srcs = new List<NavMeshBuildSource>();

			NavMeshBuilder.CollectSources(
				new Bounds(
					new UnityEngine.Vector3(0, 0, 0),
					new UnityEngine.Vector3(100, 0.2f, 100)),
				~0, NavMeshCollectGeometry.PhysicsColliders, 0,
				CreateMarkups(), srcs);

			var s = new NavMeshBuildSettings();
			s.agentTypeID = AgentTypeID;
			s.agentRadius = AgentRadius;
			s.agentHeight = AgentHeight;
			s.agentClimb = 0.1f;
			s.agentSlope = 60;

			NavMeshBuilder.UpdateNavMeshData(
				d, s, srcs,
				new Bounds(
					new UnityEngine.Vector3(0, 0, 0),
					new UnityEngine.Vector3(1000, 1000, 1000)));

			NavMesh.AddNavMeshData(d);
			CheckRender();
		}

		private List<NavMeshBuildMarkup> CreateMarkups()
		{
			// ghetto sets
			var ignoreTypes = new Dictionary<string, int>()
			{
				{ "InvisibleLight", 0 },
				{ "Empty", 0 },
				{ "PlayerNavigationPanel", 0}
			};

			var ignoreCategories = new Dictionary<string, int>()
			{
				{ "Animation", 0},
				{ "Force", 0},
				{ "People", 0 },
				{ "Sound", 0 },
				{ "Triggers", 0},
				{ "View", 0 },
				{ "Core", 0 }
			};


			var markups = new List<NavMeshBuildMarkup>();

			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (ignoreCategories.ContainsKey(a.category) ||
					ignoreTypes.ContainsKey(a.type))
				{
					var m = new NavMeshBuildMarkup();
					m.root = a.transform;
					m.ignoreFromBuild = true;
					markups.Add(m);
				}
				else
				{
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
			}

			return markups;
		}

		public List<Vector3> Calculate(Vector3 from, Vector3 to)
		{
			var list = new List<Vector3>();

			var path = new NavMeshPath();
			var f = new NavMeshQueryFilter();
			f.areaMask = NavMesh.AllAreas;
			f.agentTypeID = AgentTypeID;

			bool b = NavMesh.CalculatePath(
				VamU.ToUnity(from),
				VamU.ToUnity(to),
				f, path);

			if (b)
			{
				for (int i = 0; i < path.corners.Length; ++i)
					list.Add(VamU.FromUnity(path.corners[i]));
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
					Object.Destroy(nmr_);
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
					Object.Destroy(nmr_);
					nmr_ = null;
				}
			}
		}
	}
}
