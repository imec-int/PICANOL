using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using Byn.Media;
using Byn.Net;
using System;
using System.Text;

namespace Byn.Media.Browser
{
    public class BrowserMediaNetwork : BrowserWebRtcNetwork, IMediaNetwork
    {

        #region CAPI imports
        [DllImport("__Internal")]
        public static extern bool UnityMediaNetwork_IsAvailable();

        [DllImport("__Internal")]
        public static extern int UnityMediaNetwork_Create(string lJsonConfiguration);

        [DllImport("__Internal")]
        public static extern void UnityMediaNetwork_Configure(int lIndex, bool audio, bool video,
            int minWidth, int minHeight, int maxWidth, int maxHeight, int idealWidth, int idealHeight);

        [DllImport("__Internal")]
        public static extern int UnityMediaNetwork_GetConfigurationState(int lIndex);


        [DllImport("__Internal")]
        public static extern string UnityMediaNetwork_GetConfigurationError(int lIndex);


        [DllImport("__Internal")]
        public static extern void UnityMediaNetwork_ResetConfiguration(int lIndex);


        [DllImport("__Internal")]
        public static extern bool UnityMediaNetwork_TryGetFrame(int lIndex, int connectionId, int[] lWidth, int[] lHeight, byte[] lBuffer, int offset, int length);


        [DllImport("__Internal")]
        public static extern int UnityMediaNetwork_TryGetFrameDataLength(int lIndex, int connectionId);

        #endregion


        public BrowserMediaNetwork(string signalingUrl, IceServer[] iceUrls)
        {

            //TODO: change this to avoid the use of json

            StringBuilder iceUrlsString = new StringBuilder();
            string[] urls = iceUrls[0].Urls.ToArray();
            if (iceUrls != null && iceUrls.Length > 0)
            {
                iceUrlsString.Append("\"");
                iceUrlsString.Append(urls[0]);
                iceUrlsString.Append("\"");

                for(int i = 1; i < iceUrls.Length; i++)
                {
                    iceUrlsString.Append(",");
                    iceUrlsString.Append("\"");
                    iceUrlsString.Append(urls[i]);
                    iceUrlsString.Append("\"");
                }
            }

            string defaultConfig = "{\"IceUrls\":[" + iceUrlsString.ToString() + "], \"SignalingUrl\":\""+ signalingUrl + "\"}";
            Debug.Log("Creating unity media network with configuration: " + defaultConfig);
            mReference = UnityMediaNetwork_Create(defaultConfig);
        }

        public static new bool IsAvailable()
        {
            try
            {
                Debug.Log("Check availability via UnityMediaNetwork_IsAvailable");

                //js side will check if all needed functions are available and if the browser is supported
                return UnityMediaNetwork_IsAvailable();
            }
            catch (EntryPointNotFoundException)
            {
                //not available at all
                return false;
            }
        }
        private static bool sInjectionTried = false;
        static public new void InjectJsCode()
        {

            //use sInjectionTried to block multiple calls.
            if (Application.platform == RuntimePlatform.WebGLPlayer && sInjectionTried == false)
            {
                sInjectionTried = true;
                Debug.Log("injecting webrtcvideochatplugin");
                TextAsset txt = Resources.Load<TextAsset>("webrtcvideochatplugin");
                if (txt == null)
                {
                    Debug.LogError("Failed to find webrtcvideochatplugin.txt in Resource folder. Can't inject the JS plugin!");
                    return;
                }
                Application.ExternalEval(txt.text);
            }
        }
        public void Configure(MediaConfig config)
        {
            UnityMediaNetwork_Configure(mReference,
                config.Audio, config.Video,
                config.MinWidth, config.MinHeight,
                config.MaxWidth, config.MaxHeight,
                config.IdealWidth, config.IdealHeight);
        }

        public RawFrame TryGetFrame(ConnectionId id)
        {
            int length = UnityMediaNetwork_TryGetFrameDataLength(mReference, id.id);
            if (length < 0)
                return null;

            int[] width = new int[1];
            int[] height = new int[1];
            byte[] buffer = new byte[length];

            bool res = UnityMediaNetwork_TryGetFrame(mReference, id.id, width, height, buffer, 0, buffer.Length);
            if (res)
                return new RawFrame(buffer, width[0], height[0]);
            return null;
        }

        public MediaConfigurationState GetConfigurationState()
        {
            int res = UnityMediaNetwork_GetConfigurationState(mReference);
            MediaConfigurationState state = (MediaConfigurationState)res;
            return state;
        }
        public override void Update()
        {
            base.Update();

        }
        public string GetConfigurationError()
        {
            if(GetConfigurationState() == MediaConfigurationState.Failed)
            {
                return "An error occurred while requesting Audio/Video features. Check the browser log for more details.";
            }else
            {
                return null;
            }

        }

        public void ResetConfiguration()
        {
            UnityMediaNetwork_ResetConfiguration(mReference);
        }
    }
}