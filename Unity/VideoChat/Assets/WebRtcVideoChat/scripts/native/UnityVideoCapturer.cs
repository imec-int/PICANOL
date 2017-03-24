using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebRtcCSharp;
using System.Collections;
using System.Threading;
using UnityEngine;
using Byn.Common;

namespace Byn.Media.Native
{
    class UnityVideoCapturer : HLCustomVideoCapturer
    {

        private bool mRunning = false;
        public bool Running
        {
            get
            {
                return mRunning;
            }

            set
            {
                mRunning = value;
            }
        }
        private int mWidth;
        private int mHeight;
        private byte[] mImageBuffer = null;

        private string mName = null;


        private WebCamTexture mTexture = null;

        public UnityVideoCapturer(string name = null)
        {
            mName = name;
            SLog.L("UnityVideoCapturerer " + name + " created", UnityVideoCapturerFactory.LOGTAG);
        }
        //called by webrtc
        public override bool Start(VideoFormat capture_format)
        {
			UpdateBufferSize (capture_format.width, capture_format.height);
			UpdateFrame(VideoType.kARGB, mImageBuffer, (uint)mImageBuffer.Length, mWidth, mHeight);

            mRunning = true;
            SLog.L("UnityVideoCapturerer " + mName + " started", UnityVideoCapturerFactory.LOGTAG);
            return true;
        }

		private void UpdateBufferSize(int width, int height)
		{
			mWidth = width;
			mHeight = height;
			if(mImageBuffer == null || mImageBuffer.Length != (width * height * 4))
			{
				mImageBuffer = new byte[width * height * 4];
				for (int i = 0; i < mImageBuffer.Length; i++)
					mImageBuffer[i] = 255;
			}
		}

        //called by webrtc
        public override void Stop()
        {
            mRunning = false;
            SLog.L("UnityVideoCapturerer " + mName + " stopped", UnityVideoCapturerFactory.LOGTAG);
        }

        //
        private void SetupUnityCamera()
        {
			SLog.L("UnityVideoCapturerer " + mName + " unity setup " + mWidth + "x" + mHeight, UnityVideoCapturerFactory.LOGTAG);
			mTexture = new WebCamTexture(mName, mWidth, mHeight, 15);
            mTexture.Play();
        }


        //called in unity thread but will call to webrtc
        public void TriggerUpdate()
        {
            if (mRunning)
            {
                //we create during the first update to make sure everything using unity objects is done in the
                //main thread
                if (mTexture == null && mName != null)
                {
                    SetupUnityCamera();
                }

                if (mTexture == null)
                {
                    //test image.
                    DeliverTestImage();
                }
                else
                {
                    DeliverImage();
                }
            }
        }
        //called in unity thread but will itself call to webrtc (locking here could lead to a deadlock!)
        private void DeliverImage()
        {
            if (mTexture.didUpdateThisFrame)
            {
                Color32[] colors = mTexture.GetPixels32();
				UpdateBufferSize (mTexture.width, mTexture.height);
				if (colors.Length == mImageBuffer.Length / 4)
                {
					bool unityFlip = false;

					//unity flips texture data on some platforms (might be flipped twice currently?)
					//this should be resolved entirely on the GPU in the future
					if (Application.platform == RuntimePlatform.OSXEditor ||
					   Application.platform == RuntimePlatform.OSXPlayer) {
						unityFlip = true;
					}

					if (unityFlip)
					{
						for (int y = 0; y < mHeight; y++)
						{
							for (int x = 0; x < mWidth; x++)
							{
								int pixelDst = y * mWidth + x;
								int pixelSrc = (mHeight - 1 - y) * mWidth + x;
								mImageBuffer[pixelDst * 4 + 0] = colors[pixelSrc].b;
								mImageBuffer[pixelDst * 4 + 1] = colors[pixelSrc].g;
								mImageBuffer[pixelDst * 4 + 2] = colors[pixelSrc].r;
								mImageBuffer[pixelDst * 4 + 3] = colors[pixelSrc].a;
							}
						}
					} else {
						for (int i = 0; i < colors.Length; i++)
						{
							mImageBuffer[i * 4 + 0] = colors[i].b;
							mImageBuffer[i * 4 + 1] = colors[i].g;
							mImageBuffer[i * 4 + 2] = colors[i].r;
							mImageBuffer[i * 4 + 3] = colors[i].a;
						}
					}
					UpdateFrame(VideoType.kARGB, mImageBuffer, (uint)mImageBuffer.Length, mWidth, mHeight);

                }
                else
                {
					Debug.LogError("Skipped frame. invalid buffer length: " + mImageBuffer.Length + " expected " + colors.Length * 4);
                }
            }
            else
            {
                //skip frame
            }
        }

        private void DeliverTestImage()
        {
			for (int i = 0; i < mImageBuffer.Length; i++)
				mImageBuffer[i] = (byte)(mImageBuffer[i] + i);
            //UpdateFrame(VideoType.kARGB, arr, (uint)arr.Length, width, height);
        }

        public override void Dispose()
        {
            SLog.L("UnityVideoCapturerer disposing", UnityVideoCapturerFactory.LOGTAG);
            if (mTexture != null)
            {
                mTexture.Stop();
            }
            base.Dispose();
            SLog.L("UnityVideoCapturerer disposed", UnityVideoCapturerFactory.LOGTAG);
        }
    }

    /// <summary>
    /// Called by native code to list devices and create new video devices.
    /// </summary>
    class UnityVideoCapturerFactory : HLVideoCapturerFactory
    {
        public static readonly string LOGTAG = "UnityVideoCapturerFactory";
        public static List<UnityVideoCapturer> mActiveCapturers = new List<UnityVideoCapturer>();

        public static readonly string sTestDeviceName = "CSharpTestDevice";

        public static readonly string sDevicePrefix = "Unity_";

        //used to make sure nothing is accessed by webrtc and unity at the same time. Also used for UnityVideoCapturer
        public static readonly object sUnityLock = new object();

        //called from native code
        public override HLCustomVideoCapturer Create(string deviceName)
        {
            try
            {
                if (deviceName == sTestDeviceName)
                {
                    var v = new UnityVideoCapturer();
                    lock (mActiveCapturers)
                    {
                        mActiveCapturers.Add(v);
                    }
                    return v;
                }
                foreach (var device in WebCamTexture.devices)
                {
					Debug.Log("debugU: WebcamTextures ="+device.name.ToString());
                    if (deviceName == (sDevicePrefix + device.name))
                    {
                        Debug.Log("Creating new unity video capturer: " + device.name);
                        var v = new UnityVideoCapturer(device.name);
                        if (v != null)
                        {
                            lock (mActiveCapturers)
                            {
                                mActiveCapturers.Add(v);
                            }
                            return v;
                        }
                    }
                }

            }
            catch (Exception e)
            {
                SLog.LogException(e, LOGTAG);
            }
            return null;
        }
        //called from native code
        public override StringVector GetVideoDevices()
        {
            StringVector vector = new StringVector();
            try
            {

                foreach (var v in WebCamTexture.devices)
                {
                    if (v.isFrontFacing)
                    {
                        string deviceName = sDevicePrefix + v.name;
                        vector.Add(deviceName);
                    }
                }
                foreach (var v in WebCamTexture.devices)
                {
                    if (v.isFrontFacing == false)
                    {
                        string deviceName = sDevicePrefix + v.name;
                        vector.Add(deviceName);
                    }
                }
                vector.Add(sTestDeviceName);

            }
            catch (Exception e)
            {
                SLog.LogException(e, LOGTAG);
            }
            return vector;
        }

        public void Update()
        {
            UnityVideoCapturer[] capturers = null;
            lock (mActiveCapturers)
            {
                capturers = mActiveCapturers.ToArray();
            }
            foreach (var v in capturers)
            {
                if (v.Running)
                {
                    v.TriggerUpdate();
                }
                else
                {
                    lock (mActiveCapturers)
                    {
                        mActiveCapturers.Remove(v);
                    }
                    v.Dispose();
                }
            }

        }
    }

}

