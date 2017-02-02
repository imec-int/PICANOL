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
	public LineRenderer m_lineRenderer2;

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
	private Vector3 recent_point2;
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
	private Vector3[] points2;
	private Vector3[] positionsOfPoints;
	private GameObject[] tempPoints;
	private GameObject[] tempPoints2;
	private List<Vector3> temp;

	/// <summary>
	///  counter for amount of dots
	/// </summary>
	private int m_i;
	/// <summary>
	/// The dot marker object.
	/// </summary>
	public GameObject DotMarker;
	public GameObject DotMarker2;
	private GameObject[] DotMarkerInvisible;

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
	private bool mode2D;
	private Vector2[] touchPositions;
	private Vector2[] CamCopyVector;
	public Texture markerTex;
	private int pointIndex;
	private int pointIndex2;
	private bool checkPointCloudIndex;
	private const int GRID_SIZE = 9;
	private Vector2[] GridPosition;
	/// <summary>
	/// The margin for the grid when checking if tapped nearby a certain point.
	/// </summary>
	private const int margin = 10;
	/// <summary>
	/// Start this instance.
	/// </summary>
	public void Start ()
	{
		GUI.color = Color.black;
		m_tangoApplication = FindObjectOfType<TangoApplication> ();
		m_tangoApplication.Register (this);
		markerTex = FindObjectOfType<Texture> ();
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
		ScreenPos3 = new Vector3[m_pointCloud.m_pointsCount];
		camCopyMade = false;
		mode2D = false; 
		touchPositions = new Vector2[4];
		CamCopyVector = new Vector2[m_pointCloud.m_pointsCount];

		points2 = new Vector3[9];
		tempPoints2 = new GameObject[4];
		checkPointCloudIndex = false;
		DotMarkerInvisible = new GameObject[GRID_SIZE];
		GridPosition = new Vector2[GRID_SIZE];
	}

	/// <summary>
	/// Unity destroy function.
	/// </summary>
	public void OnDestroy ()
	{
		//remove screencaps
		Path_Name = System.IO.Path.Combine (Application.persistentDataPath, Screen_Shot_File_Name);
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
		if (Input.GetMouseButtonDown (0)) {
			
			if (shot_taken) {
				StartCoroutine (_WaitForDepth (Input.mousePosition));
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
		
		if (m_tangoApplication.HasRequestedPermissions ()) {
			GUI.color = Color.white;
			if (shot_taken) {

				Color colPreviousGUIColor = GUI.color;

				GUI.color = new Color (colPreviousGUIColor.r, colPreviousGUIColor.g, colPreviousGUIColor.b, 1f);
				GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), Screenshot);
				GUI.color = colPreviousGUIColor;
			}
			Rect buttonRect = new Rect (UI_LABEL_START_X,
				                  UI_LABEL_START_Y,
				                  300,
				                  UI_LABEL_SIZE_Y);
			Rect buttonRect2 = new Rect (UI_LABEL_START_X,
				                   UI_LABEL_START_Y * 5,
				                   300,
				                   UI_LABEL_SIZE_Y);
			Rect buttonRect3 = new Rect (UI_LABEL_START_X,
				UI_LABEL_START_Y * 10,
				300,
				UI_LABEL_SIZE_Y);
			#pragma warning disable 618
			if (GUI.Button (buttonRect, "<size=25>" + "Clear" + "</size>")) {
				// Function to clear the points entered (actually reposition the index)
				ClearPoints ();
			}
			if (GUI.Button (buttonRect2, "<size=25>" + "ScreenCap" + "</size>")) {
				// Function to clear the points entered (actually reposition the index)
				screenCap ();

			}
			if (GUI.Button (buttonRect3, "<size=20>" + "new screencap" + "</size>")) {
				newScreenCap ();
			}
//			if (mode2D) { 
//				//array list with vector2 coords to draw temp dots
//				//GUI.DrawTextureWithTexCoords(new Rect(0,0,touchPositions[m_i].x,touchPositions[m_i].y),markerTex, new Rect(0,0,10f,10f));
//				GUI.DrawTexture (new Rect (touchPositions [m_i - 1], new Vector2 (10f, 10f)), markerTex);
//			}
			#pragma warning restore 618

//			if (m_i > 0) {
//				GUI.Label (new Rect (800.0f,
//					UI_LABEL_START_Y,
//					200,
//					UI_LABEL_SIZE_Y),
//					"<size=25>" + m_i.ToString () + "</size>");
//			}
			string text;
			if (shot_taken) {
				text = "Pic taken! "; //+ Path_Name.ToString ()
			} else {
				text = "Pic not taken yet";
			}
			GUI.Label (new Rect (500.0f,
				UI_LABEL_START_Y,
				500.0f,
				200.0f),
				"<size=25>" + text + "</size>");
//			GUI.TextField (new Rect (800.0f,
//				UI_LABEL_START_Y,
//				400f,
//				100f),
//				m_i.ToString () + " wanted " + pointIndex.ToString () + " " + recent_point.ToString () + " through cam " + pointIndex2.ToString () + " " + recent_point2.ToString ());
//			GUI.TextField (new Rect (800.0f,
//				UI_LABEL_START_Y + 100f,
//				400f,
//				100), "PointCloud corresponds? " + checkPointCloudIndex.ToString ());
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
	private IEnumerator _WaitForDepth (Vector2 touchPosition)
	{
		// if max dots placed don't place markers or wait for depth
		if (m_i >= 4) {
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
		int distance;
		//we take the smallest distance between points, to have no overlap when searching for the correct point in the invisble grid.
//		if (Screen.height > Screen.width)
//			distance = (int)Screen.width / 8 - margin;
//		else
//			distance = (int)Screen.height / 8 - margin;
			distance = (int)Math.Sqrt((float)Math.Pow((float)Screen.width/8f,2f)+(float)Math.Pow((float)Screen.height/8f,2f));// ==> teveel overlap met schuine!
		pointIndex = FindClosestPointGrid (touchPosition, distance);
		if (pointIndex > -1) {
			// choose prefab object with corresponding index
//			recent_point = m_pointCloud.m_points [pointIndex];
			// Place different marker on top
			enableDot(DotMarkerInvisible[pointIndex]);
//			recent_point = 
//			points [m_i] = recent_point;
//	 		showDots (DotMarker, "marker", points[m_i]);
			if (m_i < 3) {
				m_i++;
			} else {
				mode2D = false;
				UpdateLine ();
				m_i++;
			}
		}
	}

	void UpdateLine ()
	{
		//enable linerenderer
		m_lineRenderer.enabled = true;
		GameObject[] DotList = GameObject.FindGameObjectsWithTag ("marker");
		foreach (GameObject t in DotList) {
			temp.Add (t.transform.position);
		}
		temp.Add (DotList [0].transform.position);
		positionsOfPoints = temp.ToArray ();
		m_lineRenderer.numPositions = positionsOfPoints.Length; // add this
		m_lineRenderer.SetPositions (positionsOfPoints);
	}

	/// <summary>
	/// Places the markerdots.
	/// </summary>
	private GameObject showDots (GameObject marker, string strmarker, Vector3 MarkerPlace)
	{
		tempPoints [m_i] = (GameObject)Instantiate (marker);
		tempPoints [m_i].transform.position = MarkerPlace;
		tempPoints [m_i].tag = strmarker;
		return tempPoints [m_i];
	}
	/// <summary>
	/// Enables the dots which are being approx tapped .
	/// </summary>
	/// <returns>The dots.</returns>
	private GameObject enableDot(GameObject marker){
		GameObject test = (GameObject)Instantiate (DotMarker);
			test.transform.position = marker.transform.position;
		test.tag = "marker";
		return test;
	}
	/// <summary>
	/// Clears the Marker dots & the line renderer.
	/// </summary>
	void ClearPoints ()
	{
		temp.Clear ();
		m_lineRenderer.enabled = false;
		m_i = 0;
		// remove all game objects based on marker tag
		GameObject[] enemies = GameObject.FindGameObjectsWithTag ("marker");
		foreach (GameObject enemy in enemies) {
			GameObject.Destroy (enemy);
		}
		enemies = GameObject.FindGameObjectsWithTag ("marker_invisible");
		foreach (GameObject enemy in enemies) {
			GameObject.Destroy (enemy);
		}
		//clear points array (CORRECT WAY?)
		tempPoints = new GameObject[4];
	}

	void screenCap ()
	{
		// Make a local copy of the camera to be used later 
		// ==>to retrieve correct point cloud locations, we want the camera at the time the screen is being frozen by the assistant, while the person in need (PIN) is able to use his camera at free will)
		// Coordinates of pointcloud are being made by reference of posx, posy of assistants screen taps (converted to resolution of PINs screen)
		if (!shot_taken) {
			Application.CaptureScreenshot (Screen_Shot_File_Name);
			Path_Name = System.IO.Path.Combine (Application.persistentDataPath, Screen_Shot_File_Name);
			FillGrid ();
			if (System.IO.File.Exists (Path_Name)) {
				byte[] Bytes_File = System.IO.File.ReadAllBytes (Path_Name);
				Screenshot = new Texture2D (0, 0, TextureFormat.RGBA32, false);
				Screenshot.LoadImage (Bytes_File);
				shot_taken = true;
			} 

//			#TODO //remove this part for androidapp
			else {
				shot_taken = true;
			}
			// Make Copy of the PointCloud information based on this camera location

//			#
		} else {

			//get rid of screenshot overlay by falsifying the screenshotBoolean
			shot_taken = false;
		
		}
	}

	void newScreenCap(){
		Path_Name = System.IO.Path.Combine (Application.persistentDataPath, Screen_Shot_File_Name);
		if (System.IO.File.Exists (Path_Name)) {
			System.IO.File.Delete (Path_Name);
		}

		Screenshot = null;
		shot_taken = false;
		screenCap ();
	}

	/// @endcond
	/// <summary>
	/// Finds the closest point from a Grid to a position on screen.
	/// 
	/// This function is slow, as it looks at every single point in the absolute Grid.
	/// Avoid calling this more than once a frame.
	/// </summary>
	/// <returns>The index of the closest point, or -1 if not found.</returns>
	/// <param name="cam">The current camera.</param>
	/// <param name="pos">Position on screen (in pixels).</param>
	/// <param name="maxDist">The maximum pixel distance to allow.</param>
	public int FindClosestPointGrid (Vector2 pos, int maxDist)
	{
		int bestIndex = -1;
		float bestDistSqr = 0;

		for (int i = 0; i < GRID_SIZE; i++) {
			
			Vector2 screenPos = GridPosition [i];
			float distSqr = Vector2.SqrMagnitude (screenPos - pos);
			if (distSqr > maxDist * maxDist) {
				continue;
			}

			if (bestIndex == -1 || distSqr < bestDistSqr) {
				bestIndex = i;
				bestDistSqr = distSqr;
			}
		}

		return bestIndex;
	}
	/// <summary>
	/// Fills the grid.
	/// GameObject[] DotMarkerInvisible keeps track of where in absolute coords we've placed invisible markers
	/// </summary>
	public void FillGrid ()
	{
		Camera cam = Camera.main;
		//place 1 marker in center of screen
		//
		// 2 5 8
		// 1 4 7
		// 0 3 6

		int k = 0;
		for (int i = 0; i <= 2; i++) {
			for (int j = 0; j <= 2; j++) {
				GridPosition [k] = new Vector2 (Screen.width * 0.25f + Screen.width * i * 0.25f, Screen.height * 0.25f + Screen.height * j * 0.25f);
				k++;
			}
		}
		for (k = 0; k < GRID_SIZE; k++) {
			int pointIndexGrid = m_pointCloud.FindClosestPoint (cam, GridPosition [k], 10);
			if (pointIndexGrid > -1) {
				Vector3 recent_point_Grid = m_pointCloud.m_points [pointIndexGrid];
				points2 [k] = recent_point_Grid;
				DotMarkerInvisible[k]=showDots (DotMarker2, "marker_invisible", points2[k]);
			}
		}

	}

}

