// auto generated from PersonalityEnums.tt

using System.Collections.Generic;

namespace Cue
{
	public class PS : BasicEnumValues
	{
		// durations
		public static readonly DurationIndex GazeDuration = new DurationIndex(0);
		public static readonly DurationIndex GazeRandomInterval = new DurationIndex(1);
		public static readonly DurationIndex EmergencyGazeDuration = new DurationIndex(2);
		public static readonly DurationIndex GazeSaccadeInterval = new DurationIndex(3);
		public static readonly DurationIndex ZappedByPlayerGazeDuration = new DurationIndex(4);
		public static readonly DurationIndex ZappedByOtherGazeDuration = new DurationIndex(5);
		public static readonly DurationIndex OtherZappedGazeDuration = new DurationIndex(6);

		public const int DurationCount = 7;
		public override int GetDurationCount() { return 7; }

		// bools
		public static readonly BoolIndex GazeSaccade = new BoolIndex(0);
		public static readonly BoolIndex ZappedEnabled = new BoolIndex(1);

		public const int BoolCount = 2;
		public override int GetBoolCount() { return 2; }

		// floats
		public static readonly FloatIndex GazeSaccadeMovementRange = new FloatIndex(0);
		public static readonly FloatIndex AvoidGazePlayer = new FloatIndex(1);
		public static readonly FloatIndex AvoidGazePlayerInsidePersonalSpace = new FloatIndex(2);
		public static readonly FloatIndex AvoidGazePlayerDuringSex = new FloatIndex(3);
		public static readonly FloatIndex AvoidGazePlayerDelayAfterOrgasm = new FloatIndex(4);
		public static readonly FloatIndex AvoidGazePlayerWeight = new FloatIndex(5);
		public static readonly FloatIndex AvoidGazeOthers = new FloatIndex(6);
		public static readonly FloatIndex AvoidGazeOthersInsidePersonalSpace = new FloatIndex(7);
		public static readonly FloatIndex AvoidGazeOthersDuringSex = new FloatIndex(8);
		public static readonly FloatIndex AvoidGazeOthersDelayAfterOrgasm = new FloatIndex(9);
		public static readonly FloatIndex AvoidGazeOthersWeight = new FloatIndex(10);
		public static readonly FloatIndex AvoidGazeUninvolvedHavingSex = new FloatIndex(11);
		public static readonly FloatIndex LookAboveMaxWeight = new FloatIndex(12);
		public static readonly FloatIndex LookAboveMaxWeightOrgasm = new FloatIndex(13);
		public static readonly FloatIndex LookAboveMinExcitement = new FloatIndex(14);
		public static readonly FloatIndex LookAboveMinPhysicalRate = new FloatIndex(15);
		public static readonly FloatIndex IdleNaturalRandomWeight = new FloatIndex(16);
		public static readonly FloatIndex IdleEmptyRandomWeight = new FloatIndex(17);
		public static readonly FloatIndex NaturalRandomWeight = new FloatIndex(18);
		public static readonly FloatIndex NaturalOtherEyesWeight = new FloatIndex(19);
		public static readonly FloatIndex BusyOtherEyesWeight = new FloatIndex(20);
		public static readonly FloatIndex NaturalPlayerEyesWeight = new FloatIndex(21);
		public static readonly FloatIndex BusyPlayerEyesWeight = new FloatIndex(22);
		public static readonly FloatIndex MaxTirednessForRandomGaze = new FloatIndex(23);
		public static readonly FloatIndex OtherEyesExcitementWeight = new FloatIndex(24);
		public static readonly FloatIndex OtherEyesOrgasmWeight = new FloatIndex(25);
		public static readonly FloatIndex BlowjobEyesWeight = new FloatIndex(26);
		public static readonly FloatIndex BlowjobGenitalsWeight = new FloatIndex(27);
		public static readonly FloatIndex HandjobEyesWeight = new FloatIndex(28);
		public static readonly FloatIndex HandjobGenitalsWeight = new FloatIndex(29);
		public static readonly FloatIndex PenetratedEyesWeight = new FloatIndex(30);
		public static readonly FloatIndex PenetratedGenitalsWeight = new FloatIndex(31);
		public static readonly FloatIndex PenetratingEyesWeight = new FloatIndex(32);
		public static readonly FloatIndex PenetratingGenitalsWeight = new FloatIndex(33);
		public static readonly FloatIndex GropedEyesWeight = new FloatIndex(34);
		public static readonly FloatIndex GropedTargetWeight = new FloatIndex(35);
		public static readonly FloatIndex GropingEyesWeight = new FloatIndex(36);
		public static readonly FloatIndex GropingTargetWeight = new FloatIndex(37);
		public static readonly FloatIndex OtherBlowjobEyesWeight = new FloatIndex(38);
		public static readonly FloatIndex OtherBlowjobTargetEyesWeight = new FloatIndex(39);
		public static readonly FloatIndex OtherBlowjobTargetGenitalsWeight = new FloatIndex(40);
		public static readonly FloatIndex OtherHandjobEyesWeight = new FloatIndex(41);
		public static readonly FloatIndex OtherHandjobTargetEyesWeight = new FloatIndex(42);
		public static readonly FloatIndex OtherHandjobTargetGenitalsWeight = new FloatIndex(43);
		public static readonly FloatIndex OtherPenetrationEyesWeight = new FloatIndex(44);
		public static readonly FloatIndex OtherPenetrationSourceEyesWeight = new FloatIndex(45);
		public static readonly FloatIndex OtherPenetrationSourceGenitalsWeight = new FloatIndex(46);
		public static readonly FloatIndex OtherGropedEyesWeight = new FloatIndex(47);
		public static readonly FloatIndex OtherGropedSourceEyesWeight = new FloatIndex(48);
		public static readonly FloatIndex OtherGropedTargetWeight = new FloatIndex(49);
		public static readonly FloatIndex ZappedTentativeTime = new FloatIndex(50);
		public static readonly FloatIndex ZappedCooldown = new FloatIndex(51);
		public static readonly FloatIndex ZappedTime = new FloatIndex(52);
		public static readonly FloatIndex ZappedByPlayerBreastsEyesWeight = new FloatIndex(53);
		public static readonly FloatIndex ZappedByPlayerBreastsTargetWeight = new FloatIndex(54);
		public static readonly FloatIndex ZappedByPlayerBreastsExcitement = new FloatIndex(55);
		public static readonly FloatIndex ZappedByPlayerGenitalsEyesWeight = new FloatIndex(56);
		public static readonly FloatIndex ZappedByPlayerGenitalsTargetWeight = new FloatIndex(57);
		public static readonly FloatIndex ZappedByPlayerGenitalsExcitement = new FloatIndex(58);
		public static readonly FloatIndex ZappedByPlayerPenetrationEyesWeight = new FloatIndex(59);
		public static readonly FloatIndex ZappedByPlayerPenetrationTargetWeight = new FloatIndex(60);
		public static readonly FloatIndex ZappedByPlayerPenetrationExcitement = new FloatIndex(61);
		public static readonly FloatIndex ZappedByPlayerMouthEyesWeight = new FloatIndex(62);
		public static readonly FloatIndex ZappedByPlayerMouthExcitement = new FloatIndex(63);
		public static readonly FloatIndex ZappedByPlayerLookUpWeight = new FloatIndex(64);
		public static readonly FloatIndex ZappedByOtherBreastsEyesWeight = new FloatIndex(65);
		public static readonly FloatIndex ZappedByOtherBreastsTargetWeight = new FloatIndex(66);
		public static readonly FloatIndex ZappedByOtherBreastsExcitement = new FloatIndex(67);
		public static readonly FloatIndex ZappedByOtherGenitalsEyesWeight = new FloatIndex(68);
		public static readonly FloatIndex ZappedByOtherGenitalsTargetWeight = new FloatIndex(69);
		public static readonly FloatIndex ZappedByOtherGenitalsExcitement = new FloatIndex(70);
		public static readonly FloatIndex ZappedByOtherPenetrationEyesWeight = new FloatIndex(71);
		public static readonly FloatIndex ZappedByOtherPenetrationTargetWeight = new FloatIndex(72);
		public static readonly FloatIndex ZappedByOtherPenetrationExcitement = new FloatIndex(73);
		public static readonly FloatIndex ZappedByOtherMouthEyesWeight = new FloatIndex(74);
		public static readonly FloatIndex ZappedByOtherMouthExcitement = new FloatIndex(75);
		public static readonly FloatIndex ZappedByOtherLookUpWeight = new FloatIndex(76);
		public static readonly FloatIndex OtherZappedEyesWeight = new FloatIndex(77);
		public static readonly FloatIndex OtherZappedTargetWeight = new FloatIndex(78);
		public static readonly FloatIndex OtherZappedSourceWeight = new FloatIndex(79);
		public static readonly FloatIndex LookAtPlayerOnGrabWeight = new FloatIndex(80);
		public static readonly FloatIndex LookAtPlayerTimeAfterGrab = new FloatIndex(81);
		public static readonly FloatIndex KissSpeedEnergyFactor = new FloatIndex(82);
		public static readonly FloatIndex IdleMaxExcitement = new FloatIndex(83);
		public static readonly FloatIndex TirednessExcitementRateFactor = new FloatIndex(84);
		public static readonly FloatIndex GazeEnergyTirednessFactor = new FloatIndex(85);
		public static readonly FloatIndex GazeTirednessFactor = new FloatIndex(86);
		public static readonly FloatIndex MovementEnergyTirednessFactor = new FloatIndex(87);
		public static readonly FloatIndex ExpressionTirednessFactor = new FloatIndex(88);
		public static readonly FloatIndex MovementEnergyRampUpDelayAfterOrgasm = new FloatIndex(89);
		public static readonly FloatIndex MovementEnergyRampUpAfterOrgasm = new FloatIndex(90);
		public static readonly FloatIndex AvoidGazeAnger = new FloatIndex(91);
		public static readonly FloatIndex AngerWhenPlayerInteracts = new FloatIndex(92);
		public static readonly FloatIndex AngerMaxExcitementForAnger = new FloatIndex(93);
		public static readonly FloatIndex AngerMaxExcitementForHappiness = new FloatIndex(94);
		public static readonly FloatIndex AngerExcitementFactorForAnger = new FloatIndex(95);
		public static readonly FloatIndex AngerExcitementFactorForHappiness = new FloatIndex(96);
		public static readonly FloatIndex DefaultHappiness = new FloatIndex(97);
		public static readonly FloatIndex DefaultAnger = new FloatIndex(98);
		public static readonly FloatIndex DefaultPlayfulness = new FloatIndex(99);
		public static readonly FloatIndex MaxExcitedExpression = new FloatIndex(100);
		public static readonly FloatIndex ExpressionMinHoldTime = new FloatIndex(101);
		public static readonly FloatIndex ExpressionMaxHoldTime = new FloatIndex(102);
		public static readonly FloatIndex ExcitedExpressionWeightModifier = new FloatIndex(103);
		public static readonly FloatIndex ExclusiveExpressionWeightModifier = new FloatIndex(104);
		public static readonly FloatIndex OrgasmExpressionRangeMin = new FloatIndex(105);
		public static readonly FloatIndex OrgasmExpressionRangeMax = new FloatIndex(106);
		public static readonly FloatIndex OrgasmFirstExpressionTime = new FloatIndex(107);
		public static readonly FloatIndex OrgasmSyncMinExcitement = new FloatIndex(108);
		public static readonly FloatIndex SlapMinExpressionChange = new FloatIndex(109);
		public static readonly FloatIndex SlapMaxExpressionChange = new FloatIndex(110);
		public static readonly FloatIndex SlapMinTime = new FloatIndex(111);
		public static readonly FloatIndex SlapMaxTime = new FloatIndex(112);
		public static readonly FloatIndex MaxSweat = new FloatIndex(113);
		public static readonly FloatIndex MaxFlush = new FloatIndex(114);
		public static readonly FloatIndex TemperatureExcitementMax = new FloatIndex(115);
		public static readonly FloatIndex TemperatureExcitementRate = new FloatIndex(116);
		public static readonly FloatIndex TemperatureDecayRate = new FloatIndex(117);
		public static readonly FloatIndex TirednessRateDuringPostOrgasm = new FloatIndex(118);
		public static readonly FloatIndex TirednessBaseDecayRate = new FloatIndex(119);
		public static readonly FloatIndex TirednessBackToBaseRate = new FloatIndex(120);
		public static readonly FloatIndex DelayAfterOrgasmUntilTirednessDecay = new FloatIndex(121);
		public static readonly FloatIndex TirednessMaxExcitementForBaseDecay = new FloatIndex(122);
		public static readonly FloatIndex OrgasmBaseTirednessIncrease = new FloatIndex(123);
		public static readonly FloatIndex ExcitementDecayRate = new FloatIndex(124);
		public static readonly FloatIndex ExcitementPostOrgasm = new FloatIndex(125);
		public static readonly FloatIndex OrgasmTime = new FloatIndex(126);
		public static readonly FloatIndex PostOrgasmTime = new FloatIndex(127);
		public static readonly FloatIndex RateAdjustment = new FloatIndex(128);
		public static readonly FloatIndex PenetrationDamper = new FloatIndex(129);
		public static readonly FloatIndex MinCollisionMagnitude = new FloatIndex(130);

		public const int FloatCount = 131;
		public override int GetFloatCount() { return 131; }

		// strings
		public static readonly StringIndex MovementEnergyRampUpAfterOrgasmEasing = new StringIndex(0);

		public const int StringCount = 1;
		public override int GetStringCount() { return 1; }




		private static string[] durationNames_ = new string[]
		{
			"gazeDuration",
			"gazeRandomInterval",
			"emergencyGazeDuration",
			"gazeSaccadeInterval",
			"zappedByPlayerGazeDuration",
			"zappedByOtherGazeDuration",
			"otherZappedGazeDuration",
		};

		public static int DurationFromString(string s)
		{
			for (int i = 0; i<durationNames_.Length; ++i)
			{
				if (durationNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static int[] DurationFromStringMany(string s)
		{
			var list = new List<int>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = DurationFromString(tp);
				if (i != -1)
					list.Add(i);
			}

			return list.ToArray();
		}

		public override string GetDurationName(DurationIndex i)
		{
			return DurationToString(i);
		}

		public static string DurationToString(DurationIndex i)
		{
			if (i.index >= 0 && i.index < durationNames_.Length)
				return durationNames_[i.index];
			else
				return $"?{i.index}";
		}

		public static string[] DurationNames
		{
			get { return durationNames_; }
		}

		private static string[] boolNames_ = new string[]
		{
			"gazeSaccade",
			"zappedEnabled",
		};

		public static int BoolFromString(string s)
		{
			for (int i = 0; i<boolNames_.Length; ++i)
			{
				if (boolNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static int[] BoolFromStringMany(string s)
		{
			var list = new List<int>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = BoolFromString(tp);
				if (i != -1)
					list.Add(i);
			}

			return list.ToArray();
		}

		public override string GetBoolName(BoolIndex i)
		{
			return BoolToString(i);
		}

		public static string BoolToString(BoolIndex i)
		{
			if (i.index >= 0 && i.index < boolNames_.Length)
				return boolNames_[i.index];
			else
				return $"?{i.index}";
		}

		public static string[] BoolNames
		{
			get { return boolNames_; }
		}

		private static string[] floatNames_ = new string[]
		{
			"gazeSaccadeMovementRange",
			"avoidGazePlayer",
			"avoidGazePlayerInsidePersonalSpace",
			"avoidGazePlayerDuringSex",
			"avoidGazePlayerDelayAfterOrgasm",
			"avoidGazePlayerWeight",
			"avoidGazeOthers",
			"avoidGazeOthersInsidePersonalSpace",
			"avoidGazeOthersDuringSex",
			"avoidGazeOthersDelayAfterOrgasm",
			"avoidGazeOthersWeight",
			"avoidGazeUninvolvedHavingSex",
			"lookAboveMaxWeight",
			"lookAboveMaxWeightOrgasm",
			"lookAboveMinExcitement",
			"lookAboveMinPhysicalRate",
			"idleNaturalRandomWeight",
			"idleEmptyRandomWeight",
			"naturalRandomWeight",
			"naturalOtherEyesWeight",
			"busyOtherEyesWeight",
			"naturalPlayerEyesWeight",
			"busyPlayerEyesWeight",
			"maxTirednessForRandomGaze",
			"otherEyesExcitementWeight",
			"otherEyesOrgasmWeight",
			"blowjobEyesWeight",
			"blowjobGenitalsWeight",
			"handjobEyesWeight",
			"handjobGenitalsWeight",
			"penetratedEyesWeight",
			"penetratedGenitalsWeight",
			"penetratingEyesWeight",
			"penetratingGenitalsWeight",
			"gropedEyesWeight",
			"gropedTargetWeight",
			"gropingEyesWeight",
			"gropingTargetWeight",
			"otherBlowjobEyesWeight",
			"otherBlowjobTargetEyesWeight",
			"otherBlowjobTargetGenitalsWeight",
			"otherHandjobEyesWeight",
			"otherHandjobTargetEyesWeight",
			"otherHandjobTargetGenitalsWeight",
			"otherPenetrationEyesWeight",
			"otherPenetrationSourceEyesWeight",
			"otherPenetrationSourceGenitalsWeight",
			"otherGropedEyesWeight",
			"otherGropedSourceEyesWeight",
			"otherGropedTargetWeight",
			"zappedTentativeTime",
			"zappedCooldown",
			"zappedTime",
			"zappedByPlayerBreastsEyesWeight",
			"zappedByPlayerBreastsTargetWeight",
			"zappedByPlayerBreastsExcitement",
			"zappedByPlayerGenitalsEyesWeight",
			"zappedByPlayerGenitalsTargetWeight",
			"zappedByPlayerGenitalsExcitement",
			"zappedByPlayerPenetrationEyesWeight",
			"zappedByPlayerPenetrationTargetWeight",
			"zappedByPlayerPenetrationExcitement",
			"zappedByPlayerMouthEyesWeight",
			"zappedByPlayerMouthExcitement",
			"zappedByPlayerLookUpWeight",
			"zappedByOtherBreastsEyesWeight",
			"zappedByOtherBreastsTargetWeight",
			"zappedByOtherBreastsExcitement",
			"zappedByOtherGenitalsEyesWeight",
			"zappedByOtherGenitalsTargetWeight",
			"zappedByOtherGenitalsExcitement",
			"zappedByOtherPenetrationEyesWeight",
			"zappedByOtherPenetrationTargetWeight",
			"zappedByOtherPenetrationExcitement",
			"zappedByOtherMouthEyesWeight",
			"zappedByOtherMouthExcitement",
			"zappedByOtherLookUpWeight",
			"otherZappedEyesWeight",
			"otherZappedTargetWeight",
			"otherZappedSourceWeight",
			"lookAtPlayerOnGrabWeight",
			"lookAtPlayerTimeAfterGrab",
			"kissSpeedEnergyFactor",
			"idleMaxExcitement",
			"tirednessExcitementRateFactor",
			"gazeEnergyTirednessFactor",
			"gazeTirednessFactor",
			"movementEnergyTirednessFactor",
			"expressionTirednessFactor",
			"movementEnergyRampUpDelayAfterOrgasm",
			"movementEnergyRampUpAfterOrgasm",
			"avoidGazeAnger",
			"angerWhenPlayerInteracts",
			"angerMaxExcitementForAnger",
			"angerMaxExcitementForHappiness",
			"angerExcitementFactorForAnger",
			"angerExcitementFactorForHappiness",
			"defaultHappiness",
			"defaultAnger",
			"defaultPlayfulness",
			"maxExcitedExpression",
			"expressionMinHoldTime",
			"expressionMaxHoldTime",
			"excitedExpressionWeightModifier",
			"exclusiveExpressionWeightModifier",
			"orgasmExpressionRangeMin",
			"orgasmExpressionRangeMax",
			"orgasmFirstExpressionTime",
			"orgasmSyncMinExcitement",
			"slapMinExpressionChange",
			"slapMaxExpressionChange",
			"slapMinTime",
			"slapMaxTime",
			"maxSweat",
			"maxFlush",
			"temperatureExcitementMax",
			"temperatureExcitementRate",
			"temperatureDecayRate",
			"tirednessRateDuringPostOrgasm",
			"tirednessBaseDecayRate",
			"tirednessBackToBaseRate",
			"delayAfterOrgasmUntilTirednessDecay",
			"tirednessMaxExcitementForBaseDecay",
			"orgasmBaseTirednessIncrease",
			"excitementDecayRate",
			"excitementPostOrgasm",
			"orgasmTime",
			"postOrgasmTime",
			"rateAdjustment",
			"penetrationDamper",
			"minCollisionMagnitude",
		};

		public static int FloatFromString(string s)
		{
			for (int i = 0; i<floatNames_.Length; ++i)
			{
				if (floatNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static int[] FloatFromStringMany(string s)
		{
			var list = new List<int>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = FloatFromString(tp);
				if (i != -1)
					list.Add(i);
			}

			return list.ToArray();
		}

		public override string GetFloatName(FloatIndex i)
		{
			return FloatToString(i);
		}

		public static string FloatToString(FloatIndex i)
		{
			if (i.index >= 0 && i.index < floatNames_.Length)
				return floatNames_[i.index];
			else
				return $"?{i.index}";
		}

		public static string[] FloatNames
		{
			get { return floatNames_; }
		}

		private static string[] stringNames_ = new string[]
		{
			"movementEnergyRampUpAfterOrgasmEasing",
		};

		public static int StringFromString(string s)
		{
			for (int i = 0; i<stringNames_.Length; ++i)
			{
				if (stringNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static int[] StringFromStringMany(string s)
		{
			var list = new List<int>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = StringFromString(tp);
				if (i != -1)
					list.Add(i);
			}

			return list.ToArray();
		}

		public override string GetStringName(StringIndex i)
		{
			return StringToString(i);
		}

		public static string StringToString(StringIndex i)
		{
			if (i.index >= 0 && i.index < stringNames_.Length)
				return stringNames_[i.index];
			else
				return $"?{i.index}";
		}

		public static string[] StringNames
		{
			get { return stringNames_; }
		}



		private static string[] allNames_ = new string[] {
			"gazeDuration",
			"gazeRandomInterval",
			"emergencyGazeDuration",
			"gazeSaccadeInterval",
			"zappedByPlayerGazeDuration",
			"zappedByOtherGazeDuration",
			"otherZappedGazeDuration",
			"gazeSaccade",
			"zappedEnabled",
			"gazeSaccadeMovementRange",
			"avoidGazePlayer",
			"avoidGazePlayerInsidePersonalSpace",
			"avoidGazePlayerDuringSex",
			"avoidGazePlayerDelayAfterOrgasm",
			"avoidGazePlayerWeight",
			"avoidGazeOthers",
			"avoidGazeOthersInsidePersonalSpace",
			"avoidGazeOthersDuringSex",
			"avoidGazeOthersDelayAfterOrgasm",
			"avoidGazeOthersWeight",
			"avoidGazeUninvolvedHavingSex",
			"lookAboveMaxWeight",
			"lookAboveMaxWeightOrgasm",
			"lookAboveMinExcitement",
			"lookAboveMinPhysicalRate",
			"idleNaturalRandomWeight",
			"idleEmptyRandomWeight",
			"naturalRandomWeight",
			"naturalOtherEyesWeight",
			"busyOtherEyesWeight",
			"naturalPlayerEyesWeight",
			"busyPlayerEyesWeight",
			"maxTirednessForRandomGaze",
			"otherEyesExcitementWeight",
			"otherEyesOrgasmWeight",
			"blowjobEyesWeight",
			"blowjobGenitalsWeight",
			"handjobEyesWeight",
			"handjobGenitalsWeight",
			"penetratedEyesWeight",
			"penetratedGenitalsWeight",
			"penetratingEyesWeight",
			"penetratingGenitalsWeight",
			"gropedEyesWeight",
			"gropedTargetWeight",
			"gropingEyesWeight",
			"gropingTargetWeight",
			"otherBlowjobEyesWeight",
			"otherBlowjobTargetEyesWeight",
			"otherBlowjobTargetGenitalsWeight",
			"otherHandjobEyesWeight",
			"otherHandjobTargetEyesWeight",
			"otherHandjobTargetGenitalsWeight",
			"otherPenetrationEyesWeight",
			"otherPenetrationSourceEyesWeight",
			"otherPenetrationSourceGenitalsWeight",
			"otherGropedEyesWeight",
			"otherGropedSourceEyesWeight",
			"otherGropedTargetWeight",
			"zappedTentativeTime",
			"zappedCooldown",
			"zappedTime",
			"zappedByPlayerBreastsEyesWeight",
			"zappedByPlayerBreastsTargetWeight",
			"zappedByPlayerBreastsExcitement",
			"zappedByPlayerGenitalsEyesWeight",
			"zappedByPlayerGenitalsTargetWeight",
			"zappedByPlayerGenitalsExcitement",
			"zappedByPlayerPenetrationEyesWeight",
			"zappedByPlayerPenetrationTargetWeight",
			"zappedByPlayerPenetrationExcitement",
			"zappedByPlayerMouthEyesWeight",
			"zappedByPlayerMouthExcitement",
			"zappedByPlayerLookUpWeight",
			"zappedByOtherBreastsEyesWeight",
			"zappedByOtherBreastsTargetWeight",
			"zappedByOtherBreastsExcitement",
			"zappedByOtherGenitalsEyesWeight",
			"zappedByOtherGenitalsTargetWeight",
			"zappedByOtherGenitalsExcitement",
			"zappedByOtherPenetrationEyesWeight",
			"zappedByOtherPenetrationTargetWeight",
			"zappedByOtherPenetrationExcitement",
			"zappedByOtherMouthEyesWeight",
			"zappedByOtherMouthExcitement",
			"zappedByOtherLookUpWeight",
			"otherZappedEyesWeight",
			"otherZappedTargetWeight",
			"otherZappedSourceWeight",
			"lookAtPlayerOnGrabWeight",
			"lookAtPlayerTimeAfterGrab",
			"kissSpeedEnergyFactor",
			"idleMaxExcitement",
			"tirednessExcitementRateFactor",
			"gazeEnergyTirednessFactor",
			"gazeTirednessFactor",
			"movementEnergyTirednessFactor",
			"expressionTirednessFactor",
			"movementEnergyRampUpDelayAfterOrgasm",
			"movementEnergyRampUpAfterOrgasm",
			"avoidGazeAnger",
			"angerWhenPlayerInteracts",
			"angerMaxExcitementForAnger",
			"angerMaxExcitementForHappiness",
			"angerExcitementFactorForAnger",
			"angerExcitementFactorForHappiness",
			"defaultHappiness",
			"defaultAnger",
			"defaultPlayfulness",
			"maxExcitedExpression",
			"expressionMinHoldTime",
			"expressionMaxHoldTime",
			"excitedExpressionWeightModifier",
			"exclusiveExpressionWeightModifier",
			"orgasmExpressionRangeMin",
			"orgasmExpressionRangeMax",
			"orgasmFirstExpressionTime",
			"orgasmSyncMinExcitement",
			"slapMinExpressionChange",
			"slapMaxExpressionChange",
			"slapMinTime",
			"slapMaxTime",
			"maxSweat",
			"maxFlush",
			"temperatureExcitementMax",
			"temperatureExcitementRate",
			"temperatureDecayRate",
			"tirednessRateDuringPostOrgasm",
			"tirednessBaseDecayRate",
			"tirednessBackToBaseRate",
			"delayAfterOrgasmUntilTirednessDecay",
			"tirednessMaxExcitementForBaseDecay",
			"orgasmBaseTirednessIncrease",
			"excitementDecayRate",
			"excitementPostOrgasm",
			"orgasmTime",
			"postOrgasmTime",
			"rateAdjustment",
			"penetrationDamper",
			"minCollisionMagnitude",
			"movementEnergyRampUpAfterOrgasmEasing",
		};

		public static string[] AllNames
		{
			get { return allNames_; }
		}

		public override string[] GetAllNames()
		{
			return AllNames;
		}
	}
}
