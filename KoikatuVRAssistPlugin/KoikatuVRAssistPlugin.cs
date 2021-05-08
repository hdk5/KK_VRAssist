using BepInEx;
using HarmonyLib;
using System;

namespace KoikatuVRAssistPlugin
{
	[BepInPlugin(GUID, PluginName, Version)]
	[BepInProcess("KoikatuVR")]
	[BepInProcess("Koikatsu Party VR")]
	public class KoikatuVRAssistPlugin : BaseUnityPlugin
	{
		public const string GUID = "KK_KoikatuVRAssistPlugin";
		public const string Version = "1.1.0";
		public const string PluginName = "KoikatuVRAssistPlugin";

		private void Awake()
		{
			if (Type.GetType("VRHScene, Assembly-CSharp") != null)
			{
				var harmony = new Harmony(GUID);
				try
				{
					harmony.PatchAll(typeof(GripMoveHook));
				}
				catch (Exception)
				{
					harmony.UnpatchAll(harmony.Id);
					Logger.LogError("Harmony patch failed, Nothing Patched.");
					throw;
				}		
#if DEBUG
				Logger.LogDebug("Hooks Patched");
#endif
			}
			else
			{
				Logger.LogError("VRHScene not found. Nothing Patched.");
			}
		}
	}
}
