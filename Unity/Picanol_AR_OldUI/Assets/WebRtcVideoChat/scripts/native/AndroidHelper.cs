using UnityEngine;
using System.Collections;

namespace Byn.Media.Android
{
    public class AndroidHelper
    {
        public static void SetSpeakerOn(bool value)
        {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

            AndroidJavaClass contextClass = new AndroidJavaClass("android.content.Context");
            AndroidJavaObject contextClass_AUDIO_SERVICE = contextClass.GetStatic<AndroidJavaObject>("AUDIO_SERVICE");

            AndroidJavaObject audioManager = context.Call<AndroidJavaObject>("getSystemService", contextClass_AUDIO_SERVICE);
            audioManager.Call("setSpeakerphoneOn", value);

        }
    }
}
