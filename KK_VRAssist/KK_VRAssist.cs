using BepInEx;
using HarmonyLib;
using System;

namespace KK_VRAssist
{
	[BepInPlugin(GUID, PluginName, Version)]
	[BepInProcess("KoikatuVR")]
	[BepInProcess("Koikatsu Party VR")]
	public class KK_VRAssist : BaseUnityPlugin
	{
		public const string GUID = "KK_VRAssist";
		public const string PluginName = "KK_VRAssist";
		public const string Version = "1.2.0";

		private void Awake()
		{
			if (Type.GetType("VRHScene, Assembly-CSharp") != null)
			{
				var harmony = new Harmony(GUID);
				try
				{
					harmony.PatchAll(typeof(GripMoveHook));
					harmony.PatchAll(typeof(InputOverride));
				}
				catch (Exception)
				{
					harmony.UnpatchSelf();
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
