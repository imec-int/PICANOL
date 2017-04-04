//-----------------------------------------------------------------------
// <copyright file="PointToPointGUIController.cs" company="Google">
//
// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using Tango;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading;


/// <summary>
/// GUI controller to show distance data.
/// </summary>
public class MultiPointToPointGUIController : MonoBehaviour, ITangoDepth
{
	// Constant values for overlay.
	public const float UI_LABEL_START_X = 15.0f;
	public const float UI_LABEL_START_Y = 300.0f;
	public const float UI_LABEL_SIZE_X = 1920.0f;
	public const float UI_LABEL_SIZE_Y = 100.0f;

	/// <summary>
	/// The point cloud object in the scene.
	/// </summary>
	public TangoPointCloud m_pointCloud;

	/// <summary>
	/// The scene's Tango application.
	/// </summary>
	private TangoApplication m_tangoApplication;

	public CallApp call;

	/// <summary>
	/// helper functions for multipoint
	/// </summary>
	public MultiPointHelp m_help;

	/// <summary>
	/// If set, then the depth camera is on and we are waiting for the next
	/// depth update.
	/// </summary>
	private bool m_waitingForDepth;

	private Rect buttonRect, buttonRect2, buttonRect3, screenOverlay;

	private string Path_Name;
	private int pointIndex;

	/// <summary>
	/// The margin for the grid when checking if tapped nearby a certain point.
	/// </summary>
	private const int margin = 10;
	/// <summary>
	/// Start this instance.
	/// </summary>
	private string text;
	//	private bool circle;

	public Texture2D[] cycleButtons;
	public Texture2D screenGrab;
	public Texture2D[] toggleGrid;
	public Texture2D clearButton;
	private Int16 grid;
	public Int16 draw_mode;
	/// <summary>
	/// Debug console to be able to see the unity log on every platform
	/// </summary>
	public bool uDebugConsole = false;
	/// <summary>
	/// Output message list to show incoming and sent messages + output messages of the
	/// system itself.
	/// </summary>
	public MessageList uOutput;

	/// <summary>
	/// The marker prefab to place on taps.
	/// </summary>
	public GameObject m_prefabMarker;

	public bool clearSignal = false;
	public bool MouseUp = true;
	public bool prevMousUp = true;
	private bool once = false;
	public string[] AddInfoMarker;
	private GUIStyle style;

	/// <summary>
	/// If set, this is the selected marker.
	/// </summary>
	private ARMarker m_selectedMarker;
	private bool touchedMarker = false;

//	public GUIText DisplayText;

	public void Start ()
	{
		// Make sure the android phone doesn't go to sleepmode ==> bad for connection
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		GUI.color = Color.black;
		m_tangoApplication = FindObjectOfType<TangoApplication> ();
		m_tangoApplication.Register (this);

		m_help.m_i = 0;
		m_help.shot_taken = false;
		m_help.Screen_Shot_File_Name = "test.png";

//		wRTC = new webRTC ();
		// If screenshot previously existed, remove it
		Path_Name = System.IO.Path.Combine (Application.persistentDataPath, m_help.Screen_Shot_File_Name);
		if (System.IO.File.Exists (Path_Name)) {
			System.IO.File.Delete (Path_Name);
		}

		m_help.m_pointCloud = m_pointCloud;

		buttonRect = new Rect (UI_LABEL_START_X,
			45.0f,
			300,
			300);
		buttonRect2 = new Rect (UI_LABEL_START_X,
			UI_LABEL_START_Y + 45.0f,
			300,
			300);
		buttonRect3 = new Rect (UI_LABEL_START_X,
			UI_LABEL_START_Y * 2 + 45.0f,
			300,
			300);
		screenOverlay = new Rect (0, 0, Screen.width, Screen.height);
		// keep track of positions of screen to place markers if necessary (rectangle option)
		m_help.GridCalculations ();
		draw_mode = 1;

		//shows the console on all platforms. for debugging only
		if (uDebugConsole)
			DebugHelper.ActivateConsole ();
		AddInfoMarker = new string[100];
//		wRTC.Append ("debug text test");
		//Set the webconnection
//		wRTC.setup_webRTC ();
	}

	/// <summary>
	/// Unity destroy function.
	/// </summary>
	public void OnDestroy ()
	{
		//remove screencaps
		Path_Name = System.IO.Path.Combine (Application.persistentDataPath, m_help.Screen_Shot_File_Name);
		if (System.IO.File.Exists (Path_Name)) {
			System.IO.File.Delete (Path_Name);
		}
		//unregister tango app
		m_tangoApplication.Unregister (this);
	}

	/// <summary>
	/// Update this instance.
	/// </summary>
	public void Update ()
	{
		
		//Rectangles use a different AXIS system than mouse position on screen (inverse y)
//		Vector3 mouse = new Vector3 (Input.mousePosition.x, Screen.height - Input.mousePosition.y, Input.mousePosition.z);
//		bool noButton = !buttonRect2.Contains (mouse) && !buttonRect.Contains (mouse) && !buttonRect3.Contains (mouse);
//		// It's better to keep the mousebutton effects in the update method as this works faster than the onGUI class
//		switch (draw_mode) {
//		case 0:
//			if (Input.GetMouseButtonDown (0)) {
//				if (noButton) {
//					StartCoroutine (_WaitForDepth (Input.mousePosition, m_help.shot_taken));
//				}
//			}
////			if (Input.GetMouseButtonUp (0)) {
////				// do nothing
////			}
////			if (Input.GetMouseButton (0)) {
////				// do nothing
////			}
//			break;
//		case 1:
//			if (Input.GetMouseButtonDown (0)) {
//				if (noButton) {
//					StartCoroutine (_WaitForDepthCircle (Input.mousePosition, call.markerNumber));
//				}
//				break;
//			}
////			if (Input.GetMouseButtonUp (0)) {
////			}
////			if (Input.GetMouseButton (0)) {
////			}
//			break;
//		case 2:
//			if (Input.GetMouseButtonDown (0)) {
//				if (noButton) {
//					m_help.tmpLine.Clear ();
//					StartCoroutine (_WaitForDepthFreeDraw (Input.mousePosition));
//				}
//			}
//			if (Input.GetMouseButtonUp (0)) {
//				if (noButton) {
//					m_help.newLineRenderer ();
//				}
//			}
//			if (Input.GetMouseButton (0)) {
//				if (noButton) {
//					StartCoroutine (_WaitForDepthFreeDraw (Input.mousePosition));
//				}
//			}
//			break;
//		}
		if (Input.GetKey (KeyCode.Escape)) {
			// This is a fix for a lifecycle issue where calling
			// Application.Quit() here, and restarting the application
			// immediately results in a deadlocked app.
			AndroidHelper.AndroidQuit ();
		}
//		requestHoldImage ();
	}

	public void RemoteUpdate (float x, float y, int marker)
	{
//		Debug.Log ("debugU: remote update: " + x + " " + y);
		//Rectangles use a different AXIS system than mouse position on screen (inverse y)
		Vector3 mouse = new Vector3 (x, y, 0.0f);
		bool noButton = !buttonRect2.Contains (mouse) && !buttonRect.Contains (mouse) && !buttonRect3.Contains (mouse);
		// It's better to keep the mousebutton effects in the update method as this works faster than the onGUI class
		switch (draw_mode) {
		case 0:
			if (noButton) {
				StartCoroutine (_WaitForDepth (mouse, m_help.shot_taken));
			}
			break;
		case 1:
			
			if (noButton) {
				StartCoroutine (_WaitForDepthCircle (mouse, marker));
			}
			break;
		case 2:
//			Debug.Log ("MouseMovement =from " + prevMousUp.ToString () + " to " + MouseUp.ToString ());
			if (prevMousUp & !MouseUp) {
				if (noButton) {
					m_help.tmpLine.Clear ();
					StartCoroutine (_WaitForDepthFreeDraw (mouse));
				}
			}
			if (MouseUp) {
				if (noButton) {
					m_help.newLineRenderer ();
				}
			}
			if (!MouseUp) {
				if (noButton) {
					StartCoroutine (_WaitForDepthFreeDraw (mouse));
				}
			}
			break;
		}
		if (Input.GetKey (KeyCode.Escape)) {
			// This is a fix for a lifecycle issue where calling
			// Application.Quit() here, and restarting the application
			// immediately results in a deadlocked app.
			AndroidHelper.AndroidQuit ();
		}
		prevMousUp = MouseUp;



	}

	/// <summary>
	/// Display simple GUI.
	/// </summary>
	public void OnGUI ()
	{
		//DebugHelper.DrawConsole ();
		//text = "Pic not taken yet";
		if (m_tangoApplication.HasRequestedPermissions ()) {
			GUI.color = Color.white;
			GUI.backgroundColor = Color.clear;
			if (m_help.shot_taken) {
				grid = 1;
			} else {
				grid = 0;
			}
			// ***************Here we check if an already placed marker gets touched****************
			Camera cam = Camera.main;
			RaycastHit hitInfo;
			if (Input.GetMouseButtonDown (0)) {
				touchedMarker = true;
			}
			if (Physics.Raycast (cam.ScreenPointToRay (Input.mousePosition), out hitInfo) & touchedMarker) {
				// Found a marker, select it (so long as it isn't disappearing)!
				GameObject tapped = hitInfo.collider.gameObject;
				style = new GUIStyle (GUI.skin.box);
				style.fontSize = 60;
				Texture2D backGround = new Texture2D (Screen.width, 200);
				backGround.SetPixels (new Color[]{ Color.gray });
				style.normal.background = backGround;
//				DisplayText.text = tapped.name + ": " + AddInfoMarker [Convert.ToInt32 (tapped.name)];
				GUI.Label(new Rect (0, Screen.height / 2, Screen.width, 200), tapped.name + ": " + AddInfoMarker [Convert.ToInt32 (tapped.name)], style);
			} else
				touchedMarker = false;
			
			//************************************************************************
			/*switch (draw_mode) {
			case 0:
				#pragma warning disable 618
//				if (m_help.shot_taken) {
//					GUI.DrawTexture (screenOverlay, m_help.Screenshot);
//				} 
				if (GUI.Button (buttonRect, cycleButtons [draw_mode])) {
					changeMode ();
					break;
				} 
				if (GUI.Button (buttonRect2, toggleGrid [grid])) {
					// Function to clear the points entered (actually reposition the index)
					m_help.screenCap ();
					break;
				} 
				if (GUI.Button (buttonRect3, screenGrab)) {
					m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
					m_help.newScreenCap ();
					break;
				}
				
				#pragma warning restore 618
				break;
			case 1:
				if (GUI.Button (buttonRect, cycleButtons [draw_mode])) {
					changeMode ();
					break;
				} 
				if (GUI.Button (buttonRect2, clearButton)) {
					Debug.Log ("Clear message received from peer?" + clearSignal.ToString ());
					// Function to clear the points entered (actually reposition the index)
					m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
					clearSignal = true;
					break;
				} 
				break;
			case 2:
				if (GUI.Button (buttonRect, cycleButtons [draw_mode])) {
					changeMode ();
					break;
				} 
				if (GUI.Button (buttonRect2, clearButton) || clearSignal) {
					// Function to clear the points entered (actually reposition the index)
					m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
					clearSignal = true;
				} 
				break;
			default:
				draw_mode = 1;
				break;
			}*/
		}
	}

	private void requestHoldImage ()
	{
//		Debug.Log ("requestHoldImage = " + CallApp.holdStill.ToString () + " once? " + once.ToString ());
		if (CallApp.holdStill & !once) {
			m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
			StartCoroutine (m_help.newScreenCap ());
			m_help.shot_taken = true;
			once = true;
		} else if (CallApp.holdStill) {
			m_help.shot_taken = true;

		} else {
			m_help.shot_taken = false;
			once = false;
		}
	}

	public void changeMode ()
	{
		// Function to clear the points entered (actually reposition the index)
		m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
		if (draw_mode < 2)
			draw_mode++;
		else
			draw_mode = 0;
	}

	/// <summary>
	/// This is called each time new depth data is available.
	/// 
	/// On the Tango tablet, the depth callback occurs at 5 Hz.
	/// </summary>
	/// <param name="tangoDepth">Tango depth.</param>
	public void OnTangoDepthAvailable (TangoUnityDepth tangoDepth)
	{
		// Don't handle depth here because the PointCloud may not have been
		// updated yet. Just tell the coroutine it can continue.
		m_waitingForDepth = false;
	}

	/// <summary>
	/// This is called when successfully connected to Tango service.
	/// </summary>
	public void OnTangoServiceConnected ()
	{
		m_tangoApplication.SetDepthCameraRate (
			TangoEnums.TangoDepthCameraRate.DISABLED);
	}

	/// <summary>
	/// This is called when disconnected from the Tango service.
	/// </summary>
	public void OnTangoServiceDisconnected ()
	{
	}

	/// <summary>
	/// Wait for the next depth update, then find the nearest point in the point
	/// cloud.
	/// </summary>
	/// <param name="touchPosition">Touch position on the screen.</param>
	/// <returns>Coroutine IEnumerator.</returns>
	private IEnumerator _WaitForDepthFreeDraw (Vector2 touchPosition)
	{
		m_waitingForDepth = true;
		// Turn on the camera and wait for a single depth update
		m_tangoApplication.SetDepthCameraRate (
			TangoEnums.TangoDepthCameraRate.MAXIMUM);
		while (m_waitingForDepth) {
			yield return null;
		}
		m_tangoApplication.SetDepthCameraRate (TangoEnums.TangoDepthCameraRate.DISABLED);
		Camera cam = Camera.main;
		pointIndex = m_pointCloud.FindClosestPoint (cam, touchPosition, 10);
		Vector3 worldTouchPoint = m_pointCloud.m_points [pointIndex];
		if (pointIndex > -1) {
			m_help.UpdateFreeDraw (worldTouchPoint);
		}
	}

	private IEnumerator _WaitForDepthCircle (Vector2 touchPosition, int markerNumber)
	{
		m_waitingForDepth = true;

		// Turn on the camera and wait for a single depth update.
		m_tangoApplication.SetDepthCameraRate (TangoEnums.TangoDepthCameraRate.MAXIMUM);
		while (m_waitingForDepth) {
			yield return null;
		}

		m_tangoApplication.SetDepthCameraRate (TangoEnums.TangoDepthCameraRate.DISABLED);

		// Find the plane.
		Camera cam = Camera.main;
		Vector3 planeCenter;
		Plane plane;
		if (!m_pointCloud.FindPlane (cam, touchPosition, out planeCenter, out plane)) {
			yield break;
		}

		// Ensure the location is always facing the camera.  This is like a LookRotation, but for the Y axis.
		Vector3 up = plane.normal;
		Vector3 forward;
		if (Vector3.Angle (plane.normal, cam.transform.forward) < 175) {
			Vector3 right = Vector3.Cross (up, cam.transform.forward).normalized;
			forward = Vector3.Cross (right, up).normalized;
		} else {
			// Normal is nearly parallel to camera look direction, the cross product would have too much
			// floating point error in it.
			forward = Vector3.Cross (up, cam.transform.right);
		}

		GameObject tmp = Instantiate (m_prefabMarker, planeCenter, Quaternion.LookRotation (forward, up));
		tmp.transform.localScale = new Vector3 (0.01f, 0.01f, 0.01f);
		tmp.name = markerNumber.ToString ();
		TextMesh markerText = (TextMesh)Instantiate (m_help.Text3D);

		//place number next to it
		m_help.placeNumber (markerText, planeCenter, Quaternion.LookRotation (forward, up), markerNumber);
		m_help.LineRendererIndex++;
		tmp.tag = "circle";
	}

	/// <summary>
	/// Wait for the next depth update, then find the nearest point in the point
	/// cloud.
	/// </summary>
	/// <param name="touchPosition">Touch position on the screen.</param>
	/// <returns>Coroutine IEnumerator.</returns>
	private IEnumerator _WaitForDepth (Vector2 touchPosition, bool shot_taken)
	{
		// if max dots placed don't place markers or wait for depth
		if (m_help.m_i >= 4) {
			yield break;
		}
		m_waitingForDepth = true;

		// Turn on the camera and wait for a single depth update
		m_tangoApplication.SetDepthCameraRate (TangoEnums.TangoDepthCameraRate.MAXIMUM);
		while (m_waitingForDepth) {
			yield return null;
		}

		m_tangoApplication.SetDepthCameraRate (TangoEnums.TangoDepthCameraRate.DISABLED);
		int distance = 0;
		distance = (int)Math.Sqrt ((float)Math.Pow ((float)Screen.width / 8f, 2f) + (float)Math.Pow ((float)Screen.height / 8f, 2f));// ==> teveel overlap met schuine!
		//With screenoverlay


//		Debug.Log ("shot taken?" + shot_taken.ToString ());


		if (shot_taken) {
			//we take the smallest distance between points, to have no overlap when searching for the correct point in the invisible grid.
			if (Screen.height > Screen.width)
				distance = (int)Screen.width / 8 - margin;
			else
				distance = (int)Screen.height / 8 - margin;
//			Debug.Log ("distance = " + distance);
			pointIndex = m_help.FindClosestPointGrid (touchPosition, distance);
		} else {
			Camera cam = Camera.main;
			pointIndex = m_pointCloud.FindClosestPoint (cam, touchPosition, 10);
		}
//		Debug.Log ("pointIndex =" + pointIndex);
		if (pointIndex > -1) {
			m_help.enableDot (pointIndex);
			if (m_help.m_i < 3) {
				m_help.m_i++;
			} else {
				m_help.UpdateRectangle ();
				m_help.m_i++;
			}
		}
	}


}
















