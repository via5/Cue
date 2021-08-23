using Cue.Sys;
using System;

namespace Cue
{
	class ClothingManager : IClothing
	{
		private const string Visible = "visible";
		private const string Hidden = "hidden";

		private readonly Person person_;
		private Sys.Vam.FloatParameterRO heelsAngle_;
		private Sys.Vam.FloatParameterRO heelsHeight_;
		private Sys.Vam.StringChooserParameter genitalsState_;
		private Sys.Vam.StringChooserParameter breastsState_;

		public ClothingManager(Person p)
		{
			person_ = p;

			heelsAngle_ = new Sys.Vam.FloatParameterRO(
				p, "via5.ClothingManager", "Heels angle");

			heelsHeight_ = new Sys.Vam.FloatParameterRO(
				p, "via5.ClothingManager", "Heels height");

			genitalsState_ = new Sys.Vam.StringChooserParameter(
				p, "via5.ClothingManager", "Genitals state");

			breastsState_ = new Sys.Vam.StringChooserParameter(
				p, "via5.ClothingManager", "Breasts state");
		}

		public float HeelsAngle
		{
			get { return heelsAngle_.Value; }
		}

		public float HeelsHeight
		{
			get { return heelsHeight_.Value; }
		}

		public bool GenitalsVisible
		{
			get { return (genitalsState_.Value == Visible); }
			set { genitalsState_.Value = (value ? Visible : Hidden); }
		}

		public bool BreastsVisible
		{
			get { return (breastsState_.Value == Visible); }
			set { breastsState_.Value = (value ? Visible : Hidden); }
		}

		private static string GetID(DAZClothingItem item)
		{
			if (item.internalUid != "")
				return item.internalUid;
			else
				return item.name;
		}

		public void Dump()
		{
			var cs = person_.VamAtom.Atom.GetComponentInChildren<DAZCharacterSelector>();
			if (cs != null)
			{
				foreach (var c in cs.clothingItems)
				{
					if (c.isActiveAndEnabled)
						Cue.LogInfo($"{GetID(c)}");
				}
			}
		}

		public override string ToString()
		{
			return
				$"cm: " +
				$"genitals={genitalsState_} breasts={breastsState_}";
		}
	}
}
