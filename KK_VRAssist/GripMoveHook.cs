using HarmonyLib;
using UnityEngine;

namespace KK_VRAssist
{
	public static class GripMoveHook
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(VRHScene), "ViveCntrollerMove")]
		public static void PerformGripMoveHook(VRHScene __instance, bool __result)
		{
			if (__result)
				GripMoveAssistObj.GetOrAddGripMoveAssistObj(__instance).PerformGripMove(__instance);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(VRHScene), "Update")]
		public static void PerformFloatingMenuHook(VRHScene __instance) => GripMoveAssistObj.GetOrAddGripMoveAssistObj(__instance).PerformFloatingMainMenu(__instance);

		[HarmonyPostfix]
		[HarmonyPatch(typeof(VRHScene), "Update")]
		public static void PerformScrollHook(VRHScene __instance) => GripMoveAssistObj.GetOrAddGripMoveAssistObj(__instance).PerformScrollSpeedByTouch(__instance);
	}
}
