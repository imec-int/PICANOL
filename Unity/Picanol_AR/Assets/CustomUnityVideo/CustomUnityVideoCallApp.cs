using Byn.Media;
using Byn.Media.Native;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomUnityVideoCallApp : CallApp {

    private CustomVideoCapturerFactory mVideoFactory;

    protected override void Start()
    {

#if UNITY_WEBGL
        //doesn't work with WebGL
        Debug.LogError("Custom video isn't supported in the browser.");
#else

        CustomUnityVideo.Instance.Register();
#endif
		base.Start();
    }

}
