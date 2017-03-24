﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Byn.Media.Browser
{
    public class BrowserWebRtcCall : AWebRtcCall
    {
        private NetworkConfig mConfig;

        public BrowserWebRtcCall(NetworkConfig config) :
            base(config)
        {
            mConfig = config;
            Initialize(CreateNetwork());
        }

        private IMediaNetwork CreateNetwork()
        {
            return new BrowserMediaNetwork(mConfig.SignalingUrl, mConfig.GetIceServersOrDefault());
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {

                //Dispose was called by the user -> cleanup other managed objects

                //cleanup the internal network
                if (mNetwork != null)
                    mNetwork.Dispose();
                mNetwork = null;

                //unregister on network factory to allow garbage collection
                //if (this.mFactory != null)
                //    this.mFactory.OnCallDisposed(this);
                //this.mFactory = null;
            }
        }
    }
}
