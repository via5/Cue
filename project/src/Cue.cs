using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cue
{
	class NavMeshRenderer : MonoBehaviour
	{
		Material mat = null;
		UnityEngine.AI.NavMeshTriangulation tris_;

		public void Update()
		{
			tris_ = UnityEngine.AI.NavMesh.CalculateTriangulation();
		}

		public void OnPostRender()
		{
			if (mat == null)
			{
				mat = new Material(Shader.Find("Battlehub/RTGizmos/Handles"));
				mat.color = new Color(1, 1, 1, 0.1f);
				mat.SetFloat("_Offset", 1f);
				mat.SetFloat("_MinAlpha", 1f);
			}

			var walkableColor = new Color(0, 1, 0, 1);
			var nonWalkableColor = new Color(1, 0, 0, 1);
			var unknownColor = new Color(0, 0, 1, 1);

			GL.PushMatrix();

			mat.SetPass(0);
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

	class Cue : MVRScript
	{
		private static Cue instance_ = null;
		private W.ISys sys_ = null;
		private Person person_ = null;
		private readonly List<IObject> objects_ = new List<IObject>();
		private UI.IUI ui_ = null;
		private NavMeshRenderer nmr = null;

		public Cue()
		{
			instance_ = this;
		}

		public static Cue Instance
		{
			get { return instance_; }
		}

		public W.ISys Sys
		{
			get { return sys_; }
		}

		public List<IObject> Objects
		{
			get { return objects_; }
		}

		public Person Person
		{
			get { return person_; }
		}

		public override void Init()
		{
			base.Init();

			try
			{
				SuperController.singleton.StartCoroutine(DeferredInit());
			}
			catch (Exception e)
			{
				SuperController.LogError(e.Message);
			}
		}

		private UnityEngine.AI.NavMeshBuildSource AddBox(
			float x, float z, float w, float h)
		{
			var src = new UnityEngine.AI.NavMeshBuildSource();
			src.transform = transform.localToWorldMatrix * Matrix4x4.Translate(new UnityEngine.Vector3(x, 0, z));
			src.shape = UnityEngine.AI.NavMeshBuildSourceShape.Box;
			src.size = new UnityEngine.Vector3(w, 0, h);
			return src;
		}

		private IEnumerator DeferredInit()
		{
			yield return new WaitForEndOfFrame();

			if (W.MockSys.Instance != null)
			{
				sys_ = W.MockSys.Instance;
			}
			else
			{
				sys_ = new W.VamSys(this);
				ui_ = new UI.VamUI();
			}

			var o = new BasicObject(sys_.GetAtom("Bed1"));
			o.SitSlot = new Slot(new Vector3(0, 0, -1.3f), 180);
			objects_.Add(o);

			o = new BasicObject(sys_.GetAtom("Chair1"));
			o.SitSlot = new Slot(new Vector3(0, 0, 0.3f), 0);
			objects_.Add(o);

			o = new BasicObject(sys_.GetAtom("Empty"));
			o.SitSlot = new Slot(new Vector3(0, 0, 0.3f), 0);
			objects_.Add(o);

			o = new BasicObject(sys_.GetAtom("Table1"));
			objects_.Add(o);

			person_ = new Person(sys_.GetAtom("Person"));

			ui_.Init();


			var d = new UnityEngine.AI.NavMeshData();
			var s = new UnityEngine.AI.NavMeshBuildSettings();

			UnityEngine.AI.NavMesh.AddNavMeshData(d);

			var sources = new List<UnityEngine.AI.NavMeshBuildSource>();
			sources.Add(AddBox(-1.5f, 0, 1.5f, 7));
			sources.Add(AddBox(-3, 2.5f, 6, 1));
			sources.Add(AddBox(0, 0, 1.5f, 0.8f));
			sources.Add(AddBox(3, 2.2f, 10, 2.0f));
			sources.Add(AddBox(3.7f, 6f, 4.5f, 6));

			UnityEngine.AI.NavMeshBuilder.UpdateNavMeshData(
				d, s, sources,
				new Bounds(
					new UnityEngine.Vector3(0, 0, 0),
					new UnityEngine.Vector3(100, 0.1f, 100)));

			nmr = Camera.main.gameObject.AddComponent<NavMeshRenderer>();
			nmr.Update();
		}

		public void Update()
		{
			U.Safe(() =>
			{
				if (!sys_.Paused)
					person_.Update(sys_.Time.deltaTime);

				ui_.Update();
			});
		}

		public void FixedUpdate()
		{
			U.Safe(() =>
			{
				if (sys_.Paused)
					return;

				person_.FixedUpdate(sys_.Time.deltaTime);
			});
		}

		public void OnEnable()
		{
			U.Safe(() =>
			{
			});
		}

		public void OnDisable()
		{
			U.Safe(() =>
			{
				UnityEngine.Object.Destroy(nmr);
				UnityEngine.AI.NavMesh.RemoveAllNavMeshData();
				nmr = null;
			});
		}

		static public void LogVerbose(string s)
		{
			//Instance.sys_.Log.Verbose(s);
		}

		static public void LogInfo(string s)
		{
			Instance.sys_.Log.Info(s);
		}

		static public void LogWarning(string s)
		{
			Instance.sys_.Log.Error(s);
		}

		static public void LogError(string s)
		{
			Instance.sys_.Log.Error(s);
		}
	}
}
