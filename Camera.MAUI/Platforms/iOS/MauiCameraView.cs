using AVFoundation;
using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace Camera.MAUI.Platforms.iOS;

internal class MauiCameraView : UIView, IAVCaptureFileOutputRecordingDelegate
{
    private CameraView cameraView;

    public MauiCameraView(CameraView cameraView)
    {
        this.cameraView = cameraView;
    }
    public void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError error)
    {
    }
}
