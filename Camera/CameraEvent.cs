using OpenCVForUnity.CoreModule;
using System;
using UnityEngine;

public class CameraInitEventArgs : EventArgs
{
    public CameraInitEventArgs(Mat _cam, Mat _inv, Mat _dist, int _w, int _h, float _scale_w, float _scale_h, float _cropped, float _scaled)
    {
        camMat = _cam;
        invCamMat = _inv;
        distCoeffs = _dist;

        width = _w;
        height = _h;
        widthScale = _scale_w;
        heightScale = _scale_h;
        fHeightCropped = _cropped;
        fImageToScreen = _scaled;
    }
    public Mat camMat { get; set; }
    public Mat invCamMat { get; set; }
    public Mat distCoeffs { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public float widthScale { get; set; }
    public float heightScale { get; set; }
    //터치에서 이미지로 변환
    public float fHeightCropped { get; set; }
    //이미지에서 터치 스크린으로 변환
    public float fImageToScreen { get; set; }
}

class CameraInitEvent
{
    public static event EventHandler<CameraInitEventArgs> camInitialized;
    public static void RunEvent(CameraInitEventArgs e)
    {
        if (camInitialized != null)
        {
            camInitialized(null, e);
        }
    }
}
public class ImageCatchEventArgs : EventArgs
{
    public ImageCatchEventArgs(ref Mat _rgb, int _id)
    {
        rgbMat = _rgb;
        mnFrameID = _id;
    }
    public Mat rgbMat { get; set; }
    public int mnFrameID { get; set; }
}

class ImageCatchEvent
{
    public static event EventHandler<ImageCatchEventArgs> frameReceived;
    public static void RunEvent(ImageCatchEventArgs e)
    {
        if (frameReceived != null)
        {
            frameReceived(null, e);
        }
    }
}