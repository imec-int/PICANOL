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
public class MultPointToPointGUIController : MonoBehaviour, ITangoDepth
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
	/// The marker prefab to place on taps.
	/// </summary>
	public GameObject m_prefabMarker;

	public void Start ()
	{
		GUI.color = Color.black;
		m_tangoApplication = FindObjectOfType<TangoApplication> ();
		m_tangoApplication.Register (this);

		m_help.m_i = 0;
		m_help.shot_taken = false;
		m_help.Screen_Shot_File_Name = "test.png";

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
		draw_mode = 0;
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
		Vector3 muis = new Vector3 (Input.mousePosition.x, Screen.height - Input.mousePosition.y, Input.mousePosition.z);
//		Rect rect = new Rect(0, 0, Screen.width, Screen.height);
//		if (rect.Contains(muis))
//			text = "muis in scherm "+muis.ToString();
//		if(buttonRect.Contains(muis))
//			text = "button 1 "+muis.ToString();
//		if(buttonRect2.Contains(muis))
//			text = "button 2 "+muis.ToString();
//		if(buttonRect3.Contains(muis))
//			text = "button 3 "+muis.ToString();
		// It's better to keep the mousebutton effects in the update method as this works faster than the onGUI class
		switch (draw_mode) {
		case 0:
			if (Input.GetMouseButtonDown (0)) {
				if (!buttonRect2.Contains (muis)) {
					StartCoroutine (_WaitForDepth (Input.mousePosition));
				}
			}
			if (Input.GetMouseButtonUp (0)) {
				// do nothing
			}
			if (Input.GetMouseButton (0)) {
				// do nothing
			}
			break;
		case 1:
			if (Input.GetMouseButtonDown (0)) {
				if (!buttonRect2.Contains (muis)) {
					
					StartCoroutine (_WaitForDepthCircle (Input.mousePosition));
				}
				break;
			}
			if (Input.GetMouseButtonUp (0)) {
				
			}
			if (Input.GetMouseButton (0)) {
			}
			break;
		case 2:
			if (Input.GetMouseButtonDown (0)) {
				m_help.tmpLine.Clear ();

				StartCoroutine (_WaitForDepthFreeDraw (Input.mousePosition));
			}
			if (Input.GetMouseButtonUp (0)) {
				if (!buttonRect2.Contains (muis)) {
					m_help.newLineRenderer ();
					m_help.placeNumber (m_help.FreeDrawText, m_help.sum, new Quaternion (0, 0, 0, 1));

				}

				
			}
			if (Input.GetMouseButton (0)) {
				StartCoroutine (_WaitForDepthFreeDraw (Input.mousePosition));
			}

			break;


		}
		if (Input.GetKey (KeyCode.Escape)) {
			// This is a fix for a lifecycle issue where calling
			// Application.Quit() here, and restarting the application
			// immediately results in a deadlocked app.
			AndroidHelper.AndroidQuit ();
		}
	}

	/// <summary>
	/// Display simple GUI.
	/// </summary>
	public void OnGUI ()
	{
		//text = "Pic not taken yet";
		if (m_tangoApplication.HasRequestedPermissions ()) {
			GUI.color = Color.white;
			GUI.backgroundColor = Color.clear;
			if (m_help.shot_taken) {
				grid = 1;
			} else {
				grid = 0;
			}
			text = m_help.sum.ToString();
			GUI.Label (new Rect (300.0f,
					45.0f,
					500.0f,
					200.0f),
					"<size=25>" + text + "</size>");
			switch (draw_mode) {
			case 0:
				#pragma warning disable 618
				if (m_help.shot_taken) {
					GUI.DrawTexture (screenOverlay, m_help.Screenshot);
				} 
				if (GUI.Button (buttonRect, cycleButtons [draw_mode])) {
					// Function to clear the points entered (actually reposition the index)
					m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
					draw_mode++;
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
//					if (Input.GetMouseButtonDown (0)) {
//						StartCoroutine (_WaitForDepth (Input.mousePosition));
//						break;
//					}
//					if (Input.GetMouseButtonUp (0)) {
//						// do nothing
//					}
//					if (Input.GetMouseButton (0)) {
//						// do nothing
//					}
						
				
				#pragma warning restore 618
				break;
			case 1:
				if (GUI.Button (buttonRect, cycleButtons [draw_mode])) {
					m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
					draw_mode++;
					break;
				} 
				if (GUI.Button (buttonRect2, clearButton)) {
					// Function to clear the points entered (actually reposition the index)
					m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
					break;
				} 

//					if (Input.GetMouseButtonDown (0)) {
//						StartCoroutine (_WaitForDepthCircle (Input.mousePosition));
//						break;
//					}
//					if (Input.GetMouseButtonUp (0)) {
//
//					}
//					if (Input.GetMouseButton (0)) {
//					}

				break;
			case 2:
				if (GUI.Button (buttonRect, cycleButtons [draw_mode])) {
					draw_mode = 0;
					m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
				} 
				if (GUI.Button (buttonRect2, clearButton)) {
					// Function to clear the points entered (actually reposition the index)
					m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
				} 
//					if (Input.GetMouseButtonDown (0)) {
//						m_help.tmpLine.Clear ();
//						StartCoroutine (_WaitForDepthFreeDraw (Input.mousePosition));
//					}
//					if (Input.GetMouseButtonUp (0)) {
//						StartCoroutine (_WaitForDepthFreeDraw (Input.mousePosition));
//					}
//					if (Input.GetMouseButton (0)) {
//						StartCoroutine (_WaitForDepthFreeDraw (Input.mousePosition));
//					}
//					
				
				break;
			default:
				draw_mode = 1;
				break;
			}
		}
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

	private IEnumerator _WaitForDepthCircle (Vector2 touchPosition)
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
		TextMesh markerText = (TextMesh)Instantiate (m_help.Text3D);

		//place number next to it
		m_help.placeNumber (markerText, planeCenter, Quaternion.LookRotation (forward, up));
		m_help.LineRendererIndex++;
		tmp.tag = "circle";
	}

	/// <summary>
	/// Wait for the next depth update, then find the nearest point in the point
	/// cloud.
	/// </summary>
	/// <param name="touchPosition">Touch position on the screen.</param>
	/// <returns>Coroutine IEnumerator.</returns>
	private IEnumerator _WaitForDepth (Vector2 touchPosition)
	{
		// if max dots placed don't place markers or wait for depth
		if (m_help.m_i >= 4) {
			yield break;
		}
		m_waitingForDepth = true;

		// Turn on the camera and wait for a single depth update
		m_tangoApplication.SetDepthCameraRate (
			TangoEnums.TangoDepthCameraRate.MAXIMUM);
		while (m_waitingForDepth) {
			yield return null;
		}

		m_tangoApplication.SetDepthCameraRate (
			TangoEnums.TangoDepthCameraRate.DISABLED);
		int distance = 0;
		distance = (int)Math.Sqrt ((float)Math.Pow ((float)Screen.width / 8f, 2f) + (float)Math.Pow ((float)Screen.height / 8f, 2f));// ==> teveel overlap met schuine!
		//With screenoverlay
		if (m_help.shot_taken) {
			//we take the smallest distance between points, to have no overlap when searching for the correct point in the invisible grid.
			//		if (Screen.height > Screen.width)
			//			distance = (int)Screen.width / 8 - margin;
			//		else
			//			distance = (int)Screen.height / 8 - margin;
			pointIndex = m_help.FindClosestPointGrid (touchPosition, distance);
		} else {
			Camera cam = Camera.main;
			pointIndex = m_pointCloud.FindClosestPoint (cam, touchPosition, 10);
		}
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
















