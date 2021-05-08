using BepInEx;
using HarmonyLib;
using System;

namespace KoikatuVRAssistPlugin
{
	[BepInPlugin(GUID, PluginName, Version)]
	public class KoikatuVRAssistPlugin : BaseUnityPlugin
	{
		public const string GUID = "KK_KoikatuVRAssistPlugin";
		public const string Version = "1.1.0";
		public const string PluginName = "KoikatuVRAssistPlugin";

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
