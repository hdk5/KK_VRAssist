using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HarmonyLib;
using System.Reflection.Emit;
using static VRViveController;

namespace KoikatuVRAssistPlugin
{
	/// <summary>
	/// Shifts VR H scene's handling of controller press-down events to occur at press-up instead, and provides methods to intercept and bypass the handling.
	/// <para>Use <c>SkipNextControllerAction()</c> to skip a specific controller and the next specific input</para>
	/// <para> Currently, only grip and menu button events can be intercepted </para>
	/// </summary>
	public static class InputOverride
	{
		public enum SkippableButtonKind
		{
			Grip = EViveButtonKind.Grip,
			Menu = EViveButtonKind.Menu
		}

		/// <summary>
		/// Stores the controller and button pairs whose next press up event will be ignored by the game
		/// </summary>
		private static HashSet<KeyValuePair<VRViveController, EViveButtonKind>> SkipNextPressUp = new HashSet<KeyValuePair<VRViveController, EViveButtonKind>>();


		/// <summary>
		/// Make the game ignore the next press up event and its associated action of the given controller and button in VR H scene
		/// <para> Currently supports skipping the following inputs and their associated actions: </para>
		/// <list type="bullet">
		/// <item><description><b> Grip - </b> Show/hide menu </description></item>
		/// <item><description><b> Menu button - </b> Switch operation mode </description></item>
		/// </list>
		/// </summary>
		public static void SkipNextControllerAction (VRViveController vRViveController, SkippableButtonKind buttonKind)
		{
			if (!Enum.IsDefined(typeof(SkippableButtonKind), buttonKind))
				throw new ArgumentException("Input not skippable");

			SkipNextPressUp.Add(new KeyValuePair<VRViveController, EViveButtonKind>(vRViveController, (EViveButtonKind) buttonKind));
		}


		[HarmonyTranspiler]
		[HarmonyPatch(typeof(VRHScene), "Update")]
		private static IEnumerable<CodeInstruction> GripDownOverrideTpl(IEnumerable<CodeInstruction> instructions)
		{
			var newInstructions = new CodeMatcher(instructions)
				.MatchForward(true,
					new CodeMatch(OpCodes.Ldc_I4_1),
					new CodeMatch(OpCodes.Ldloc_S),
					new CodeMatch(OpCodes.Ldc_I4_0),
					new CodeMatch(operand: AccessTools.Method(typeof(VRViveControllerManager), nameof(VRViveControllerManager.IsPressDownSelectHand))))
				.SetAndAdvance(OpCodes.Call, AccessTools.Method(
					typeof(InputOverride), nameof(PressUpOrSkip), new Type[] { typeof(VRViveControllerManager), typeof(EViveButtonKind), typeof(int), typeof(int) }))
				.Instructions();
#if DEBUG
			File.WriteAllLines($"{nameof(GripDownOverrideTpl)}.txt", newInstructions.Select(x => x.ToString()).ToArray());
#endif
			return newInstructions;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(VRViveController), "Update")]
		private static IEnumerable<CodeInstruction> MenuDownOverrideTpl(IEnumerable<CodeInstruction> instructions)
		{
			var newInstructions = new CodeMatcher(instructions)
				.MatchForward(true,
					new CodeMatch(OpCodes.Ldc_I4_2),
					new CodeMatch(OpCodes.Ldc_I4_M1),
					new CodeMatch(operand: AccessTools.Method(typeof(VRViveController), nameof(VRViveController.IsPressDown))))
				.SetAndAdvance(OpCodes.Call, AccessTools.Method(
					typeof(InputOverride), nameof(PressUpOrSkip), new Type[] { typeof(VRViveController), typeof(EViveButtonKind), typeof(int) }))
				.Instructions();
#if DEBUG
			File.WriteAllLines($"{nameof(MenuDownOverrideTpl)}.txt", newInstructions.Select(x => x.ToString()).ToArray());
#endif
			return newInstructions;
		}

		private static bool PressUpOrSkip(VRViveControllerManager callObject, EViveButtonKind button, int deviceIndex, int mode)
		{
			var isPressUp = callObject.IsPressUpSelectHand(button, deviceIndex, mode);
			var pairToCheck = new KeyValuePair<VRViveController, EViveButtonKind>(callObject.lstController[deviceIndex], button);

			if (isPressUp && SkipNextPressUp.Contains(pairToCheck))
			{
				SkipNextPressUp.Remove(pairToCheck);
				return false;
			}
			else
				return isPressUp;
				
		}
		private static bool PressUpOrSkip(VRViveController callObject, EViveButtonKind button, int mode)
		{
			var isPressUp = callObject.IsPressUp(button, mode);
			var pairToCheck = new KeyValuePair<VRViveController, EViveButtonKind>(callObject, button);

			if (isPressUp && SkipNextPressUp.Contains(pairToCheck))
			{
				SkipNextPressUp.Remove(pairToCheck);
				return false;
			}
			else
				return isPressUp;
		}
	}
}
