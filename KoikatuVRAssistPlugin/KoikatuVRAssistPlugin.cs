using BepInEx;
using UnityEngine;

namespace KoikatuVRAssistPlugin
{
	[BepInPlugin("B0EAC71B-76A9-4D0E-A26F-CB3FB853D78A", "KoikatuVRAssistPlugin", "1.1.0")]
	public class KoikatuVRAssistPlugin : BaseUnityPlugin
	{
		public KoikatuVRAssistPlugin()
		{
			if (Application.dataPath.EndsWith("KoikatuVR_Data"))
			{
				GripMoveHook.InstallHook();
			}
			else
			{
				BepInLogger.Log("Not KoikatuVR. Shutdown KoikatuVRAssistPlugin.");
			}
		}
	}
}
