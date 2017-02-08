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
	public const float UI_LABEL_START_Y = 40.0f;
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
	private bool circle;
	//private List<Vector2> lineList;

	public void Start ()
	{
		GUI.color = Color.black;
		m_tangoApplication = FindObjectOfType<TangoApplication> ();
		m_tangoApplication.Register (this);

		m_help.m_i = 0;
		//Line = new List<Vector3> ();
		//lineList = new List<Vector2> ();
		m_help.shot_taken = false;
		m_help.Screen_Shot_File_Name = "test.png";

		// If screenshot previously existed, remove it
		Path_Name = System.IO.Path.Combine (Application.persistentDataPath, m_help.Screen_Shot_File_Name);
		if (System.IO.File.Exists (Path_Name)) {
			System.IO.File.Delete (Path_Name);
		}

		m_help.m_pointCloud = m_pointCloud;

		buttonRect = new Rect (UI_LABEL_START_X,
			UI_LABEL_START_Y,
			300,
			UI_LABEL_SIZE_Y);
		buttonRect2 = new Rect (UI_LABEL_START_X,
			UI_LABEL_START_Y * 5,
			300,
			UI_LABEL_SIZE_Y);
		buttonRect3 = new Rect (UI_LABEL_START_X,
			UI_LABEL_START_Y * 9,
			300,
			UI_LABEL_SIZE_Y);
		screenOverlay = new Rect (0, 0, Screen.width, Screen.height);
		// keep track of positions of screen to place markers if necessary (rectangle option)
		m_help.GridCalculations ();


		circle = false; 
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
		///////////////////////
		Vector2 tmp;
		tmp = Input.mousePosition;
		tmp.y = Screen.height - tmp.y;

		if (Input.GetMouseButtonDown (0)) {
			if (!circle) {
				//if (ScreenshotReady) {
				StartCoroutine (_WaitForDepth (Input.mousePosition));
				//} 
			} else {
				//lineList.Clear ();
//				lineList.Add (tmp);
				m_help.tmpLine.Clear ();
				StartCoroutine (_WaitForDepthCircle (Input.mousePosition));
				// start new instance of LineList & Line, get a new LineRenderer

			}
		}

		if (Input.GetMouseButtonUp (0)) {
			if (circle) {
				StartCoroutine (_WaitForDepthCircle (Input.mousePosition));
			} else {
				// do nothing
			}
		}
		if (Input.GetMouseButton (0)) {
			if (circle) {
//				lineList.Add (tmp);
				StartCoroutine (_WaitForDepthCircle (Input.mousePosition));
			} else {
				// do nothing
			}
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
		text = "Pic not taken yet";
		if (m_tangoApplication.HasRequestedPermissions ()) {
			if (!circle) {
				GUI.color = Color.white;
				if (m_help.shot_taken) {
					GUI.DrawTexture (screenOverlay, m_help.Screenshot);
				}

				#pragma warning disable 618
				if (GUI.Button (buttonRect, "<size=25>" + "Circle?" + "</size>")) {
					// Function to clear the points entered (actually reposition the index)
					circle = true;
				}
				if (GUI.Button (buttonRect2, "<size=25>" + "ScreenCap" + "</size>")) {
					// Function to clear the points entered (actually reposition the index)
					m_help.screenCap ();
				}
				if (GUI.Button (buttonRect3, "<size=20>" + "new screencap" + "</size>")) {
					m_help.ClearPoints ();
					m_help.newScreenCap ();
				}
				#pragma warning restore 618

				GUI.Label (new Rect (500.0f,
					UI_LABEL_START_Y,
					500.0f,
					200.0f),
					"<size=25>" + text + "</size>");
			} else {
				if (GUI.Button (buttonRect, "<size=25>" + "rectangle?" + "</size>")) {
					circle = false;
				}
				if (GUI.Button (buttonRect2, "<size=25>" + "Clear" + "</size>")) {
					// Function to clear the points entered (actually reposition the index)
					m_help.ClearPoints ();
				}
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
	private IEnumerator _WaitForDepthCircle (Vector2 touchPosition)
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
			m_help.UpdateCircle (worldTouchPoint);
		}
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
			//we take the smallest distance between points, to have no overlap when searching for the correct point in the invisble grid.
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
				m_help.UpdateLine ();
				m_help.m_i++;
			}
		}
	}


}
















