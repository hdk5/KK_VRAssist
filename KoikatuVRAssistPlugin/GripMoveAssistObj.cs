using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KoikatuVRAssistPlugin
{
	public class GripMoveAssistObj : MonoBehaviour
	{
		private Transform transViveCntroller;

		private VRHandCtrl handCtrl;

		private Vector3 posVRCamera;

		private Vector3 posRotationCenter;

		private Vector3 posViveController;

		private Quaternion rotViveController;

		private GameObject centerMarker;

		private GameObject originTobe;

		private static FieldInfo f_action = typeof(VRHandCtrl).GetField("action", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private float[] gripDownTime = new float[2]
		{
			float.MaxValue,
			float.MaxValue
		};

		private float[] gripUpTime = new float[2]
		{
			float.MinValue,
			float.MinValue
		};

		public float floatingMenuDelta = 1.5f;

		private static FieldInfo f_lstObjMainCanvas = typeof(VRHScene).GetField("lstObjMainCanvas", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static FieldInfo f_device = typeof(VRViveController).GetField("device", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private Vector3[] detachedMainCanvasPos = new Vector3[2];

		private Quaternion[] detachedMainCanvasRot = new Quaternion[2];

		private GameObject[] canvasMoveMarker = new GameObject[2];

		private Vector3[] lastPos = new Vector3[2];

		private Quaternion[] lastRot = new Quaternion[2];

		private static FieldInfo f_lstProc = typeof(VRHScene).GetField("lstProc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static FieldInfo f_flags = typeof(HActionBase).GetField("flags", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		public static float speedRate = 2f;

		public void Start()
		{
			base.gameObject.GetComponent<VRHScene>().managerVR.scrCamera.camera.nearClipPlane = 0.001f;
		}

		public void PerformGripMove(VRHScene __instance)
		{
			int _num = 0;
			if (__instance.managerVR.scrControllerManager.IsPressDown(VRViveController.EViveButtonKind.Trigger, -1, out _num))
			{
				VRViveController vRViveController = __instance.managerVR.scrControllerManager.lstController[_num];
				if (vRViveController.mode != 2 && vRViveController.IsSpriteOver(vRViveController.mode))
				{
					return;
				}
				transViveCntroller = __instance.managerVR.scrControllerManager.GetTransform(_num);
				posVRCamera = __instance.managerVR.objMove.transform.position;
				posViveController = ((!transViveCntroller) ? Vector3.zero : transViveCntroller.localPosition);
				rotViveController = ((!transViveCntroller) ? Quaternion.identity : transViveCntroller.localRotation);
				posRotationCenter = transViveCntroller.position;
				if (!centerMarker)
				{
					centerMarker = new GameObject("__GripMove__CenterMarker");
				}
				if (!originTobe)
				{
					originTobe = new GameObject("__GripMove__OriginTobeMarker");
					originTobe.transform.parent = centerMarker.transform;
				}
				centerMarker.transform.position = transViveCntroller.position;
				originTobe.transform.position = __instance.managerVR.objMove.transform.position;
				originTobe.transform.rotation = __instance.managerVR.objMove.transform.rotation;
				handCtrl = transViveCntroller.gameObject.GetComponentInChildren<VRHandCtrl>();
			}
			if (__instance.managerVR.scrControllerManager.IsPressUp(VRViveController.EViveButtonKind.Trigger, -1, out _num) && transViveCntroller == __instance.managerVR.scrControllerManager.GetTransform(_num))
			{
				transViveCntroller = null;
				handCtrl = null;
			}
			if ((bool)transViveCntroller)
			{
				if (handCtrl != null && (VRHandCtrl.HandAction)f_action.GetValue(handCtrl) != 0)
				{
					transViveCntroller = null;
					handCtrl = null;
					return;
				}
				Vector3 direction = posViveController - transViveCntroller.localPosition;
				originTobe.transform.position = posVRCamera + __instance.managerVR.objMove.transform.TransformDirection(direction);
				Vector3 eulerAngles = (rotViveController * Quaternion.Inverse(transViveCntroller.localRotation)).eulerAngles;
				centerMarker.transform.RotateAround(posRotationCenter, Vector3.up, eulerAngles.y);
				__instance.managerVR.objMove.transform.position = originTobe.transform.position;
				__instance.managerVR.objMove.transform.rotation = originTobe.transform.rotation;
				posVRCamera = __instance.managerVR.objMove.transform.position;
				posViveController = transViveCntroller.localPosition;
				rotViveController = transViveCntroller.localRotation;
			}
		}

		public void PerformFloatingMainMenu(VRHScene __instance)
		{
			VRViveControllerManager scrControllerManager = __instance.managerVR.scrControllerManager;
			List<GameObject> list = f_lstObjMainCanvas.GetValue(__instance) as List<GameObject>;
			float time = Time.time;
			for (int i = 0; i < 2; i++)
			{
				VRViveController vRViveController = scrControllerManager.lstController[i];
				SteamVR_Controller.Device device = f_device.GetValue(vRViveController) as SteamVR_Controller.Device;
				if (device == null)
				{
					continue;
				}
				bool num = scrControllerManager.IsPressDownSelectHand(VRViveController.EViveButtonKind.Grip, i);
				bool flag = scrControllerManager.IsPressUpSelectHand(VRViveController.EViveButtonKind.Grip, i);
				bool press = device.GetPress(4uL);
				GameObject gameObject = list[i];
				bool flag2 = gameObject.transform.parent == __instance.managerVR.objMove.transform;
				if (num)
				{
					gripDownTime[i] = time;
					if (flag2)
					{
						if (canvasMoveMarker[i] == null)
						{
							canvasMoveMarker[i] = new GameObject("__MainCanvasMoveMarker__");
							canvasMoveMarker[i].transform.parent = vRViveController.transform;
						}
						canvasMoveMarker[i].transform.position = gameObject.transform.position;
						canvasMoveMarker[i].transform.rotation = gameObject.transform.rotation;
					}
				}
				if (flag)
				{
					gripDownTime[i] = float.MaxValue;
					if ((double)(time - gripUpTime[i]) < 0.5 && flag2)
					{
						detachedMainCanvasPos[i] = gameObject.transform.localPosition;
						detachedMainCanvasRot[i] = gameObject.transform.localRotation;
						gameObject.transform.parent = scrControllerManager.lstController[i].GetComponentInChildren<VRHandCtrl>().transform;
						gameObject.transform.localPosition = Vector3.zero;
						gameObject.transform.localRotation = Quaternion.identity;
					}
					gripUpTime[i] = time;
				}
				if (!press)
				{
					continue;
				}
				float num2 = time - gripDownTime[i];
				if ((double)num2 > 0.5)
				{
					if (!gameObject.activeSelf)
					{
						gameObject.SetActive(value: true);
					}
					if (flag2)
					{
						gameObject.transform.position = canvasMoveMarker[i].transform.position;
						gameObject.transform.rotation = canvasMoveMarker[i].transform.rotation;
					}
				}
				if (num2 > floatingMenuDelta && !flag2)
				{
					gameObject.transform.parent = __instance.managerVR.objMove.transform;
					gripDownTime[i] = float.MaxValue;
				}
				lastPos[i] = vRViveController.transform.localPosition;
				lastRot[i] = vRViveController.transform.localRotation;
			}
		}

		public void PerformScrollSpeedByTouch(VRHScene __instance)
		{
			VRViveControllerManager scrControllerManager = __instance.managerVR.scrControllerManager;
			List<GameObject> list = f_lstObjMainCanvas.GetValue(__instance) as List<GameObject>;
			for (int i = 0; i < 2; i++)
			{
				VRViveController obj = scrControllerManager.lstController[i];
				SteamVR_Controller.Device device = f_device.GetValue(obj) as SteamVR_Controller.Device;
				if (device != null && list[i].transform.parent == __instance.managerVR.objMove.transform && device.GetTouch(4294967296uL))
				{
					Vector2 axis = device.GetAxis();
					float num = 0.5f;
					if (0f - num < axis.x && axis.x < num)
					{
						ProcSpeedUpClick(__instance, axis.y);
					}
					else if (!(axis.x < 0f - num))
					{
						_ = axis.x;
					}
				}
			}
		}

		private void ProcSpeedUpClick(VRHScene __instance, float value)
		{
			(f_lstProc.GetValue(__instance) as List<HActionBase>).SafeProc((int)__instance.flags.mode, delegate(HActionBase proc)
			{
				HFlag hFlag = f_flags.GetValue(proc) as HFlag;
				if (hFlag != null)
				{
					hFlag.speedUpClac = Vector2.zero;
					hFlag.speedCalc = Mathf.Clamp01(hFlag.speedCalc + speedRate * value * Time.deltaTime);
				}
			});
		}
	}
}
