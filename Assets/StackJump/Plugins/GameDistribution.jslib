// Bridge to the GameDistribution SDK loaded by the gamedistribution WebGL template.
// Every SDK event and ad outcome is sent to Platform as a string via PlatformRunner.GdEvent.
mergeInto(LibraryManager.library, {
  SJ_GD_Init: function () {
    window.sjGdSend = function (name) {
      SendMessage("PlatformRunner", "GdEvent", name);
    };
    var queued = window.sjGdQueue || [];
    window.sjGdQueue = [];
    for (var i = 0; i < queued.length; i++) window.sjGdSend(queued[i]);
    // The template wasn't used (e.g. a local test page): report no SDK.
    if (!window.sjGdEvent) window.sjGdSend("SJ_SDK_FAILED");
  },

  SJ_GD_ShowAd: function (rewarded) {
    var send = window.sjGdSend;
    try {
      if (typeof gdsdk === "undefined" || !gdsdk.showAd) { send("SJ_AD_FAILED"); return; }
      var p = rewarded ? gdsdk.showAd("rewarded") : gdsdk.showAd();
      if (!p || !p.then) { send("SJ_AD_DONE"); return; }
      p.then(function () { send("SJ_AD_DONE"); })
       .catch(function () { send("SJ_AD_FAILED"); });
    } catch (e) {
      send("SJ_AD_FAILED");
    }
  },

  SJ_GD_PreloadRewarded: function () {
    var send = window.sjGdSend;
    try {
      if (typeof gdsdk === "undefined" || !gdsdk.preloadAd) { send("SJ_REWARDED_NONE"); return; }
      gdsdk.preloadAd("rewarded")
        .then(function () { send("SJ_REWARDED_READY"); })
        .catch(function () { send("SJ_REWARDED_NONE"); });
    } catch (e) {
      send("SJ_REWARDED_NONE");
    }
  },
});
