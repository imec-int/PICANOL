/* 
 * Copyright (C) 2015 Christoph Kutza
 * 
 * Please refer to the LICENSE file for license information
 */
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Text;
using System;
using Byn.Net;
using System.Collections.Generic;
using Byn.Media;
using Byn.Media.Native;
using System.IO;
using Byn.Common;
using Tango;

/// <summary>
/// This class + prefab is a complete app allowing to call another app using a shared text or password
/// to meet online.
/// 
/// It supports Audio, Video and Text chat. Audio / Video can optionally turned on/off via toggles.
/// 
/// After the join button is pressed the (first) app will initialize a native webrtc plugin 
/// and contact a server to wait for incoming connections under the given string.
/// 
/// Another instance of the app can connect using the same string. (It will first try to
/// wait for incoming connections which will fail as another app is already waiting and after
/// that it will connect to the other side)
/// 
/// The important methods are "Setup" to initialize the call class (after join button is pressed) and
/// "Call_CallEvent" which reacts to events triggered by the class.
/// 
/// Also make sure to use your own servers for production (uSignalingUrl and uStunServer).
/// 
/// NOTE: Currently, only 1 to 1 connections are supported. This will change in the future.
/// </summary>
public class CallApp : MonoBehaviour
{
	/// <summary>
	/// This is a test server. Don't use in production! The server code is in a zip file in WebRtcNetwork
	/// </summary>
	public string uSignalingUrl = "ws://because-why-not.com:12776/callapp";

	/// <summary>
	/// Mozilla stun server. Used to get trough the firewall and establish direct connections.
	/// Replace this with your own production server as well. 
	/// </summary>
	public string uIceServer = "stun:because-why-not.com:12779";

	//
	public string uIceServerUser = "";
	public string uIceServerPassword = "";

	/// <summary>
	/// Second ice server. As I can't guarantee my one is always online.
	/// </summary>
	public string uIceServer2 = "stun:stun.l.google.com:19302";

	/// <summary>
	/// Set true to use send the WebRTC log + wrapper log output to the unity log.
	/// </summary>
	public bool uLog = false;

	/// <summary>
	/// Debug console to be able to see the unity log on every platform
	/// </summary>
	public bool uDebugConsole = false;
	/*------Part for connection with other scripts*/
	/// <summary>
	/// Connection with AR camera?
	/// </summary>
	public Camera m_camera;
	/// <summary>
	/// Connection with Multipoint script
	/// </summary>
	public MultiPointToPointGUIController Mptp;


	/// <summary>
	/// The old draw mode, we only send an update when mode changes to keep draw mode synced over peers.
	/// </summary>
	private int old_draw_mode;

	/*------------------------------------------------*/

	#region UI


	[Header ("Setup panel")]
	/// <summary>
    /// Panel with the join button. Will be hidden after setup
    /// </summary>
    public RectTransform uSetupPanel;

	/// <summary>
	/// Input field used to enter the room name.
	/// </summary>
	public InputField uRoomNameInputField;
	/// <summary>
	/// Join button to connect to a server.
	/// </summary>
	public Button uJoinButton;

	public Toggle uAudioToggle;
	public Toggle uVideoToggle;
	public Dropdown uVideoDropdown;

	[Header ("Video and Chat panel")]
	public RectTransform uInCallBase;
	public RectTransform uVideoPanel;
	public RectTransform uChatPanel;

	[Header ("Default positions/transformations")]
	public RectTransform uVideoBase;
	public RectTransform uChatBase;


	[Header ("Fullscreen positions/transformations")]
	public RectTransform uFullscreenPanel;
	public RectTransform uVideoBaseFullscreen;
	public RectTransform uChatBaseFullscreen;




	[Header ("Chat panel elements")]
	/// <summary>
    /// Input field to enter a new message.
    /// </summary>
    public InputField uMessageInputField;

	/// <summary>
	/// Output message list to show incoming and sent messages + output messages of the
	/// system itself.
	/// </summary>
	public MessageList uMessageOutput;


	/// <summary>
	/// Send button.
	/// </summary>
	public Button uSendMessageButton;

	/// <summary>
	/// Shutdown button. Disconnects all connections + shuts down the server if started.
	/// </summary>
	public Button uShutdownButton;


	[Header ("Video panel elements")]
	/// <summary>
    /// Image of the local camera
    /// </summary>
    public RawImage uLocalVideoImage;

	/// <summary>
	/// Image of the remote camera
	/// </summary>
	public RawImage uRemoteVideoImage;

	[Header ("Aspect ratio fitters")]
	public AspectRatioFitter uLocalAspectRatio;
	public AspectRatioFitter uRemoteAspectRatio;

	[Header ("Resources")]
	public Texture2D uNoCameraTexture;

	#endregion

	public Microphone audio;
	/// <summary>
	/// Do not change. This length is enforced on the server side to avoid abuse.
	/// </summary>
	public const int MAX_CODE_LENGTH = 256;

	/// <summary>
	/// Call class handling all the functionality
	/// </summary>
	protected ICall mCall;

	/// <summary>
	/// Texture of the local video
	/// </summary>
	protected Texture2D mLocalVideoTexture = null;

	/// <summary>
	/// Texture of the remote video
	/// </summary>
	protected Texture2D mRemoteVideoTexture = null;

	/// <summary>
	/// Configuration of audio / video functionality
	/// </summary>
	protected MediaConfig mMediaConfig = new MediaConfig ();
	protected bool mFullscreen = true;
	//false;

	private static bool sLogSet = false;

	private bool setupDone = false;

	/// <summary>
	/// If caller = true (android phone) then we send the camera images, we are not expecting to receive any
	/// if false, then we receive camera images and can transmit coordinates to the Android phone
	///
	/// </summary>
	private bool caller = false;
	public bool MouseDown = true;
	public static bool holdStill = false;
	public int markerNumber = 0;

	protected virtual void Start ()
	{
//		Debug.Log ("debugU: Application Started: VideoChat ==> Debug ok?");
		if (uDebugConsole)
			DebugHelper.ActivateConsole ();
		if (uLog) {
			if (sLogSet == false) {
				SLog.SetLogger (OnLog);
				sLogSet = true;
				SLog.L ("Log active");
			}
		}
		setupDone = false;
		//This can be used to get the native webrtc log but causes a huge slowdown
		//only use if not webgl
//		Append (Network.player.ipAddress);
		bool nativeWebrtcLog = false;
		if (nativeWebrtcLog) {
#if UNITY_ANDROID
			//due to BUG0008 the callbacks in android doesn't work yet. use logcat instead of unity log
			Byn.Net.Native.NativeWebRtcNetworkFactory.SetNativeDebugLog (WebRtcCSharp.LoggingSeverity.LS_INFO);
#elif (!UNITY_WEBGL || UNITY_EDITOR)
			caller = false;
			Byn.Net.Native.NativeWebRtcNetworkFactory.LogNative (WebRtcCSharp.LoggingSeverity.LS_INFO);
#else
			caller = false;
            //webgl. logging isn't supported here and has to be done via the browser.
            Debug.LogWarning("Platform doesn't support native webrtc logging.");
#endif
		}

		#if UNITY_ANDROID
		caller = true;
		#else
			caller = false;
		#endif
//		Append ("caller = " + caller.ToString ());
		if (UnityCallFactory.Instance == null) {
			Debug.LogError ("UnityCallFactory failed to initialize");
		}

		//use video and audio by default (the UI is toggled on by default as well it will change on click )
		mMediaConfig.Audio = uAudioToggle.isOn;
		mMediaConfig.Video = uVideoToggle.isOn;
		
		//keep the resolution low.
		//This helps avoiding problems with very weak CPU's and very high resolution cameras
		//(apparently a problem with win10 tablets)
		mMediaConfig.MinWidth = 160;
		mMediaConfig.MinHeight = 120;
		mMediaConfig.MaxWidth = 1920;
		mMediaConfig.MaxHeight = 1080;
		mMediaConfig.IdealWidth = 1080;
		mMediaConfig.IdealHeight = 1920;
		mMediaConfig.Audio = true;
		
		SetGuiState (true);
		//fill the video dropbox
		UpdateVideoDropdown ();
		JoinButtonPressed ();
		restrictions ();
	}


	public static void OnLog (object msg, string[] tags)
	{
		StringBuilder builder = new StringBuilder ();
		builder.Append ("TAGS:[");
		foreach (var v in tags) {
			builder.Append (v);
			builder.Append (",");
		}
		builder.Append ("]");
		builder.Append (msg);
		Debug.Log (builder.ToString ());
	}

	private IEnumerator restrictions ()
	{
	

		yield return Application.RequestUserAuthorization (UserAuthorization.WebCam | UserAuthorization.Microphone);
		if (Application.HasUserAuthorization (UserAuthorization.WebCam | UserAuthorization.Microphone)) {
			Debug.Log ("allowed");
		} else {
			Debug.Log ("not allowed");
		}
	}

	/// <summary>
	/// Creates the call object and uses the configure method to activate the 
	/// video / audio support if the values are set to true.
	/// </summary>
	/// generating new frames after this call so the user can see himself before
	/// the call is connected.</param>
	protected virtual void Setup ()
	{
		Append ("Setting up ...");
		//
		//setup the server
		NetworkConfig netConfig = new NetworkConfig ();
		netConfig.IceServers.Add (new IceServer (uIceServer, uIceServerUser, uIceServerPassword));
		netConfig.SignalingUrl = uSignalingUrl;
		mCall = UnityCallFactory.Instance.Create (netConfig);
		if (mCall == null) {
			Append ("Failed to create the call");
			return;
		}
		string[] devices = UnityCallFactory.Instance.GetVideoDevices ();
		if (devices == null || devices.Length == 0) {
			Debug.Log ("no device found or no device information available");
			Append ("no device found or no device information available");
		} else {
			foreach (string s in devices) {
				Debug.Log ("device found: " + s);
//				Append ("device found: " + s);
			}
		}




		Append ("Call created!");
		mCall.CallEvent += Call_CallEvent;

		if (caller) {
			//if (uVideoDropdown.options.Count == 4) {
			mMediaConfig.VideoDeviceName = uVideoDropdown.options [4].text;
			//}
		}	

		//Debug.Log ("debugU: New video device selected: " + mMediaConfig.VideoDeviceName.ToString ());
		//Debug.Log ("debugU: mMediaConfig " + mCall.ToString ());
		mCall.Configure (mMediaConfig);
		
		setupDone = true;
		SetGuiState (false);
	}

	/// <summary>
	/// Destroys the call object and shows the setup screen again.
	/// Called after a call ends or an error occurred.
	/// </summary>
	protected virtual void Reset ()
	{
		CleanupCall ();
		SetGuiState (true);
	}

	/// <summary>
	/// Handler of call events.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	protected void Call_CallEvent (object sender, CallEventArgs e)
	{
		
		switch (e.Type) {
		case CallEventType.CallAccepted:
                //Outgoing call was successful or an incoming call arrived
			Append ("Connection established");
			break;
		case CallEventType.CallEnded:
                //Call was ended / one of the users hung up -> reset the app
			Append ("Call ended");
			Reset ();
			break;
		case CallEventType.ListeningFailed:
                //listening for incoming connections failed
                //this usually means a user is using the string / room name already to wait for incoming calls
                //try to connect to this user
                //(note might also mean the server is down or the name is invalid in which case call will fail as well)
			EnsureLength ();
			mCall.Call (uRoomNameInputField.text);
			break;

		case CallEventType.ConnectionFailed:
			{
				Byn.Media.ErrorEventArgs args = e as Byn.Media.ErrorEventArgs;
				Append ("Connection failed error: " + args.ErrorMessage);
				Reset ();
			}
			break;
		case CallEventType.ConfigurationFailed:
			{
				Byn.Media.ErrorEventArgs args = e as Byn.Media.ErrorEventArgs;
				Append ("Configuration failed error: " + args.ErrorMessage);
				Reset ();
			}
			break;

		case CallEventType.FrameUpdate:
                //new frame received from webrtc (either from local camera or network)
			//if (!caller)
			UpdateFrame (e as FrameUpdateEventArgs);
			break;
		case CallEventType.Message:
			{
				//text message received
				MessageEventArgs args = e as MessageEventArgs;
				switch (args.Content.Substring (0, Math.Min (3, e.ToString ().Length))) {
				case "mou":
					Debug.Log ("mouse received! " + args.Content);
					mouse_update (args.Content.Substring (7));
					break;
				case "dra":
					Mptp.draw_mode = Convert.ToInt16 (args.Content.Substring (5));
					old_draw_mode = Mptp.draw_mode;
					Debug.Log ("New Draw mode:" + Mptp.draw_mode.ToString ());
					break;
				case "cle":
					Mptp.m_help.ClearPoints (new string[]{ "circle", "marker", "marker_invisible" });
					break;
				case "Mou":
					Debug.Log ("Mouse received" + e.ToString ());
					if (args.Content.EndsWith ("up"))
						Mptp.MouseUp = true;
					else
						Mptp.MouseUp = false;
					break;
				case "Hol":
					Append (args.Content.ToString ());
					if (args.Content.EndsWith ("alse"))
						holdStill = false;
					else
						holdStill = true;
					Debug.Log ("request to hold still?" + holdStill.ToString ());
					break;
				case "add":
					Debug.Log (args.Content.ToString ());
					addInfo (args.Content.ToString ().Substring (9));
					break;
				case "pla":
					Debug.Log (args.Content.ToString ());
					break;
				default:
					Append (args.Content);
					break;
				}

				break;
			}
		case CallEventType.WaitForIncomingCall:
			{
				//the chat app will wait for another app to connect via the same string
				WaitForIncomingCallEventArgs args = e as WaitForIncomingCallEventArgs;
				Append ("Waiting for incoming call address: " + args.Address);
				break;
			}
		}

	}

	/// <summary>
	/// Send mouse coordinates of remote assistant to the tango environment
	/// </summary>
	/// <param name="mouseLoc">Mouse location.</param>
	void mouse_update (string mouseLoc)
	{
		String[] mouseSplit = mouseLoc.Split (';');
		float x_coor = Convert.ToSingle (mouseSplit [0]);
		float y_coor = Convert.ToSingle (mouseSplit [1]);
		markerNumber = Convert.ToInt32 (mouseSplit [2]);
//		Append ("x_coor =" + x_coor.ToString () + " y_coor =" + y_coor.ToString () + " markernumber " + markerNumber.ToString ());
		Mptp.RemoteUpdate (x_coor, y_coor, markerNumber);
	}

	void addInfo (string extraInfo)
	{
		String[] InfoSplit = extraInfo.Split (';');
		string info = InfoSplit [0];
		markerNumber = Convert.ToInt32 (InfoSplit [1]);
//		Append ("addInfo " + info + " marker: " + markerNumber.ToString ());
		Mptp.AddInfoMarker [markerNumber] = info;
	}

	/// <summary>
	/// Updates the texture based on the given frame update.
	/// 
	/// Returns true if a complete new texture was created
	/// </summary>
	/// <param name="tex"></param>
	/// <param name="frame"></param>
	protected bool UpdateTexture (ref Texture2D tex, RawFrame frame)
	{
		bool newTextureCreated = false;
		//texture exists but has the wrong height /width? -> destroy it and set the value to null
		if (tex != null && (tex.width != frame.Width || tex.height != frame.Height)) {
			Texture2D.Destroy (tex);
			tex = null;
		}
		//no texture? create a new one first
		if (tex == null) {
			newTextureCreated = true;
			Debug.Log ("Creating new texture with resolution " + frame.Width + "x" + frame.Height + " Format:" + mMediaConfig.Format);
			if (mMediaConfig.Format == FramePixelFormat.ABGR) {
				tex = new Texture2D (frame.Width, frame.Height, TextureFormat.RGBA32, false);
			} else {
				tex = new Texture2D (frame.Width, frame.Height, TextureFormat.YUY2, false);
			}

			tex.wrapMode = TextureWrapMode.Clamp;
		}
		///copy image data into the texture and apply
		tex.LoadRawTextureData (frame.Buffer);
		tex.Apply ();
		return newTextureCreated;
	}


	/// <summary>
	/// Updates the local video. If the frame is null it will hide the video image
	/// </summary>
	/// <param name="frame"></param>
	protected virtual void UpdateLocalTexture (RawFrame frame)
	{
		//if(caller)VideoOverlayProvider.UpdateARScreen(TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR);
//		if (caller) 
		//else{
		if (uLocalVideoImage != null) {
			if (frame != null) {

				bool changed = UpdateTexture (ref mLocalVideoTexture, frame);
				uLocalVideoImage.texture = mLocalVideoTexture;
				if (uLocalVideoImage.gameObject.activeSelf == false) {
					uLocalVideoImage.gameObject.SetActive (true);

				}
				if (changed) {
					uLocalAspectRatio.aspectRatio = mLocalVideoTexture.width / (float)mLocalVideoTexture.height;
					uLocalVideoImage.transform.rotation = Quaternion.Euler (00f, 90f, 0f);
				}
			} else {
				uLocalVideoImage.texture = null;
				uLocalVideoImage.gameObject.SetActive (false);
			}
		}
//
//		}
	}

	/// <summary>
	/// Updates the remote video. If the frame is null it will hide the video image.
	/// </summary>
	/// <param name="frame"></param>
	protected virtual void UpdateRemoteTexture (RawFrame frame)
	{
//		if (!caller) {
		if (uRemoteVideoImage != null) {
			if (frame != null) {
				bool changed = UpdateTexture (ref mRemoteVideoTexture, frame);
				uRemoteVideoImage.texture = mRemoteVideoTexture;
				//TODO camera images entered rotated, fix this with camera intrinsics? 
				// Only necessary on receiving app, not android app
				uRemoteVideoImage.transform.rotation = Quaternion.Euler (00f, 00f, 180f);
				if (changed) {
					uRemoteAspectRatio.aspectRatio = mRemoteVideoTexture.width / (float)mRemoteVideoTexture.height;
				}
			} else {
				uRemoteVideoImage.texture = uNoCameraTexture;
			}
		}
//		}
	}

	protected virtual void UpdateFrame (FrameUpdateEventArgs frameUpdateEventArgs)
	{
		//the avoid wasting CPU time the library uses the format returned by the browser -> ABGR little endian thus
		//the bytes are in order R G B A
		//Unity seem to use this byte order but also flips the image horizontally (reading the last row first?)
		//this is reversed using UI to avoid wasting CPU time

		if (frameUpdateEventArgs.IsRemote == false) {
			UpdateLocalTexture (frameUpdateEventArgs.Frame);
		} else {
			UpdateRemoteTexture (frameUpdateEventArgs.Frame);
		}
	}

	/// <summary>
	/// Destroys the call. Used if unity destroys the object or if a call
	/// ended / failed due to an error.
	/// 
	/// </summary>
	protected virtual void CleanupCall ()
	{
		if (mCall != null) {

			Debug.Log ("Destroying call!");
			mCall.CallEvent -= Call_CallEvent;
			mCall.Dispose ();
			mCall = null;
			//call the garbage collector. This isn't needed but helps to discover memory issues early on
			GC.Collect ();
			GC.WaitForPendingFinalizers ();
			Debug.Log ("Call destroyed");
		}
	}

	private void OnDestroy ()
	{
		CleanupCall ();
		setupDone = false;
	}
	//private bool mSpeaker = false;

	private void OnGUI ()
	{
		//GUILayout.BeginArea(new Rect(0, 100, 500, 500));
		//if (GUILayout.Button("Test"))
		//{
		//    mSpeaker = !mSpeaker;
		//    Byn.Media.Android.AndroidHelper.SetSpeakerOn(mSpeaker);
		//    Debug.Log("Set speaker to " + mSpeaker);
		//}
		//GUILayout.EndArea();
		//draws the debug console (or the show button in the corner to open it)
		//DebugHelper.DrawConsole ();

		
	}

	/// <summary>
	/// toggle audio on / off
	/// </summary>
	/// <param name="state"></param>
	public void AudioToggle (bool state)
	{
		mMediaConfig.Audio = state; 
	}

	/// <summary>
	/// toggle video on / off
	/// </summary>
	/// <param name="state"></param>
	public void VideoToggle (bool state)
	{
		mMediaConfig.Video = state;
		UpdateVideoDropdown ();
	}

	/// <summary>
	/// Updates the dropdown menu based on the current video devices and toggle status
	/// </summary>
	private void UpdateVideoDropdown ()
	{
		uVideoDropdown.ClearOptions ();
		if (UnityCallFactory.Instance.CanSelectVideoDevice ()) {
			List<string> devices = new List<string> ();
			string[] videoDevices = UnityCallFactory.Instance.GetVideoDevices ();
			devices.Add ("Any");
			devices.AddRange (videoDevices);
			uVideoDropdown.AddOptions (devices);
			uVideoDropdown.interactable = uVideoToggle.isOn;
		} else {
			uVideoDropdown.AddOptions (new List<string> (new string[] { "Default" }));
			uVideoDropdown.interactable = false;
		}
		
	}

	public void VideoDropdownOnValueChanged (int index)
	{
		if (index <= 0) {
			mMediaConfig.VideoDeviceName = null;
		} else {
//			Debug.Log ("debugU: Videodevice int= " + index);
			mMediaConfig.VideoDeviceName = uVideoDropdown.options [index].text;
			Debug.Log ("New video device selected: " + mMediaConfig.VideoDeviceName);
		}
	}

	/// <summary>
	/// Adds a new message to the message view
	/// </summary>
	/// <param name="text"></param>
	public void Append (string text)
	{
		if (uMessageOutput != null) {
			uMessageOutput.AddTextEntry (text);
		}
		Debug.Log ("Chat output: " + text);
	}

	public void Fullscreen ()
	{

		bool newValues = !mFullscreen;

		//just in case: make sure fullscreen button is ignored if in setup mode
		if (newValues == true && uSetupPanel.gameObject.activeSelf)
			return;
		SetFullscreen (newValues);
	}

	private void SetFullscreen (bool value)
	{
		mFullscreen = value;
		if (mFullscreen) {
			uVideoPanel.SetParent (uVideoBaseFullscreen, false);
			uChatPanel.SetParent (uChatBaseFullscreen, false);
			uInCallBase.gameObject.SetActive (false);
			uFullscreenPanel.gameObject.SetActive (true);
		} else {
			uVideoPanel.GetComponent<RectTransform> ().SetParent (uVideoBase, false);
			uChatPanel.GetComponent<RectTransform> ().SetParent (uChatBase, false);
			uInCallBase.gameObject.SetActive (true);
			uFullscreenPanel.gameObject.SetActive (false);
		}
	}

	/// <summary>
	/// The call object needs to be updated regularly to sync data received via webrtc with
	/// unity. All events will be triggered during the update method in the unity main thread
	/// to avoid multi threading errors
	/// </summary>
	protected virtual void Update ()
	{
		if (mCall != null) {
			//update the call
			mCall.Update ();
		}
		if (setupDone) {
			if (Mptp.draw_mode == 1) {
				if (Input.GetMouseButtonDown (0) & setupDone) {
					if (!MouseDown) {
						// if first pressed we send message that mouse is down to start drawing new line renderer
						MouseDown = true;
						SendMsg (uMessageInputField.text);
						//SendMsg ("MouseDown");
					}
//					SendMsg ("mouse test");
//					SendMsg ("player: tester");
					//SendMsg ("mouse: " + Input.mousePosition.x.ToString () + ";" + Input.mousePosition.y.ToString ()); // (1440 - mouse.y) to be correct.

				}
				if (Input.GetMouseButtonUp (0) & setupDone & !Mptp.clearSignal) {

				//	SendMsg ("mouse: " + Input.mousePosition.x.ToString () + ";" + Input.mousePosition.y.ToString ()); // (1440 - mouse.y) to be correct.
					MouseDown = false;
				//	SendMsg ("MouseUp");
				}
			}
			if (Mptp.draw_mode != old_draw_mode) {
				// If this entity changes its draw mode, we send message to other peer
				SendMsg ("draw:" + Mptp.draw_mode);
				old_draw_mode = Mptp.draw_mode;
			}

			if (Mptp.clearSignal) {
				SendMsg ("clear");
				Mptp.clearSignal = false;
			}
		}
	}

	#region UI

	/// <summary>
	/// Shows the setup screen or the chat + video
	/// </summary>
	/// <param name="showSetup">true Shows the setup. False hides it.</param>
	private void SetGuiState (bool showSetup)
	{
		uSetupPanel.gameObject.SetActive (showSetup);

		uSendMessageButton.interactable = !showSetup;
		uShutdownButton.interactable = !showSetup;
		uMessageInputField.interactable = !showSetup;

		//this is going to hide the textures until it is updated with a new frame update
		UpdateLocalTexture (null);
		UpdateRemoteTexture (null);
		SetFullscreen (false);
	}

	/// <summary>
	/// Join button pressed. Tries to join a room.
	/// </summary>
	public void JoinButtonPressed ()
	{
		Setup ();
		EnsureLength ();
		Append ("Trying to listen on address " + uRoomNameInputField.text);
		mCall.Listen (uRoomNameInputField.text);
	}

	private void EnsureLength ()
	{
		if (uRoomNameInputField.text.Length > MAX_CODE_LENGTH) {
			uRoomNameInputField.text = uRoomNameInputField.text.Substring (0, MAX_CODE_LENGTH);
		}
	}

	/// <summary>
	/// This is called if the send button
	/// </summary>
	public void SendButtonPressed ()
	{
		//get the message written into the text field
		string msg = uMessageInputField.text;
//			Append ("playerSend: " + uMessageInputField.text + msg + uRoomNameInputField.text);
		SendMsg ("player: " + msg);
	}

	/// <summary>
	/// User either pressed enter or left the text field
	/// -> if return key was pressed send the message
	/// </summary>
	public void InputOnEndEdit ()
	{
		if (Input.GetKey (KeyCode.Return)) {
			string msg = uMessageInputField.text;
			Append ("PlayerInput: " + msg);

			SendMsg ("player: " + uMessageInputField.text);
		}
	}

	/// <summary>
	/// Sends a message to the other end
	/// </summary>
	/// <param name="msg"></param>
	private void SendMsg (string msg)
	{
//		Append ("antwoord: "+uMessageInputField.text+msg);
		if (String.IsNullOrEmpty (msg)) {
			//never send null or empty messages. webrtc can't deal with that
			msg = uMessageInputField.text;
			Append (msg);
			return;
		}
			Debug.LogWarning ("SendMSG: " + msg + " input: "+ uMessageInputField.text);
		switch (msg.Substring (0, Math.Min (3, msg.Length))) {
		case "mou":
			Debug.Log ("Remote Mouse received! " + msg);
			break;
		case "dra":
			Debug.Log (msg);
			break;
		case "cle":
			Debug.Log ("Points cleared");
			break;
		case "Mou":
			Debug.Log (msg);
			break;
		case "pla":
			Debug.Log (msg);
			//Append (msg);

//			Append (msg);
			break;
		default:
			Debug.Log (uMessageInputField.text);
		//	Append (uMessageInputField.text);

//			Append (msg);
			break;
		}
		mCall.Send (msg);
		//reset UI
		uMessageInputField.text = "";
		uMessageInputField.Select ();
	}



	/// <summary>
	/// Shutdown button pressed. Shuts the network down.
	/// </summary>
	public void ShutdownButtonPressed ()
	{
		Reset ();
	}

	#endregion
}
