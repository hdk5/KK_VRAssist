using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace KoikatuVRAssistPlugin
{
	public class GripMoveAssistObj : MonoBehaviour
	{
		private Transform transViveCntroller;

		private VRHandCtrl handCtrl;

		private GameObject[] lstObjMainCanvas = new GameObject[2];

		private Vector3 prevVRCameraPos;

		private Vector3 posRotationCenter;

		private Vector3 prevControllerPos;

		private Quaternion prevControllerRot;

		private GameObject centerMarker;

		private GameObject cameraTarget;

		private GameObject[] canvasMoveMarker = new GameObject[2];

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

		/// <summary>
		/// Hold Grip for this amount of time in seconds to force display the menu
		/// </summary>
		private const float MenuDisplayTime = 0.5f;

		/// <summary>
		/// Hold Grip for this amount of time in seconds to detach the menu
		/// </summary>
		private const float MenuDetachTime = 1.5f;

		private const float speedRate = 2f;


		public static GripMoveAssistObj GetOrAddGripMoveAssistObj(VRHScene scene)
		{
			var gripMoveAssistObj = scene.gameObject.GetComponent<GripMoveAssistObj>();
			if (gripMoveAssistObj != null)
				return gripMoveAssistObj;
			else
				gripMoveAssistObj = scene.gameObject.AddComponent<GripMoveAssistObj>();

			for (int deviceIndex = 0; deviceIndex < 2; deviceIndex++)
			{
				gripMoveAssistObj.lstObjMainCanvas[deviceIndex] = Traverse.Create(scene).Field("lstObjMainCanvas").GetValue<List<GameObject>>()[deviceIndex];
				if (gripMoveAssistObj.lstObjMainCanvas[deviceIndex] == null)
				{
					Destroy(gripMoveAssistObj);
					throw new MemberNotFoundException("lstObjMainCanvas is null when attempting to initialize GripMoveAssistObj");
				}		
			}

			return gripMoveAssistObj;
		}

		public void Start()
		{
			gameObject.GetComponent<VRHScene>().managerVR.scrCamera.camera.nearClipPlane = 0.001f;
		}

		public void PerformGripMove(VRHScene scene)
		{
			if (scene.managerVR.scrControllerManager.IsPressDown(VRViveController.EViveButtonKind.Trigger, -1, out int deviceIndex))
			{
				VRViveController vRViveController = scene.managerVR.scrControllerManager.lstController[deviceIndex];
				if (vRViveController.mode != 2 && vRViveController.IsSpriteOver(vRViveController.mode))
				{
					return;
				}

				//After the trigger is pressed, initialize the fields used to track and move the positions of the camera and controller
				transViveCntroller = scene.managerVR.scrControllerManager.GetTransform(deviceIndex);
				prevVRCameraPos = scene.managerVR.objMove.transform.position;
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
				cameraTarget.transform.position = scene.managerVR.objMove.transform.position;
				cameraTarget.transform.rotation = scene.managerVR.objMove.transform.rotation;
				handCtrl = transViveCntroller.gameObject.GetComponentInChildren<VRHandCtrl>();
			}
			if (scene.managerVR.scrControllerManager.IsPressUp(VRViveController.EViveButtonKind.Trigger, -1, out deviceIndex) && transViveCntroller == scene.managerVR.scrControllerManager.GetTransform(deviceIndex))
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
				cameraTarget.transform.position = prevVRCameraPos + scene.managerVR.objMove.transform.TransformDirection(direction);
				Vector3 eulerAngles = (prevControllerRot * Quaternion.Inverse(transViveCntroller.localRotation)).eulerAngles;
				centerMarker.transform.RotateAround(posRotationCenter, Vector3.up, eulerAngles.y);
				scene.managerVR.objMove.transform.position = cameraTarget.transform.position;
				scene.managerVR.objMove.transform.rotation = cameraTarget.transform.rotation;

				prevVRCameraPos = scene.managerVR.objMove.transform.position;
				prevControllerPos = transViveCntroller.localPosition;
				prevControllerRot = transViveCntroller.localRotation;
			}
		}

		public void PerformFloatingMainMenu(VRHScene scene)
		{
			VRViveControllerManager scrControllerManager = scene.managerVR.scrControllerManager;
			float currentTime = Time.time;

			for (int deviceIndex = 0; deviceIndex < 2; deviceIndex++)
			{
				GameObject menuCanvas = lstObjMainCanvas[deviceIndex];
				VRViveController vRViveController = scrControllerManager.lstController[deviceIndex];	
				var trackedObj = vRViveController.GetComponent<SteamVR_TrackedObject>();
				if (trackedObj.index == SteamVR_TrackedObject.EIndex.None)
					continue;

				SteamVR_Controller.Device device = SteamVR_Controller.Input((int)trackedObj.index);							
				bool menuFloating = menuCanvas.transform.parent == scene.managerVR.objMove.transform;
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
				else if (scrControllerManager.IsPressUpSelectHand(VRViveController.EViveButtonKind.Grip, deviceIndex))
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
				if (device.GetPress(4uL))
				{
					//Abort if other input is detected to prevent conflict with other controller combos
					//8589934595 is the button mask for trigger, the menu and system buttons
					if (device.GetPress(8589934595ul) || device.GetAxis().sqrMagnitude > 0.25f)
					{
						gripDownTime[deviceIndex] = currentTime;
						continue;
					}	
					
					float gripHeldTime = currentTime - gripDownTime[deviceIndex];
					if (gripHeldTime > MenuDisplayTime)
					{
						//If the time since pressing down grip exceeds 0.5 seconds then make the menu visible so the use can see the menu while dragging it with the controller.
						//And if the menu is currently detached, update the menu's position to make it temporarily follow the controller's movement while remained detached.
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
					if (gripHeldTime > MenuDetachTime && !menuFloating)
					{
						menuCanvas.transform.parent = scene.managerVR.objMove.transform;
						gripDownTime[deviceIndex] = float.MaxValue;
					}
				}
			}
		}

		public void PerformScrollSpeedByTouch(VRHScene scene)
		{
			for (int deviceIndex = 0; deviceIndex < 2; deviceIndex++)
			{
				var trackedObj = scene.managerVR.scrControllerManager.lstController[deviceIndex].GetComponent<SteamVR_TrackedObject>();
				if (trackedObj.index == SteamVR_TrackedObject.EIndex.None)
					continue;

				SteamVR_Controller.Device device = SteamVR_Controller.Input((int)trackedObj.index);

				//Translate the y axis movement of the stick to the speed gauge if:
				//- Touchpad/stick is being touched, and no other buttons are pressed. This prevents conflicts with other controller combos that involve the stick's y axis. 8589934599 is the button mask for trigger, grip, and the menu and system buttons.
				//- When menu is floating, is indicated by the parent of the menu canvas.
				//- There is less than 0.5 of x axis movement, to prevent unintentionally adjusting the speed gauge while moving the x axis.
				if (device.GetTouch(4294967296uL) && !device.GetPress(8589934599ul) && lstObjMainCanvas[deviceIndex].transform.parent == scene.managerVR.objMove.transform)
				{
					Vector2 axis = device.GetAxis();
					if (-0.5f < axis.x && axis.x < 0.5f)
					{
						ProcSpeedUpClick(scene, axis.y);
					}
				}
			}
		}

		private void ProcSpeedUpClick(VRHScene scene, float value)
		{
			HFlag hFlag = scene.flags;
			if (hFlag != null)
			{
				hFlag.speedUpClac = Vector2.zero;
				hFlag.speedCalc = Mathf.Clamp01(hFlag.speedCalc + speedRate * value * Time.deltaTime);
			}
		}
	}
}
