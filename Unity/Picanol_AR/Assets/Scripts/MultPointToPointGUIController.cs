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
	/// The line renderer to draw a line between two points.
	/// </summary>
	public LineRenderer m_lineRenderer;

	/// <summary>
	/// The scene's Tango application.
	/// </summary>
	private TangoApplication m_tangoApplication;

	/// <summary>
	/// If set, then the depth camera is on and we are waiting for the next
	/// depth update.
	/// </summary>
	private bool m_waitingForDepth;

	/// <summary>
	/// The older of the two points to measure.
	/// </summary>
//	private Vector3 m_startPoint;

	/// <summary>
	/// The newer of the two points to measure.
	/// </summary>
//	private Vector3 m_endPoint;
	private Vector3 recent_point;
	/// <summary>
	/// The distance between the two selected points.
	/// </summary>
	private float m_distance;

	/// <summary>
	/// The text to display the distance.
	/// </summary>
	private string m_distanceText;

	/// <summary>
	/// array to store touches to be able to create area of interest
	/// </summary>
	private Vector3[] points;
	private Vector3[] positionsOfPoints;
	private GameObject[] tempPoints;
	private List<Vector3> temp;

	/// <summary>
	///  counter for amount of dots
	/// </summary>
	private int m_i;
	/// <summary>
	/// The dot marker object.
	/// </summary>
	public GameObject DotMarker;

	/// <summary>
	/// The cam copy object. A copy is being made when a screenCap is requested, so that we can find the closest points with a screenshot and the locally stored camera
	/// </summary>
	public Camera CamCopy;

	private bool shot_taken;
	private string Path_Name;
	private Texture2D Screenshot;
	private string Screen_Shot_File_Name;
	private Vector3[] ScreenPos3;
	private bool camCopyMade;
	public static Camera m_emulationCamera;
	/// <summary>
	/// Start this instance.
	/// </summary>
	public void Start()
	{
		GUI.color = Color.black;
		m_tangoApplication = FindObjectOfType<TangoApplication>();
		m_tangoApplication.Register(this);
		points = new Vector3[4];
		m_i = 0;
		temp = new List<Vector3> ();
		tempPoints = new GameObject[4];
		shot_taken = false;
		Screen_Shot_File_Name = "test.png";
		// If screenshot previously existed, remove it
		Path_Name = System.IO.Path.Combine (Application.persistentDataPath, Screen_Shot_File_Name);
		if (System.IO.File.Exists (Path_Name)) {
			System.IO.File.Delete (Path_Name);
		}
		//CamCopy = new Camera ();
		ScreenPos3=new Vector3[m_pointCloud.m_pointsCount];
		camCopyMade = false;
	}

	/// <summary>
	/// Unity destroy function.
	/// </summary>
	public void OnDestroy()
	{
		//remove screencaps
		Path_Name = System.IO.Path.Combine (Application.persistentDataPath, Screen_Shot_File_Name);
		if (System.IO.File.Exists (Path_Name)) {
			System.IO.File.Delete (Path_Name);
		}
		//unregister tango app
		m_tangoApplication.Unregister(this);
	}

	/// <summary>
	/// Update this instance.
	/// </summary>
	public void Update()
	{
//		if (Input.GetMouseButtonDown(0))
//		{
//			GUI.color = Color.black;
//			if (shot_taken) {
//				StartCoroutine (_WaitForDepth (Input.mousePosition));
//			}
//		}

		if (Input.GetKey(KeyCode.Escape))
		{
			// This is a fix for a lifecycle issue where calling
			// Application.Quit() here, and restarting the application
			// immediately results in a deadlocked app.
			AndroidHelper.AndroidQuit();
		}
	}

	/// <summary>
	/// Display simple GUI.
	/// </summary>
	public void OnGUI()
	{
		if (m_tangoApplication.HasRequestedPermissions())
		{
			if (shot_taken) {
				GUI.DrawTexture(new Rect(0, 0, Screen.width,Screen.height),Screenshot);
			}
			Rect buttonRect = new Rect( UI_LABEL_START_X,
								UI_LABEL_START_Y,
								300,
								UI_LABEL_SIZE_Y);
			#pragma warning disable 618
			if (GUI.Button(buttonRect, "<size=25>" + "Clear" + "</size>"))
			{
				// Function to clear the points entered (actually reposition the index)
					ClearPoints();
			}
			#pragma warning restore 618

			Rect buttonRect2 = new Rect( UI_LABEL_START_X,
				UI_LABEL_START_Y*5,
				300,
				UI_LABEL_SIZE_Y);
			#pragma warning disable 618
			if (GUI.Button(buttonRect2, "<size=25>" + "ScreenCap" + "</size>"))
			{
				// Function to clear the points entered (actually reposition the index)
				screenCap();

			}
			#pragma warning restore 618

			if (m_i > 0) {
				GUI.Label (new Rect (900.0f,
					UI_LABEL_START_Y,
					300,
					UI_LABEL_SIZE_Y),
					"<size=25>" + m_i.ToString () + "</size>");
			}
			string text;
			if (shot_taken) {
				text = "Pic taken! " + Path_Name.ToString ();
			} else {
				text = "Pic not taken yet";
			}
			GUI.Label(new Rect(500.0f,
				UI_LABEL_START_Y,
				500.0f,
				200.0f),
				"<size=25>" + text + "</size>");
			if (Input.GetMouseButtonDown(0))
			{
				GUI.color = Color.black;
				if (shot_taken) {
					StartCoroutine (_WaitForDepth (Input.mousePosition));
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
	public void OnTangoDepthAvailable(TangoUnityDepth tangoDepth)
	{
		// Don't handle depth here because the PointCloud may not have been
		// updated yet. Just tell the coroutine it can continue.
		m_waitingForDepth = false;
	}

	/// <summary>
	/// This is called when successfully connected to Tango service.
	/// </summary>
	public void OnTangoServiceConnected()
	{
		m_tangoApplication.SetDepthCameraRate(
			TangoEnums.TangoDepthCameraRate.DISABLED);
	}

	/// <summary>
	/// This is called when disconnected from the Tango service.
	/// </summary>
	public void OnTangoServiceDisconnected()
	{
	}

	/// <summary>
	/// Wait for the next depth update, then find the nearest point in the point
	/// cloud.
	/// </summary>
	/// <param name="touchPosition">Touch position on the screen.</param>
	/// <returns>Coroutine IEnumerator.</returns>
	private IEnumerator _WaitForDepth(Vector2 touchPosition)
	{
		// if max dots placed don't place markers or wait for depth
		if (m_i >= 4) {
			yield break;
		}
		m_waitingForDepth = true;

		// Turn on the camera and wait for a single depth update
		m_tangoApplication.SetDepthCameraRate(
			TangoEnums.TangoDepthCameraRate.MAXIMUM);
		while (m_waitingForDepth)
		{
			yield return null;
		}

		m_tangoApplication.SetDepthCameraRate(
			TangoEnums.TangoDepthCameraRate.DISABLED);

		Camera cam = Camera.main;
//		if (m_emulationCamera == null)
//		{
//			m_emulationCamera = new GameObject().AddComponent<Camera>();
//			m_emulationCamera.gameObject.name = "Tango Environment Emulation Camera";
//			const float EMULATED_CAMERA_FOV = 37.8f;
//			m_emulationCamera.fieldOfView = EMULATED_CAMERA_FOV;
//			m_emulationCamera.enabled = false;
//			GameObject.DontDestroyOnLoad(m_emulationCamera.gameObject);
//		}
		//Camera cam = CamCopy;
		//////////////////////////////////////////////////////////////////////
		/*if (shot_taken && !camCopyMade) {
			for (int it = 0; it < m_pointCloud.m_pointsCount; ++it) {
				ScreenPos3 [it] = cam.WorldToScreenPoint (m_pointCloud.m_points [it]);
			}
			camCopyMade = true;
		}
		if (camCopyMade) {
			int pointIndex = FindClosestPointRemote (ScreenPos3, touchPosition, 10);*/
		int pointIndex = m_pointCloud.FindClosestPoint (cam, touchPosition, 10);
			////////////////////////////////////////////////////////////////////// 
			if (pointIndex > -1) {
				recent_point = m_pointCloud.m_points [pointIndex];
				points [m_i] = recent_point;
				showDots ();
				if (m_i < 3) {
					m_i++;
				} else {
					UpdateLine ();
				}
			}
//		}
	}

	void UpdateLine()
	{
		//enable linerenderer
		m_lineRenderer.enabled = true;

		foreach(Vector3 t in points)
			{
				temp.Add(t);
			}
		temp.Add (points [0]);
		positionsOfPoints = temp.ToArray();
		m_lineRenderer.numPositions=positionsOfPoints.Length; // add this
		m_lineRenderer.SetPositions(positionsOfPoints);
	}

	void showDots(){
		tempPoints [m_i] = (GameObject)Instantiate (DotMarker);
		tempPoints[m_i].transform.position = points[m_i];
		tempPoints[m_i].tag = "marker";
	}

	void ClearPoints()
	{
		GUI.color = Color.yellow;
		temp.Clear ();
		m_lineRenderer.enabled = false;
		m_i = 0;
		GameObject[] enemies = GameObject.FindGameObjectsWithTag("marker");
		foreach(GameObject enemy in enemies)
			GameObject.Destroy(enemy);
		//tempPoints = null;
		tempPoints = new GameObject[4];
	}

	void screenCap(){
		// Make a local copy of the camera to be used later 
		// ==>to retrieve correct point cloud locations, we want the camera at the time the screen is being frozen by the assistant, while the person in need (PIN) is able to use his camera at free will)
		// Coordinates of pointcloud are being made by reference of posx, posy of assistants screen taps (converted to resolution of PINs screen)
		if (!shot_taken) {
			//CamCopy.CopyFrom(Camera.main);

			Application.CaptureScreenshot (Screen_Shot_File_Name);
			Path_Name = System.IO.Path.Combine (Application.persistentDataPath, Screen_Shot_File_Name);
			if (System.IO.File.Exists (Path_Name)) {
			
				byte[] Bytes_File = System.IO.File.ReadAllBytes (Path_Name);
				Screenshot = new Texture2D (0, 0, TextureFormat.RGBA32, false);
				Screenshot.LoadImage (Bytes_File);
				shot_taken = true;
			} 
			else {
				shot_taken = true;
			}
		} else {

			//get rid of screenshot overlay by falsifying the screenshotBoolean
			shot_taken = false;
		
		}
	}

	/// @endcond
	/// <summary>
	/// Finds the closest point from a point cloud to a position on screen.
	/// 
	/// This function is slow, as it looks at every single point in the point
	/// cloud. Avoid calling this more than once a frame.
	/// </summary>
	/// <returns>The index of the closest point, or -1 if not found.</returns>
	/// <param name="cam">The current camera.</param>
	/// <param name="pos">Position on screen (in pixels).</param>
	/// <param name="maxDist">The maximum pixel distance to allow.</param>
	public int FindClosestPointRemote(Vector3[] ScreenPos3, Vector2 pos, int maxDist)
	{
		int bestIndex = -1;
		float bestDistSqr = 0;

		for (int it = 0; it < m_pointCloud.m_pointsCount; ++it)
		{
			//Vector3 screenPos3 = cam.WorldToScreenPoint(m_points[it]);
			Vector2 screenPos = new Vector2(ScreenPos3[it].x, ScreenPos3[it].y);

			float distSqr = Vector2.SqrMagnitude(screenPos - pos);
			if (distSqr > maxDist * maxDist)
			{
				continue;
			}

			if (bestIndex == -1 || distSqr < bestDistSqr)
			{
				bestIndex = it;
				bestDistSqr = distSqr;
			}
		}

		return bestIndex;
	}
}