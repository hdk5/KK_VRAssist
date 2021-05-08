using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KoikatuVRAssistPlugin
{
	public class GripMoveAssistObj : MonoBehaviour
	{
		private Transform transViveCntroller;

		private VRHandCtrl handCtrl;

		private Vector3 prevVRCameraPos;

		private Vector3 posRotationCenter;

		private Vector3 prevControllerPos;

		private Quaternion prevControllerRot;

		private GameObject centerMarker;

		private GameObject cameraTarget;

		private GameObject[] canvasMoveMarker = new GameObject[2];

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

		private static FieldInfo f_action = typeof(VRHandCtrl).GetField("action", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static FieldInfo f_lstObjMainCanvas = typeof(VRHScene).GetField("lstObjMainCanvas", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static FieldInfo f_device = typeof(VRViveController).GetField("device", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static FieldInfo f_lstProc = typeof(VRHScene).GetField("lstProc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static FieldInfo f_flags = typeof(HActionBase).GetField("flags", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private const float floatingMenuDelta = 1.5f;

		private const float speedRate = 2f;

		public void Start()
		{
			gameObject.GetComponent<VRHScene>().managerVR.scrCamera.camera.nearClipPlane = 0.001f;
		}

		public void PerformGripMove(VRHScene __instance)
		{
			if (__instance.managerVR.scrControllerManager.IsPressDown(VRViveController.EViveButtonKind.Trigger, -1, out int deviceIndex))
			{
				VRViveController vRViveController = __instance.managerVR.scrControllerManager.lstController[deviceIndex];
				if (vRViveController.mode != 2 && vRViveController.IsSpriteOver(vRViveController.mode))
				{
					return;
				}

				//After the trigger is pressed, initialize the fields used to track and move the positions of the camera and controller
				transViveCntroller = __instance.managerVR.scrControllerManager.GetTransform(deviceIndex);
				prevVRCameraPos = __instance.managerVR.objMove.transform.position;
				prevControllerPos = (!transViveCntroller) ? Vector3.zero : transViveCntroller.localPosition;
				prevControllerRot = (!transViveCntroller) ? Quaternion.identity : transViveCntroller.localRotation;
				posRotationCenter = transViveCntroller.position;
				if (!centerMarker)
				{
					centerMarker = new GameObject($"__GripMove__{centerMarker}");
				}
				if (!cameraTarget)
				{
					cameraTarget = new GameObject($"__GripMove__{cameraTarget}");
					cameraTarget.transform.parent = centerMarker.transform;
				}

				centerMarker.transform.position = transViveCntroller.position;
				cameraTarget.transform.position = __instance.managerVR.objMove.transform.position;
				cameraTarget.transform.rotation = __instance.managerVR.objMove.transform.rotation;
				handCtrl = transViveCntroller.gameObject.GetComponentInChildren<VRHandCtrl>();
			}
			if (__instance.managerVR.scrControllerManager.IsPressUp(VRViveController.EViveButtonKind.Trigger, -1, out deviceIndex) && transViveCntroller == __instance.managerVR.scrControllerManager.GetTransform(deviceIndex))
			{
				transViveCntroller = null;
				handCtrl = null;
			}

			//While trigger is being held, update the position and rotation of the camera according to the movement of the controller
			if ((bool)transViveCntroller)
			{
				//Abort grip move if the controller is being used to interact with the character
				if (handCtrl != null && (VRHandCtrl.HandAction)f_action.GetValue(handCtrl) != 0)
				{
					transViveCntroller = null;
					handCtrl = null;
					return;
				}

				Vector3 direction = prevControllerPos - transViveCntroller.localPosition;
				cameraTarget.transform.position = prevVRCameraPos + __instance.managerVR.objMove.transform.TransformDirection(direction);
				Vector3 eulerAngles = (prevControllerRot * Quaternion.Inverse(transViveCntroller.localRotation)).eulerAngles;
				centerMarker.transform.RotateAround(posRotationCenter, Vector3.up, eulerAngles.y);
				__instance.managerVR.objMove.transform.position = cameraTarget.transform.position;
				__instance.managerVR.objMove.transform.rotation = cameraTarget.transform.rotation;

				prevVRCameraPos = __instance.managerVR.objMove.transform.position;
				prevControllerPos = transViveCntroller.localPosition;
				prevControllerRot = transViveCntroller.localRotation;
			}
		}

		public void PerformFloatingMainMenu(VRHScene __instance)
		{
			VRViveControllerManager scrControllerManager = __instance.managerVR.scrControllerManager;
			float currentTime = Time.time;

			for (int deviceIndex = 0; deviceIndex < 2; deviceIndex++)
			{
				VRViveController vRViveController = scrControllerManager.lstController[deviceIndex];
				SteamVR_Controller.Device device = f_device.GetValue(vRViveController) as SteamVR_Controller.Device;
				if (device == null)
				{
					continue;
				}

				GameObject menuCanvas = (f_lstObjMainCanvas.GetValue(__instance) as List<GameObject>)[deviceIndex];
				bool menuFloating = menuCanvas.transform.parent == __instance.managerVR.objMove.transform;

				//When grip is pressed, prepare a GameObject (canvasMoveMarker) to follow the controller, which can then be used later to update the position of the menu
				if (scrControllerManager.IsPressDownSelectHand(VRViveController.EViveButtonKind.Grip, deviceIndex))
				{
					gripDownTime[deviceIndex] = currentTime;
					if (menuFloating)
					{
						if (canvasMoveMarker[deviceIndex] == null)
						{
							canvasMoveMarker[deviceIndex] = new GameObject("__MainCanvasMoveMarker__");
							canvasMoveMarker[deviceIndex].transform.parent = vRViveController.transform;
						}
						canvasMoveMarker[deviceIndex].transform.position = menuCanvas.transform.position;
						canvasMoveMarker[deviceIndex].transform.rotation = menuCanvas.transform.rotation;
					}
				}
				//When grip is released, reset "gripDownTime" to a default value such that all calculation for the amount of time grip is held returns negative.
				//And if the time since the last release is less than 0.5 seconds, indicating a double click, attach the menu back to the controller to end floating
				if (scrControllerManager.IsPressUpSelectHand(VRViveController.EViveButtonKind.Grip, deviceIndex))
				{
					gripDownTime[deviceIndex] = float.MaxValue;

					if ((currentTime - gripUpTime[deviceIndex]) < 0.5f && menuFloating)
					{
						menuCanvas.transform.parent = scrControllerManager.lstController[deviceIndex].GetComponentInChildren<VRHandCtrl>().transform;
						menuCanvas.transform.localPosition = Vector3.zero;
						menuCanvas.transform.localRotation = Quaternion.identity;
					}
					gripUpTime[deviceIndex] = currentTime;
				}

				//While grip is being held
				if (!device.GetPress(4uL))
				{
					continue;
				}
				float gripHeldTime = currentTime - gripDownTime[deviceIndex];
				if (gripHeldTime > 0.5f)
				{
					//If the time since pressing down grip exceeds 0.5 seconds then make the menu visible so the use can see the menu while dragging it with the controller.
					//If the menu is currently detached, update the menu's position to make it temporarily follow the controller's movement while remained detached.				
					if (!menuCanvas.activeSelf)
					{
						menuCanvas.SetActive(value: true);
					}
					
					if (menuFloating)
					{
						menuCanvas.transform.position = canvasMoveMarker[deviceIndex].transform.position;
						menuCanvas.transform.rotation = canvasMoveMarker[deviceIndex].transform.rotation;
					}
				}
				//If the menu is currently not detached and the time grip is held exceeds the defined threshold, detach the menu from the controller and attach it to the camera to make it floating.
				if (gripHeldTime > floatingMenuDelta && !menuFloating)
				{
					menuCanvas.transform.parent = __instance.managerVR.objMove.transform;
					gripDownTime[deviceIndex] = float.MaxValue;
				}
			}
		}

		public void PerformScrollSpeedByTouch(VRHScene __instance)
		{
			VRViveControllerManager scrControllerManager = __instance.managerVR.scrControllerManager;
			List<GameObject> menuCanvasList = f_lstObjMainCanvas.GetValue(__instance) as List<GameObject>;
			for (int i = 0; i < 2; i++)
			{
				VRViveController controller = scrControllerManager.lstController[i];
				SteamVR_Controller.Device device = f_device.GetValue(controller) as SteamVR_Controller.Device;

				//When menu is floating and the touchpad/stick is being touched, translate the y axis movement of the stick to the speed gauge only if there is less than 0.5 of x axis movement to prevent unintentionally adjusting the speed gauge while moving the x axis.
				if (device != null && menuCanvasList[i].transform.parent == __instance.managerVR.objMove.transform && device.GetTouch(4294967296uL))
				{
					Vector2 axis = device.GetAxis();
					if (-0.5f < axis.x && axis.x < 0.5f)
					{
						ProcSpeedUpClick(__instance, axis.y);
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
