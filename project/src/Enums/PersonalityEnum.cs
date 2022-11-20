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
		public static readonly DurationIndex SlapUpdateInterval = new DurationIndex(7);

		public const int DurationCount = 8;
		public override int GetDurationCount() { return 8; }

		// bools
		public static readonly BoolIndex GazeSaccade = new BoolIndex(0);
		public static readonly BoolIndex GazeBlink = new BoolIndex(1);
		public static readonly BoolIndex LookAboveUseGazeEnergy = new BoolIndex(2);
		public static readonly BoolIndex ZappedEnabled = new BoolIndex(3);

		public const int BoolCount = 4;
		public override int GetBoolCount() { return 4; }

		// floats
		public static readonly FloatIndex GazeSaccadeMovementRange = new FloatIndex(0);
		public static readonly FloatIndex GazeEyeTargetMovementSpeed = new FloatIndex(1);
		public static readonly FloatIndex AvoidGazePlayerMinExcitement = new FloatIndex(2);
		public static readonly FloatIndex AvoidGazePlayerMaxExcitement = new FloatIndex(3);
		public static readonly FloatIndex AvoidGazePlayerInsidePersonalSpaceMinExcitement = new FloatIndex(4);
		public static readonly FloatIndex AvoidGazePlayerInsidePersonalSpaceMaxExcitement = new FloatIndex(5);
		public static readonly FloatIndex AvoidGazePlayerDuringSexMinExcitement = new FloatIndex(6);
		public static readonly FloatIndex AvoidGazePlayerDuringSexMaxExcitement = new FloatIndex(7);
		public static readonly FloatIndex AvoidGazePlayerDelayAfterOrgasm = new FloatIndex(8);
		public static readonly FloatIndex AvoidGazePlayerWeight = new FloatIndex(9);
		public static readonly FloatIndex AvoidGazeOthersMinExcitement = new FloatIndex(10);
		public static readonly FloatIndex AvoidGazeOthersMaxExcitement = new FloatIndex(11);
		public static readonly FloatIndex AvoidGazeOthersInsidePersonalSpaceMinExcitement = new FloatIndex(12);
		public static readonly FloatIndex AvoidGazeOthersInsidePersonalSpaceMaxExcitement = new FloatIndex(13);
		public static readonly FloatIndex AvoidGazeOthersDuringSexMinExcitement = new FloatIndex(14);
		public static readonly FloatIndex AvoidGazeOthersDuringSexMaxExcitement = new FloatIndex(15);
		public static readonly FloatIndex AvoidGazeOthersDelayAfterOrgasm = new FloatIndex(16);
		public static readonly FloatIndex AvoidGazeOthersWeight = new FloatIndex(17);
		public static readonly FloatIndex AvoidGazeUninvolvedHavingSexMinExcitement = new FloatIndex(18);
		public static readonly FloatIndex AvoidGazeUninvolvedHavingSexMaxExcitement = new FloatIndex(19);
		public static readonly FloatIndex LookAboveMaxWeight = new FloatIndex(20);
		public static readonly FloatIndex LookAboveMaxWeightOrgasm = new FloatIndex(21);
		public static readonly FloatIndex LookAboveMinExcitement = new FloatIndex(22);
		public static readonly FloatIndex LookAboveMinPhysicalRate = new FloatIndex(23);
		public static readonly FloatIndex LookFrontWeight = new FloatIndex(24);
		public static readonly FloatIndex LookDownWeight = new FloatIndex(25);
		public static readonly FloatIndex IdleNaturalRandomWeight = new FloatIndex(26);
		public static readonly FloatIndex IdleEmptyRandomWeight = new FloatIndex(27);
		public static readonly FloatIndex NaturalRandomWeight = new FloatIndex(28);
		public static readonly FloatIndex NaturalOtherEyesWeight = new FloatIndex(29);
		public static readonly FloatIndex BusyOtherEyesWeight = new FloatIndex(30);
		public static readonly FloatIndex NaturalPlayerEyesWeight = new FloatIndex(31);
		public static readonly FloatIndex BusyPlayerEyesWeight = new FloatIndex(32);
		public static readonly FloatIndex MaxTirednessForRandomGaze = new FloatIndex(33);
		public static readonly FloatIndex OtherEyesExcitementWeight = new FloatIndex(34);
		public static readonly FloatIndex OtherEyesOrgasmWeight = new FloatIndex(35);
		public static readonly FloatIndex BlowjobEyesWeight = new FloatIndex(36);
		public static readonly FloatIndex BlowjobGenitalsWeight = new FloatIndex(37);
		public static readonly FloatIndex HandjobEyesWeight = new FloatIndex(38);
		public static readonly FloatIndex HandjobGenitalsWeight = new FloatIndex(39);
		public static readonly FloatIndex PenetratedEyesWeight = new FloatIndex(40);
		public static readonly FloatIndex PenetratedGenitalsWeight = new FloatIndex(41);
		public static readonly FloatIndex PenetratingEyesWeight = new FloatIndex(42);
		public static readonly FloatIndex PenetratingGenitalsWeight = new FloatIndex(43);
		public static readonly FloatIndex GropedEyesWeight = new FloatIndex(44);
		public static readonly FloatIndex GropedTargetWeight = new FloatIndex(45);
		public static readonly FloatIndex GropingEyesWeight = new FloatIndex(46);
		public static readonly FloatIndex GropingTargetWeight = new FloatIndex(47);
		public static readonly FloatIndex OtherBlowjobEyesWeight = new FloatIndex(48);
		public static readonly FloatIndex OtherBlowjobTargetEyesWeight = new FloatIndex(49);
		public static readonly FloatIndex OtherBlowjobTargetGenitalsWeight = new FloatIndex(50);
		public static readonly FloatIndex OtherHandjobEyesWeight = new FloatIndex(51);
		public static readonly FloatIndex OtherHandjobTargetEyesWeight = new FloatIndex(52);
		public static readonly FloatIndex OtherHandjobTargetGenitalsWeight = new FloatIndex(53);
		public static readonly FloatIndex OtherPenetrationEyesWeight = new FloatIndex(54);
		public static readonly FloatIndex OtherPenetrationSourceEyesWeight = new FloatIndex(55);
		public static readonly FloatIndex OtherPenetrationSourceGenitalsWeight = new FloatIndex(56);
		public static readonly FloatIndex OtherGropedEyesWeight = new FloatIndex(57);
		public static readonly FloatIndex OtherGropedSourceEyesWeight = new FloatIndex(58);
		public static readonly FloatIndex OtherGropedTargetWeight = new FloatIndex(59);
		public static readonly FloatIndex ZappedTentativeTime = new FloatIndex(60);
		public static readonly FloatIndex ZappedCooldown = new FloatIndex(61);
		public static readonly FloatIndex ZappedTime = new FloatIndex(62);
		public static readonly FloatIndex ZappedByPlayerBreastsEyesWeight = new FloatIndex(63);
		public static readonly FloatIndex ZappedByPlayerBreastsTargetWeight = new FloatIndex(64);
		public static readonly FloatIndex ZappedByPlayerBreastsExcitement = new FloatIndex(65);
		public static readonly FloatIndex ZappedByPlayerGenitalsEyesWeight = new FloatIndex(66);
		public static readonly FloatIndex ZappedByPlayerGenitalsTargetWeight = new FloatIndex(67);
		public static readonly FloatIndex ZappedByPlayerGenitalsExcitement = new FloatIndex(68);
		public static readonly FloatIndex ZappedByPlayerPenetrationEyesWeight = new FloatIndex(69);
		public static readonly FloatIndex ZappedByPlayerPenetrationTargetWeight = new FloatIndex(70);
		public static readonly FloatIndex ZappedByPlayerPenetrationExcitement = new FloatIndex(71);
		public static readonly FloatIndex ZappedByPlayerMouthEyesWeight = new FloatIndex(72);
		public static readonly FloatIndex ZappedByPlayerMouthExcitement = new FloatIndex(73);
		public static readonly FloatIndex ZappedByPlayerLookUpWeight = new FloatIndex(74);
		public static readonly FloatIndex ZappedByOtherBreastsEyesWeight = new FloatIndex(75);
		public static readonly FloatIndex ZappedByOtherBreastsTargetWeight = new FloatIndex(76);
		public static readonly FloatIndex ZappedByOtherBreastsExcitement = new FloatIndex(77);
		public static readonly FloatIndex ZappedByOtherGenitalsEyesWeight = new FloatIndex(78);
		public static readonly FloatIndex ZappedByOtherGenitalsTargetWeight = new FloatIndex(79);
		public static readonly FloatIndex ZappedByOtherGenitalsExcitement = new FloatIndex(80);
		public static readonly FloatIndex ZappedByOtherPenetrationEyesWeight = new FloatIndex(81);
		public static readonly FloatIndex ZappedByOtherPenetrationTargetWeight = new FloatIndex(82);
		public static readonly FloatIndex ZappedByOtherPenetrationExcitement = new FloatIndex(83);
		public static readonly FloatIndex ZappedByOtherMouthEyesWeight = new FloatIndex(84);
		public static readonly FloatIndex ZappedByOtherMouthExcitement = new FloatIndex(85);
		public static readonly FloatIndex ZappedByOtherLookUpWeight = new FloatIndex(86);
		public static readonly FloatIndex OtherZappedEyesWeight = new FloatIndex(87);
		public static readonly FloatIndex OtherZappedTargetWeight = new FloatIndex(88);
		public static readonly FloatIndex OtherZappedSourceWeight = new FloatIndex(89);
		public static readonly FloatIndex LookAtPlayerOnGrabWeight = new FloatIndex(90);
		public static readonly FloatIndex LookAtPlayerTimeAfterGrab = new FloatIndex(91);
		public static readonly FloatIndex KissSpeedEnergyFactor = new FloatIndex(92);
		public static readonly FloatIndex IdleMaxExcitement = new FloatIndex(93);
		public static readonly FloatIndex TirednessExcitementRateFactor = new FloatIndex(94);
		public static readonly FloatIndex GazeEnergyTirednessFactor = new FloatIndex(95);
		public static readonly FloatIndex GazeTirednessFactor = new FloatIndex(96);
		public static readonly FloatIndex MovementEnergyTirednessFactor = new FloatIndex(97);
		public static readonly FloatIndex ExpressionTirednessFactor = new FloatIndex(98);
		public static readonly FloatIndex MovementEnergyRampUpDelayAfterOrgasm = new FloatIndex(99);
		public static readonly FloatIndex MovementEnergyRampUpAfterOrgasm = new FloatIndex(100);
		public static readonly FloatIndex AvoidGazeAnger = new FloatIndex(101);
		public static readonly FloatIndex AngerWhenPlayerInteracts = new FloatIndex(102);
		public static readonly FloatIndex AngerMaxExcitementForAnger = new FloatIndex(103);
		public static readonly FloatIndex AngerMaxExcitementForHappiness = new FloatIndex(104);
		public static readonly FloatIndex AngerExcitementFactorForAnger = new FloatIndex(105);
		public static readonly FloatIndex AngerExcitementFactorForHappiness = new FloatIndex(106);
		public static readonly FloatIndex DefaultHappiness = new FloatIndex(107);
		public static readonly FloatIndex DefaultAnger = new FloatIndex(108);
		public static readonly FloatIndex DefaultPlayfulness = new FloatIndex(109);
		public static readonly FloatIndex DefaultSurprise = new FloatIndex(110);
		public static readonly FloatIndex MaxHappyExpression = new FloatIndex(111);
		public static readonly FloatIndex MaxExcitedExpression = new FloatIndex(112);
		public static readonly FloatIndex MaxPlayfulExpression = new FloatIndex(113);
		public static readonly FloatIndex MaxAngryExpression = new FloatIndex(114);
		public static readonly FloatIndex MaxSurprisedExpression = new FloatIndex(115);
		public static readonly FloatIndex MaxTiredExpression = new FloatIndex(116);
		public static readonly FloatIndex MinTiredness = new FloatIndex(117);
		public static readonly FloatIndex MinHappyChocked = new FloatIndex(118);
		public static readonly FloatIndex MaxHappyChocked = new FloatIndex(119);
		public static readonly FloatIndex MinPlayfulChocked = new FloatIndex(120);
		public static readonly FloatIndex MaxPlayfulChocked = new FloatIndex(121);
		public static readonly FloatIndex MinExcitedChocked = new FloatIndex(122);
		public static readonly FloatIndex MaxExcitedChocked = new FloatIndex(123);
		public static readonly FloatIndex MinAngryChocked = new FloatIndex(124);
		public static readonly FloatIndex MaxAngryChocked = new FloatIndex(125);
		public static readonly FloatIndex MinSurprisedChocked = new FloatIndex(126);
		public static readonly FloatIndex MaxSurprisedChocked = new FloatIndex(127);
		public static readonly FloatIndex MinTiredChocked = new FloatIndex(128);
		public static readonly FloatIndex MaxTiredChocked = new FloatIndex(129);
		public static readonly FloatIndex ExpressionMinHoldTime = new FloatIndex(130);
		public static readonly FloatIndex ExpressionMaxHoldTime = new FloatIndex(131);
		public static readonly FloatIndex ExcitedExpressionWeightModifier = new FloatIndex(132);
		public static readonly FloatIndex ExclusiveExpressionWeightModifier = new FloatIndex(133);
		public static readonly FloatIndex OrgasmExpressionRangeMin = new FloatIndex(134);
		public static readonly FloatIndex OrgasmExpressionRangeMax = new FloatIndex(135);
		public static readonly FloatIndex OrgasmFirstExpressionTime = new FloatIndex(136);
		public static readonly FloatIndex OrgasmSyncMinExcitement = new FloatIndex(137);
		public static readonly FloatIndex SlapMinExpressionChange = new FloatIndex(138);
		public static readonly FloatIndex SlapMaxExpressionChange = new FloatIndex(139);
		public static readonly FloatIndex SlapMinTime = new FloatIndex(140);
		public static readonly FloatIndex SlapMaxTime = new FloatIndex(141);
		public static readonly FloatIndex FlushRedMag = new FloatIndex(142);
		public static readonly FloatIndex MaxSweat = new FloatIndex(143);
		public static readonly FloatIndex MaxFlush = new FloatIndex(144);
		public static readonly FloatIndex MaxChokedFlush = new FloatIndex(145);
		public static readonly FloatIndex ChokedAirDownTime = new FloatIndex(146);
		public static readonly FloatIndex ChokedAirUpTime = new FloatIndex(147);
		public static readonly FloatIndex TemperatureExcitementMax = new FloatIndex(148);
		public static readonly FloatIndex TemperatureExcitementRate = new FloatIndex(149);
		public static readonly FloatIndex TemperatureDecayRate = new FloatIndex(150);
		public static readonly FloatIndex TirednessRateDuringPostOrgasm = new FloatIndex(151);
		public static readonly FloatIndex TirednessBaseDecayRate = new FloatIndex(152);
		public static readonly FloatIndex TirednessBackToBaseRate = new FloatIndex(153);
		public static readonly FloatIndex DelayAfterOrgasmUntilTirednessDecay = new FloatIndex(154);
		public static readonly FloatIndex TirednessMaxExcitementForBaseDecay = new FloatIndex(155);
		public static readonly FloatIndex OrgasmBaseTirednessIncrease = new FloatIndex(156);
		public static readonly FloatIndex ExcitementDecayRate = new FloatIndex(157);
		public static readonly FloatIndex ExcitementPostOrgasm = new FloatIndex(158);
		public static readonly FloatIndex OrgasmTime = new FloatIndex(159);
		public static readonly FloatIndex PostOrgasmTime = new FloatIndex(160);
		public static readonly FloatIndex RateAdjustment = new FloatIndex(161);
		public static readonly FloatIndex PenetrationDamper = new FloatIndex(162);
		public static readonly FloatIndex MinCollisionMag = new FloatIndex(163);
		public static readonly FloatIndex MinCollisionMagPenetration = new FloatIndex(164);
		public static readonly FloatIndex FinishOrgasmMinExcitement = new FloatIndex(165);
		public static readonly FloatIndex FinishMoodHappy = new FloatIndex(166);
		public static readonly FloatIndex FinishMoodPlayful = new FloatIndex(167);
		public static readonly FloatIndex FinishMoodAngry = new FloatIndex(168);
		public static readonly FloatIndex FinishMoodSurprised = new FloatIndex(169);
		public static readonly FloatIndex FinishMoodTired = new FloatIndex(170);

		public const int FloatCount = 171;
		public override int GetFloatCount() { return 171; }

		// strings
		public static readonly StringIndex MovementEnergyRampUpAfterOrgasmEasing = new StringIndex(0);
		public static readonly StringIndex IdleAnimation = new StringIndex(1);
		public static readonly StringIndex FinishLookAtPlayer = new StringIndex(2);
		public static readonly StringIndex FinishLookAtPlayerAction = new StringIndex(3);
		public static readonly StringIndex FinishOrgasm = new StringIndex(4);
		public static readonly StringIndex FinishMood = new StringIndex(5);

		public const int StringCount = 6;
		public override int GetStringCount() { return 6; }




		private static string[] durationNames_ = new string[]
		{
			"gazeDuration",
			"gazeRandomInterval",
			"emergencyGazeDuration",
			"gazeSaccadeInterval",
			"zappedByPlayerGazeDuration",
			"zappedByOtherGazeDuration",
			"otherZappedGazeDuration",
			"slapUpdateInterval",
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
			"gazeBlink",
			"lookAboveUseGazeEnergy",
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
			"gazeEyeTargetMovementSpeed",
			"avoidGazePlayerMinExcitement",
			"avoidGazePlayerMaxExcitement",
			"avoidGazePlayerInsidePersonalSpaceMinExcitement",
			"avoidGazePlayerInsidePersonalSpaceMaxExcitement",
			"avoidGazePlayerDuringSexMinExcitement",
			"avoidGazePlayerDuringSexMaxExcitement",
			"avoidGazePlayerDelayAfterOrgasm",
			"avoidGazePlayerWeight",
			"avoidGazeOthersMinExcitement",
			"avoidGazeOthersMaxExcitement",
			"avoidGazeOthersInsidePersonalSpaceMinExcitement",
			"avoidGazeOthersInsidePersonalSpaceMaxExcitement",
			"avoidGazeOthersDuringSexMinExcitement",
			"avoidGazeOthersDuringSexMaxExcitement",
			"avoidGazeOthersDelayAfterOrgasm",
			"avoidGazeOthersWeight",
			"avoidGazeUninvolvedHavingSexMinExcitement",
			"avoidGazeUninvolvedHavingSexMaxExcitement",
			"lookAboveMaxWeight",
			"lookAboveMaxWeightOrgasm",
			"lookAboveMinExcitement",
			"lookAboveMinPhysicalRate",
			"lookFrontWeight",
			"lookDownWeight",
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
			"defaultSurprise",
			"maxHappyExpression",
			"maxExcitedExpression",
			"maxPlayfulExpression",
			"maxAngryExpression",
			"maxSurprisedExpression",
			"maxTiredExpression",
			"minTiredness",
			"minHappyChocked",
			"maxHappyChocked",
			"minPlayfulChocked",
			"maxPlayfulChocked",
			"minExcitedChocked",
			"maxExcitedChocked",
			"minAngryChocked",
			"maxAngryChocked",
			"minSurprisedChocked",
			"maxSurprisedChocked",
			"minTiredChocked",
			"maxTiredChocked",
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
			"flushRedMag",
			"maxSweat",
			"maxFlush",
			"maxChokedFlush",
			"chokedAirDownTime",
			"chokedAirUpTime",
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
			"minCollisionMag",
			"minCollisionMagPenetration",
			"finishOrgasmMinExcitement",
			"finishMoodHappy",
			"finishMoodPlayful",
			"finishMoodAngry",
			"finishMoodSurprised",
			"finishMoodTired",
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
			"idleAnimation",
			"finishLookAtPlayer",
			"finishLookAtPlayerAction",
			"finishOrgasm",
			"finishMood",
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
			"slapUpdateInterval",
			"gazeSaccade",
			"gazeBlink",
			"lookAboveUseGazeEnergy",
			"zappedEnabled",
			"gazeSaccadeMovementRange",
			"gazeEyeTargetMovementSpeed",
			"avoidGazePlayerMinExcitement",
			"avoidGazePlayerMaxExcitement",
			"avoidGazePlayerInsidePersonalSpaceMinExcitement",
			"avoidGazePlayerInsidePersonalSpaceMaxExcitement",
			"avoidGazePlayerDuringSexMinExcitement",
			"avoidGazePlayerDuringSexMaxExcitement",
			"avoidGazePlayerDelayAfterOrgasm",
			"avoidGazePlayerWeight",
			"avoidGazeOthersMinExcitement",
			"avoidGazeOthersMaxExcitement",
			"avoidGazeOthersInsidePersonalSpaceMinExcitement",
			"avoidGazeOthersInsidePersonalSpaceMaxExcitement",
			"avoidGazeOthersDuringSexMinExcitement",
			"avoidGazeOthersDuringSexMaxExcitement",
			"avoidGazeOthersDelayAfterOrgasm",
			"avoidGazeOthersWeight",
			"avoidGazeUninvolvedHavingSexMinExcitement",
			"avoidGazeUninvolvedHavingSexMaxExcitement",
			"lookAboveMaxWeight",
			"lookAboveMaxWeightOrgasm",
			"lookAboveMinExcitement",
			"lookAboveMinPhysicalRate",
			"lookFrontWeight",
			"lookDownWeight",
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
			"defaultSurprise",
			"maxHappyExpression",
			"maxExcitedExpression",
			"maxPlayfulExpression",
			"maxAngryExpression",
			"maxSurprisedExpression",
			"maxTiredExpression",
			"minTiredness",
			"minHappyChocked",
			"maxHappyChocked",
			"minPlayfulChocked",
			"maxPlayfulChocked",
			"minExcitedChocked",
			"maxExcitedChocked",
			"minAngryChocked",
			"maxAngryChocked",
			"minSurprisedChocked",
			"maxSurprisedChocked",
			"minTiredChocked",
			"maxTiredChocked",
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
			"flushRedMag",
			"maxSweat",
			"maxFlush",
			"maxChokedFlush",
			"chokedAirDownTime",
			"chokedAirUpTime",
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
			"minCollisionMag",
			"minCollisionMagPenetration",
			"finishOrgasmMinExcitement",
			"finishMoodHappy",
			"finishMoodPlayful",
			"finishMoodAngry",
			"finishMoodSurprised",
			"finishMoodTired",
			"movementEnergyRampUpAfterOrgasmEasing",
			"idleAnimation",
			"finishLookAtPlayer",
			"finishLookAtPlayerAction",
			"finishOrgasm",
			"finishMood",
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
