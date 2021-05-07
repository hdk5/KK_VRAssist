using BepInEx;
using HarmonyLib;
using System;

namespace KoikatuVRAssistPlugin
{
	[BepInPlugin("B0EAC71B-76A9-4D0E-A26F-CB3FB853D78A", "KoikatuVRAssistPlugin", "1.1.0")]
	public class KoikatuVRAssistPlugin : BaseUnityPlugin
	{
		public KoikatuVRAssistPlugin()
		{
			if (Type.GetType("VRHScene, Assembly-CSharp") != null)
			{
				Logger.LogMessage("Installs Hook");
				Harmony.CreateAndPatchAll(typeof(GripMoveHook), "KoikatuVRAssistPlugin.GripMoveHook");
			}
			else
			{
				Logger.LogMessage("Not KoikatuVR. Shutdown KoikatuVRAssistPlugin.");
			}
		}
	}
}
