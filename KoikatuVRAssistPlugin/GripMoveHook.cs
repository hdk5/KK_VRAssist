using System;
using HarmonyLib;

namespace KoikatuVRAssistPlugin
{
	public static class GripMoveHook
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(VRHScene), "ViveCntrollerMove", new Type[] { }, null)]
		public static void ViveControllerMovePostHook(VRHScene __instance, bool __result)
		{
			if (__result)
			{
				GetGripMoveAssistObj(__instance).PerformGripMove(__instance);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(VRHScene), "Update", new Type[] { }, null)]
		public static void VRHSceneUpdatePostHook(VRHScene __instance)
		{
			if (__instance.managerVR != null && __instance.managerVR.scrControllerManager != null)
			{
				GripMoveAssistObj gripMoveAssistObj = GetGripMoveAssistObj(__instance);
				gripMoveAssistObj.PerformFloatingMainMenu(__instance);
				gripMoveAssistObj.PerformScrollSpeedByTouch(__instance);
			}
		}

		private static GripMoveAssistObj GetGripMoveAssistObj(VRHScene __instance)
		{
			GripMoveAssistObj gripMoveAssistObj = __instance.GetComponent<GripMoveAssistObj>();
			if (!gripMoveAssistObj)
			{
				gripMoveAssistObj = __instance.gameObject.AddComponent<GripMoveAssistObj>();
			}
			return gripMoveAssistObj;
		}
	}
}
