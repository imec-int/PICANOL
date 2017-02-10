using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tango;

public class MultiPointHelp : MonoBehaviour
{
	/// <summary>
	/// The dot marker object for the VISIBLE points.
	/// </summary>
	public GameObject DotMarker;
	/// <summary>
	/// If wanted (for visual feedback if algorithm is functioning properly)
	/// </summary>
	public GameObject DotMarker2;
	/// <summary>
	/// The line renderer to draw a line between two points.
	/// </summary>
	public LineRenderer m_lineRenderer;
	private LineRenderer[] m_lineRendererList;
	public int LineRendererIndex;
	private string Path_Name;
	public string Screen_Shot_File_Name;

	/// <summary>
	/// array to store touches to be able to create area of interest
	/// </summary>
	private Vector3[] points;
	/// <summary>
	/// The point cloud object in the scene.
	/// </summary>
	public TangoPointCloud m_pointCloud;
	/// <summary>
	/// Array to keep track of the invisible gameobjects 
	/// </summary>
	private GameObject[] DotMarkerInvisible;
	/// <summary>
	/// Array to keep locations of invisible markers
	/// </summary>
	private Vector2[] GridPosition;

	private GameObject[] tempPoints;
	/// <summary>
	/// Amount of invisble markers to place ==> higher is preciser yet heavier? ==> BENCHMARK!
	/// </summary>
	private const int GRID_SIZE = 144;
//	private bool ScreenshotReady;
	public Texture2D Screenshot;
	private Vector3[] positionsOfPoints;
	public bool shot_taken;
	public List<Vector3> tmpLine;
	public bool new_circle;
	public TextMesh Text3D;
	public TextMesh FreeDrawText;
	/// <summary>
	/// sum of all points in free draw to center number (text)
	/// </summary>
	public Vector3 sum;
	/// <summary>
	///  counter for amount of dots
	/// </summary>
	public int m_i;
	/// <summary>
	/// The scene's Tango application.
	/// </summary>
	private TangoApplication m_tangoApplication;
	// Use this for initialization
	void Start ()
	{
		LineRendererIndex = 0;
		m_lineRendererList = new LineRenderer[20]; 
		m_tangoApplication = FindObjectOfType<TangoApplication> ();
		m_tangoApplication.Register (this);
		tempPoints = new GameObject[GRID_SIZE];
		points = new Vector3[GRID_SIZE];
		DotMarkerInvisible = new GameObject[GRID_SIZE];
//		ScreenshotReady = false;
		m_lineRendererList[LineRendererIndex] = (LineRenderer)Instantiate (m_lineRenderer);
		FreeDrawText = (TextMesh)Instantiate (Text3D);
			//FindObjectOfType<LineRenderer> ();
		m_lineRendererList[LineRendererIndex].enabled = false;
		tmpLine = new List<Vector3> ();
		Path_Name = System.IO.Path.Combine (Application.persistentDataPath, Screen_Shot_File_Name);
		new_circle = true;

	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}

	/// <summary>
	/// Places the markerdots.
	/// </summary>
	public GameObject showDots (GameObject marker, string strmarker, Vector3 MarkerPlace, int m_i)
	{
		tempPoints [m_i] = (GameObject)Instantiate (marker);
		tempPoints [m_i].transform.position = MarkerPlace;
		tempPoints [m_i].tag = strmarker;
		return tempPoints [m_i];
	}

	/// <summary>
	/// Fills the grid.
	/// GameObject[] DotMarkerInvisible keeps track of where in absolute coords we've placed invisible markers
	/// </summary>
	public void FillGrid (GameObject[] Fill)
	{
		
		Camera cam = Camera.main;
		//place 1 marker in center of screen
		//
		// 2 5 8
		// 1 4 7
		// 0 3 6

		for (int k = 0; k < GRID_SIZE; k++) {
			int pointIndexGrid = m_pointCloud.FindClosestPoint (cam, GridPosition [k], 10);
			if (pointIndexGrid > -1) {
				Vector3 recent_point_Grid = m_pointCloud.m_points [pointIndexGrid];
				points [k] = recent_point_Grid;
				Fill [k] = showDots (DotMarker2, "marker_invisible", points [k], k);
			}
		}
	}

	public void GridCalculations ()
	{
		GridPosition = new Vector2[GRID_SIZE];

		int loop = (int)Math.Sqrt (GRID_SIZE);
		float screenDiv = (float)(1 / ((float)loop + 1));
		int k = 0;
		for (int i = 0; i <= loop - 1; i++) {
			for (int j = 0; j <= loop - 1; j++) {
				GridPosition [k] = new Vector2 (Screen.width * screenDiv + Screen.width * i * screenDiv, Screen.height * screenDiv + Screen.height * j * screenDiv);
				k++;
			}
		}
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
	/// Clears the Marker dots & the line renderer.
	/// </summary>
	public bool ClearPoints (string[] type)
	{
		tmpLine.Clear ();
		foreach (LineRenderer lr in m_lineRendererList) {
			LineRenderer.Destroy(lr);
		}
		LineRendererIndex = 0;
		m_lineRendererList[LineRendererIndex] = (LineRenderer)Instantiate (m_lineRenderer);
		m_lineRendererList[LineRendererIndex].enabled = false;
		m_i = 0;
		// remove all game objects based on marker tag
		foreach (string t in type){
			GameObject[] enemies = GameObject.FindGameObjectsWithTag (t);
			foreach (GameObject enemy in enemies) {
				GameObject.Destroy (enemy);
			}
		}

//		GameObject[] enemies = GameObject.FindGameObjectsWithTag ("marker");
//		foreach (GameObject enemy in enemies) {
//			GameObject.Destroy (enemy);
//		}
//		if (all) {
//			enemies = GameObject.FindGameObjectsWithTag ("marker_invisible");
//			foreach (GameObject enemy in enemies) {
//				GameObject.Destroy (enemy);
//			}
//		}
		//clear points array (CORRECT WAY?)
		tempPoints = new GameObject[GRID_SIZE];
		return true;
	}

	public void screenCap ()
	{			
		// We turn the screenshot on or off
		//get rid of screenshot overlay by falsifying the screenshotBoolean
		if (System.IO.File.Exists (Path_Name)) {
			byte[] Bytes_File = System.IO.File.ReadAllBytes (Path_Name);
			Screenshot = new Texture2D (0, 0, TextureFormat.RGBA32, false);
			Screenshot.LoadImage (Bytes_File);
		}	
		shot_taken = !shot_taken;
	}
	public void newLineRenderer(){

		LineRendererIndex++;
		m_lineRendererList[LineRendererIndex] = (LineRenderer)Instantiate (m_lineRenderer);
		FreeDrawText = (TextMesh)Instantiate (Text3D);
		m_lineRendererList[LineRendererIndex].enabled = false;
		}
	public void newScreenCap ()
	{
		if (System.IO.File.Exists (Path_Name)) {
			System.IO.File.Delete (Path_Name);
		}
		Application.CaptureScreenshot (Screen_Shot_File_Name);
		//Faster method, more prone to shaking...
		FillGrid (DotMarkerInvisible);
		//ERROR: ReadPixels was called to read pixels from system frame buffer, while not inside drawing frame.
		//UnityEngine.Texture2D:ReadPixels(Rect, Int32, Int32)
//		Texture2D screenShot2 = new Texture2D (Screen.width, Screen.height);
//		screenShot2.ReadPixels (new Rect (0, 0, Screen.width, Screen.height), 0, 0);
//		screenShot2.Apply ();
//		byte[] bytes = screenShot2.EncodeToPNG ();
//		Destroy(screenShot2);
//		System.IO.File.WriteAllBytes (Path_Name, bytes);
	}
	 

	/// <summary>
	/// Enables the dots which are being approx tapped .
	/// </summary>
	/// <returns>The dots.</returns>
	public GameObject enableDot (int pointIndex)
	{
		Vector3 pos = new Vector3 ();
		if (shot_taken) {
			pos = DotMarkerInvisible [pointIndex].transform.position;
		} else {
			pos = m_pointCloud.m_points [pointIndex];
		}
		GameObject test = (GameObject)Instantiate (DotMarker);
		test.transform.position = pos;
		test.tag = "marker";
		return test;
	}

	public void UpdateFreeDraw (Vector3 lastPoint)
	{

		//enable linerenderer
		m_lineRendererList[LineRendererIndex].enabled = true;
		tmpLine.Add (lastPoint);
		positionsOfPoints = tmpLine.ToArray ();
		m_lineRendererList[LineRendererIndex].numPositions = positionsOfPoints.Length; // add this
		m_lineRendererList[LineRendererIndex].SetPositions (positionsOfPoints);
		sum=new Vector3(0,0,0);
		foreach (Vector3 tmp in positionsOfPoints) {
			sum += tmp;

		}
		sum = sum / positionsOfPoints.Length;
		placeNumber (FreeDrawText, sum, new Quaternion (0, 0, 0, 1));
	}

	public void UpdateRectangle ()
	{
		//enable linerenderer
		m_lineRendererList[LineRendererIndex].enabled = true;
		GameObject[] DotList = GameObject.FindGameObjectsWithTag ("marker");
		foreach (GameObject t in DotList) {
			tmpLine.Add (t.transform.position);
		}
		tmpLine.Add (DotList [0].transform.position);
		positionsOfPoints = tmpLine.ToArray ();
		m_lineRendererList[LineRendererIndex].numPositions = positionsOfPoints.Length; // add this
		m_lineRendererList[LineRendererIndex].SetPositions (positionsOfPoints);
	}
	public void placeNumber(TextMesh tmp, Vector3 numberpoint, Quaternion orientation)
	{
		tmp.text = LineRendererIndex.ToString ();
		tmp.transform.position = numberpoint;
		tmp.transform.localRotation = orientation;
		tmp.transform.localScale = new Vector3(0.1f,0.1f,0.1f);
		tmp.tag = "marker";
	}
}
