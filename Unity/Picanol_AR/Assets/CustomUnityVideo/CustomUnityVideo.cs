using UnityEngine;
using System.Collections;
using WebRtcCSharp;
using System.Collections.Generic;
using Byn.Common;
using Byn.Media.Native;
using Byn.Media;
using Tango;

/// <summary>
/// Simulates a specific instance of a video device. It returns images that are sent to webrtc as it would
/// come right from a webcam.
/// </summary>
public class CustomVideoCapturer : HLCustomVideoCapturer
{
	//private VideoType mWebRtcInputFormat = VideoType.kYUY2;

	private int mSourceWidth;
	private int mSourceHeight;
	private byte[] mData = null;
	public CallApp call;
	public enum CapturerState
	{
		Invalid,
		Created,
		Running,
		Stopped,
		Disposed
	}
	/// <summary>
	/// Boolean to determine whether we want to freeze the screen
	/// </summary>
	public bool holdStill = false;
	private CapturerState mState = CapturerState.Invalid;

	public CapturerState State {
		get {
			return mState;
		}
	}

	public CustomVideoCapturer ()
	{
		mState = CapturerState.Created;
	}

	public override bool Start (VideoFormat capture_format)
	{
		mSourceWidth = capture_format.width;
		mSourceHeight = capture_format.height;
		mData = new byte[capture_format.width * capture_format.height * 4];

		mState = CapturerState.Running;

		//get a first image to make sure the C++ buffer isn't filled with random values from memory
		UpdateNow ();
		Debug.Log ("debugU: Video source started. Video format requested by webrtc: " + capture_format.width + "x" + capture_format.height);

		return mState == CapturerState.Running;
	}

	public override void Stop ()
	{
		Debug.Log ("Video source stopped.");
		mState = CapturerState.Stopped;
	}

	/// <summary>
	/// Gets the newest frame and sends it out to WebRTC for processing.
	/// </summary>
	public void UnityUpdate ()
	{
		//called via unity
		if (mState == CapturerState.Running) {
			//GenerateTestImage();
			// If call.holdStill is false, we want to transmit new frames, so we keep updating the frames
			// if true, we stream the same data buffer until request to change.
			mData = VideoOverlayListener.m_previousImageBuffer.data; 
			//Debug.Log ("debugU: videoformat "+VideoOverlayListener.m_previousImageBuffer.format.ToString ());
			mSourceWidth = (int)VideoOverlayListener.m_previousImageBuffer.width;
			mSourceHeight = (int)VideoOverlayListener.m_previousImageBuffer.height;
			if (!CallApp.holdStill) {
				
			} else {
				// no need to update mData and width/height
				// we place some invisible markers

				//m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
				//m_help.newScreenCap ();
			}

//			Debug.Log("Camera = "+Camera.main.name.ToString());
//			RTImage(Camera.main);
			Debug.Log ("image dimensions sent: (h x w):" + mSourceHeight.ToString () + " x " + mSourceWidth.ToString ());

//			capturing screen working too slow, plus different encoding.
//			There has to be a better way of capturing the rendered screen, right now we're only transmitting the camera image, without the AR feedback
			//			Texture2D screenS = new Texture2D (Screen.width, Screen.height);
//			mData = screenS.GetRawTextureData ();
//			mSourceWidth = Screen.width;
//			mSourceHeight = Screen.height;
//			Debug.Log ("image dimensions sent: (h x w):" + mSourceHeight.ToString() + " x " + mSourceWidth.ToString ());

			//			if(!once){
//				ImageBufferToJPG (VideoOverlayListener.m_previousImageBuffer);
//				once = true;
//			}
			UpdateNow ();

		}
	}
	// Does not work, have to check image buffer to find out why
	void ImageBufferToJPG (TangoUnityImageData buffer)
	{
		Texture2D frame = new Texture2D ((int)buffer.width, (int)buffer.height);
		frame.LoadImage (buffer.data);
		var bytes = frame.EncodeToJPG ();
		string fileName = "buffer2JPG";
		var path = System.IO.Path.Combine (Application.persistentDataPath, fileName + ".jpg");
		System.IO.File.WriteAllBytes (path, bytes);

	}
	//	void RTImage(Camera cam) { //Texture2D
	//		RenderTexture currentRT = RenderTexture.active;
	//		RenderTexture.active = cam.targetTexture;
	//		cam.Render();
	//		Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
	//		image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
	//		image.Apply();
	//		RenderTexture.active = currentRT;
	//		mData = image.GetRawTextureData ();
	//		mSourceWidth = image.width;
	//		mSourceHeight = image.height;
	////		return image;
	//	}
	//
	/*
	private void GenerateTestImage ()
	{
		for (int y = 0; y < mSourceHeight; y++) {
			for (int x = 0; x < mSourceWidth; x++) {
                
				if (x == 0 || y == 0 || x == mSourceWidth - 1 || y == mSourceHeight - 1) {
					//red border around the image 1 pixel wide to find bugs that might
					//cut off the border
					mData [(y * mSourceWidth + x) * 4 + 0] = 0;//B
					mData [(y * mSourceWidth + x) * 4 + 1] = 0;//G
					mData [(y * mSourceWidth + x) * 4 + 2] = 255;//R
					mData [(y * mSourceWidth + x) * 4 + 3] = 255;//A
				} else {
					int ax = (x + Time.frameCount) % mSourceWidth;
					int ay = (y + Time.frameCount) % mSourceHeight;

					byte r = 0;
					byte g = 0;
					byte b = 0;

					if (between (ax, 0, 64, ay, 0, 64)) {
						r = 255;
						g = 0;
						b = 0;
					} else if (between (ax, 64, 128, ay, 0, 64)) {
						r = 0;
						g = 255;
						b = 0;
					} else if (between (ax, 0, 64, ay, 64, 128)) {
						r = 0;
						g = 0;
						b = 255;
					} else if (between (ax, 64, 128, ay, 64, 128)) {
						r = 255;
						g = 255;
						b = 255;
					}

					mData [(y * mSourceWidth + x) * 4 + 0] = b;//B
					mData [(y * mSourceWidth + x) * 4 + 1] = g;//G
					mData [(y * mSourceWidth + x) * 4 + 2] = r;//R
					mData [(y * mSourceWidth + x) * 4 + 3] = 255;//A
				}
			}
		}
	}
*/
	private bool between (int x, int minx, int maxx, int y, int miny, int maxy)
	{
		if (x > minx && x < maxx && y > miny && y < maxy)
			return true;
		return false;
	}

	private void UpdateNow ()
	{
		//delivers our test ARGB images to webrtc (very expensive operation for high res images as
		//the image needs to be converted to an internal format)

		// We  capture the frames from tango camera and send them through to CallApp. Image type is NV21, documentation of tango is not correct right now. 
		UpdateFrame (VideoType.kNV21, mData, (uint)mData.Length, mSourceWidth, mSourceHeight);
	}

	public override void Dispose ()
	{
		base.Dispose ();
	}

}

/// <summary>
/// This class hooks right into webrtc's method to create and querty video devices.
/// 
/// </summary>
class CustomVideoCapturerFactory : HLVideoCapturerFactory
{
	/// <summary>
	/// 
	/// </summary>
	private static string mDeviceName = "TangoCamera";

	/// <summary>
	/// 
	/// </summary>
	public static string DeviceName {
		get {
			return mDeviceName;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	private List<CustomVideoCapturer> mActiveCapturers = new List<CustomVideoCapturer> ();

	/// <summary>
	/// 
	/// </summary>
	/// <param name="deviceName"></param>
	/// <returns></returns>
	public override HLCustomVideoCapturer Create (string deviceName)
	{
		if (deviceName == mDeviceName) {
			var capturer = new CustomVideoCapturer ();
			mActiveCapturers.Add (capturer);
			return capturer;
		}
		return null;
	}

	/// <summary>
	/// Used to allow webrtc to request for any supported video devices.
	/// Called in WebRTC Worker thread!
	/// </summary>
	/// <returns></returns>
	public override StringVector GetVideoDevices ()
	{
		StringVector vector = new StringVector ();
		vector.Add (mDeviceName);
		return vector;
	}

	/// <summary>
	/// Destroys all video capturers + releases native pointer in the base class HLVideoCapturerFactory
	/// </summary>
	public override void Dispose ()
	{
		foreach (var v in mActiveCapturers) {
			v.Dispose ();
		}
		mActiveCapturers.Clear ();
		base.Dispose ();
	}

	/// <summary>
	/// </summary>
	public void UpdateDevices ()
	{
		foreach (var v in mActiveCapturers.ToArray()) {
			if (v.State == CustomVideoCapturer.CapturerState.Running) {
				v.UnityUpdate ();
			} else {
				//it has stopped. webrtc removed it
				Debug.Log ("Stopped. Cleanup");
				mActiveCapturers.Remove (v);
				v.Dispose ();
			}
		}
	}
}

/// <summary>
/// Unity side. It handles the initialization and updates the video factory + devices each unity update call.
/// </summary>
class CustomUnityVideo : UnitySingleton<CustomUnityVideo>
{
    

	private CustomVideoCapturerFactory factory;
	private bool mRegistered = false;

	/// <summary>
	/// 
	/// </summary>
	public void Register ()
	{
		if (mRegistered == false) {
			factory = new CustomVideoCapturerFactory ();

			//directly registers the source in the native library. This will be removed in the future
			//and replaced by a simpler + more stable system

			NativeWebRtcCallFactory nativeFactory = UnityCallFactory.Instance.InternalFactory as NativeWebRtcCallFactory;
			if (nativeFactory != null) {
				nativeFactory.NetworkFactory.NativeFactory.AddVideoCaptureFactory (factory);
				Debug.Log ("debugU: Registered");
			} else {
				Debug.LogError ("debugU: Couldn't register video factory! Only native webrtc supports custom video sources!");
			}
			mRegistered = true;
		}
	}

	public void Update ()
	{
		if (factory != null)
			factory.UpdateDevices ();
	}

	protected override void OnDestroy ()
	{
		Debug.Log ("Shutting down. All video sources will be destroyed");
		if (factory != null) {
			factory.Dispose ();
			factory = null;
		}
		base.OnDestroy ();
	}
}