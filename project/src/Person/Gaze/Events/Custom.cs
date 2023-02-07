using System.Collections.Generic;

namespace Cue
{
	class GazeCustom : BasicGazeEvent
	{
		private class CustomTarget : GazeTargets.ICustomTarget
		{
			private readonly Sys.IAtom atom_;
			private readonly float weight_;
			private readonly bool exclusive_;
			private readonly bool wasOn_;
			private readonly string previousData_;

			public CustomTarget(Sys.IAtom atom, float weight, bool exclusive)
			{
				atom_ = atom;
				weight_ = weight;
				exclusive_ = exclusive;
				wasOn_ = atom.Visible;
				previousData_ = atom.Data;
			}

			public string Name
			{
				get { return atom_.ID; }
			}

			public bool Stale()
			{
				if (wasOn_ != atom_.Visible)
					return true;

				if (previousData_ != atom_.Data)
					return true;

				return false;
			}

			public float Weight
			{
				get { return weight_; }
			}

			public bool Exclusive
			{
				get { return exclusive_; }
			}

			public Vector3 Position
			{
				get { return atom_.Position; }
			}

			public override string ToString()
			{
				return $"{atom_.ID} weight {weight_:0.00} exclusive={exclusive_}";
			}
		}


		private CustomTarget[] customTargets_ = new CustomTarget[0];
		private bool hasExclusive_ = false;

		public GazeCustom(Person p)
			: base(p, I.GazeCustom)
		{
			Cue.Instance.Sys.AtomsChanged += FindAtoms;
			FindAtoms();
		}

		protected override int DoCheck(int flags)
		{
			if (customTargets_.Length == 0)
			{
				SetLastResult("no custom targets");
				return Continue;
			}

			for (int i = 0; i < customTargets_.Length; ++i)
			{
				if (customTargets_[i].Stale())
				{
					FindAtoms();
					break;
				}
			}

			if (customTargets_.Length == 0)
			{
				SetLastResult("no custom targets");
				return Continue;
			}

			if (hasExclusive_)
			{
				targets_.Clear();

				for (int i = 0; i < customTargets_.Length; ++i)
				{
					if (!customTargets_[i].Exclusive)
						continue;

					targets_.SetCustomWeight(
						i, customTargets_[i].Weight, "custom exclusive");
				}

				SetLastResult("found exclusive; stop");
				return Stop;
			}
			else
			{
				for (int i = 0; i < customTargets_.Length; ++i)
				{
					targets_.SetCustomWeight(
						i, customTargets_[i].Weight, "custom");
				}

				SetLastResult("normal, no exclusive");
			}

			return Continue;
		}

		private void FindAtoms()
		{
			var atoms = Cue.Instance.Sys.GetAtoms(true);
			var targets = new List<CustomTarget>();
			var validTargets = new List<CustomTarget>();

			hasExclusive_ = false;

			for (int i = 0; i < atoms.Count; ++i)
			{
				if (atoms[i].ID.StartsWith("cue.gaze"))
				{
					string[] lines = atoms[i].Data.Split('\n');

					string name = null;
					float weight = 1;
					bool exclusive = false;

					if (lines.Length > 0)
					{
						name = lines[0].Trim();
						if (name != person_.ID && name.ToLower() != "all")
							name = null;
					}

					if (lines.Length > 1)
						float.TryParse(lines[1], out weight);

					if (lines.Length > 2)
					{
						if (lines[2].Trim().ToLower() == "exclusive")
							exclusive = true;
					}

					var t = new CustomTarget(atoms[i], weight, exclusive);
					Cue.Instance.Log.Info($"{person_.ID}.GazeCustom: found target {t}");

					targets.Add(t);

					if (atoms[i].Visible && name != null)
					{
						if (exclusive)
							hasExclusive_ = true;

						validTargets.Add(t);
					}
				}
			}

			customTargets_ = targets.ToArray();
			person_.Gaze.Targets.SetCustomPositions(validTargets.ToArray());
		}

		public override string ToString()
		{
			return "custom";
		}
	}
}
