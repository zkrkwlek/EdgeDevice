//using ARFoundationWithOpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;


public class CameraManager : MonoBehaviour
{
    public Text mText;
    public SystemManager mManager;
    //public ARFoundationCameraToMatHelper webCamTextureToMatHelper;
    [HideInInspector]
    bool mbInitialized;
    [HideInInspector]
    public Mat rgbMat;
    [HideInInspector]
    public Mat camMatrix;
    [HideInInspector]
    public Mat invCamMatrix;
    [HideInInspector]
    public MatOfDouble distCoeffs;
    [HideInInspector]
    public Matrix4x4 fitARFoundationBackgroundMatrix;
    [HideInInspector]
    public Matrix4x4 fitHelpersFlipMatrix;

    [HideInInspector]
    public float height;
    [HideInInspector]
    public float width;
    [HideInInspector]
    public float widthScale;
    [HideInInspector]
    public float heightScale;
    [HideInInspector]
    public int mnFrame;

    public RawImage rawImage;

    public void OnWebCamTextureToMatHelperInitialized()
    {
//        // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection of AR markers.
//        if (webCamTextureToMatHelper.IsFrontFacing() && !webCamTextureToMatHelper.flipHorizontal)
//        {
//            webCamTextureToMatHelper.flipHorizontal = true;
//        }
//        else if (!webCamTextureToMatHelper.IsFrontFacing() && webCamTextureToMatHelper.flipHorizontal)
//        {
//            webCamTextureToMatHelper.flipHorizontal = false;
//        }
//        Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

//        // set camera parameters.
//        double fx;
//        double fy;
//        double cx;
//        double cy;

//#if (UNITY_IOS || UNITY_ANDROID)// && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API

//        UnityEngine.XR.ARSubsystems.XRCameraIntrinsics cameraIntrinsics = webCamTextureToMatHelper.GetCameraIntrinsics();

//        // Apply the rotate and flip properties of camera helper to the camera intrinsics.
//        Vector2 fl = cameraIntrinsics.focalLength;
//        Vector2 pp = cameraIntrinsics.principalPoint;
//        Vector2Int r = cameraIntrinsics.resolution;

//        Matrix4x4 tM = Matrix4x4.Translate(new Vector3(-r.x / 2, -r.y / 2, 0));
//        pp = tM.MultiplyPoint3x4(pp);

//        Matrix4x4 rotationAndFlipM = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, webCamTextureToMatHelper.rotate90Degree ? 90 : 0),
//            new Vector3(webCamTextureToMatHelper.flipHorizontal ? -1 : 1, webCamTextureToMatHelper.flipVertical ? -1 : 1, 1));
//        pp = rotationAndFlipM.MultiplyPoint3x4(pp);

//        if (webCamTextureToMatHelper.rotate90Degree)
//        {
//            fl = new Vector2(fl.y, fl.x);
//            r = new Vector2Int(r.y, r.x);
//        }

//        Matrix4x4 _tM = Matrix4x4.Translate(new Vector3(r.x / 2, r.y / 2, 0));
//        pp = _tM.MultiplyPoint3x4(pp);

//        cameraIntrinsics = new UnityEngine.XR.ARSubsystems.XRCameraIntrinsics(fl, pp, r);

//        fx = cameraIntrinsics.focalLength.x;
//        fy = cameraIntrinsics.focalLength.y;
//        cx = cameraIntrinsics.principalPoint.x;
//        cy = cameraIntrinsics.principalPoint.y;

//        camMatrix = new Mat(3, 3, CvType.CV_64FC1);
//        camMatrix.put(0, 0, fx);
//        camMatrix.put(0, 1, 0);
//        camMatrix.put(0, 2, cx);
//        camMatrix.put(1, 0, 0);
//        camMatrix.put(1, 1, fy);
//        camMatrix.put(1, 2, cy);
//        camMatrix.put(2, 0, 0);
//        camMatrix.put(2, 1, 0);
//        camMatrix.put(2, 2, 1.0f);

//        double invfx = 1 / fx;
//        double invfy = 1 / fy;
//        double invcx = -cx / fx;
//        double invcy = -cy / fy;
//        invCamMatrix = new Mat(3, 3, CvType.CV_64FC1);
//        invCamMatrix.put(0, 0, invfx);
//        invCamMatrix.put(0, 1, 0);
//        invCamMatrix.put(0, 2, invcx);
//        invCamMatrix.put(1, 0, 0);
//        invCamMatrix.put(1, 1, invfy);
//        invCamMatrix.put(1, 2, invcy);
//        invCamMatrix.put(2, 0, 0);
//        invCamMatrix.put(2, 1, 0);
//        invCamMatrix.put(2, 2, 1.0f);
//        //invCamMatrix = camMatrix.inv();

//        distCoeffs = new MatOfDouble(0, 0, 0, 0);

//        Debug.Log("Created CameraParameters from the camera intrinsics to be populated if the camera supports intrinsics.");

//        var focalLength = cameraIntrinsics.focalLength;
//        var principalPoint = cameraIntrinsics.principalPoint;
//        var resolution = cameraIntrinsics.resolution;

//#else // (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API

//        float width = webCamTextureMat.width();
//        float height = webCamTextureMat.height();

//        int max_d = (int)Mathf.Max(width, height);
//        fx = max_d;
//        fy = max_d;
//        cx = width / 2.0f;
//        cy = height / 2.0f;

//        camMatrix = new Mat(3, 3, CvType.CV_64FC1);
//        camMatrix.put(0, 0, fx);
//        camMatrix.put(0, 1, 0);
//        camMatrix.put(0, 2, cx);
//        camMatrix.put(1, 0, 0);
//        camMatrix.put(1, 1, fy);
//        camMatrix.put(1, 2, cy);
//        camMatrix.put(2, 0, 0);
//        camMatrix.put(2, 1, 0);
//        camMatrix.put(2, 2, 1.0f);

//        distCoeffs = new MatOfDouble(0, 0, 0, 0);

//        Debug.Log("Created a dummy CameraParameters.");

//#endif // (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API

//        height = webCamTextureMat.height();//webCamTextureToMatHelper.requestedHeight;
//        width = webCamTextureMat.width();//webCamTextureToMatHelper.requestedWidth;

//        widthScale = (float)Screen.width / width;
//        heightScale = (float)Screen.height / height;

//        Application.wantsToQuit += WantsToQuit;
//        rgbMat = new Mat((int)height, (int)width, CvType.CV_8UC3);
//        mbInitialized = true;
//        mManager.TestTime = DateTime.Now;
        
//        // Create the transform matrix to fit the ARM to the background display by ARFoundationBackground component.
//        fitARFoundationBackgroundMatrix = Matrix4x4.Scale(new Vector3(webCamTextureToMatHelper.GetDisplayFlipHorizontal() ? -1 : 1, webCamTextureToMatHelper.GetDisplayFlipVertical() ? -1 : 1, 1)) * Matrix4x4.identity;
//        // Create the transform matrix to fit the ARM to the flip properties of the camera helper.
//        fitHelpersFlipMatrix = Matrix4x4.Scale(new Vector3(webCamTextureToMatHelper.flipHorizontal ? -1 : 1, webCamTextureToMatHelper.flipVertical ? -1 : 1, 1)) * Matrix4x4.identity;

//        rawImage.uvRect = new UnityEngine.Rect(0, 0, Screen.width, Screen.height);
//        rawImage.color = new Color(1f, 1f, 1f, 0f);
//        CameraInitEvent.RunEvent(new CameraInitEventArgs(camMatrix, invCamMatrix, distCoeffs, fitARFoundationBackgroundMatrix, fitHelpersFlipMatrix, (int)width, (int)height, widthScale, heightScale));
    }

    /// <summary>
    /// Raises the webcam texture to mat helper disposed event.
    /// </summary>
    public void OnWebCamTextureToMatHelperDisposed()
    {
    }

    /// <summary>
    /// Raises the webcam texture to mat helper error occurred event.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
    {
    }


    bool WantsToQuit()
    {
        if(rgbMat != null)
            rgbMat.Dispose();
        if (camMatrix != null)
            camMatrix.Dispose();
        if (invCamMatrix != null)
            invCamMatrix.Dispose();
        return true;
    }

    void OnEnable()
    {
//        //ImageCatchEvent.frameReceived += OnCameraFrameReceived;
//#if (UNITY_IOS || UNITY_ANDROID) //&& !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API
//        webCamTextureToMatHelper.frameMatAcquired += OnFrameMatAcquired;
//#endif // (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API
//    }

//    void OnDisable()
//    {
//        //ImageCatchEvent.frameReceived -= OnCameraFrameReceived;
//#if (UNITY_IOS || UNITY_ANDROID)// && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API
//        webCamTextureToMatHelper.frameMatAcquired -= OnFrameMatAcquired;
//#endif // (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API
//        webCamTextureToMatHelper.Dispose();
        
    }

    void OnFrameMatAcquired(Mat mat, Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix, XRCameraIntrinsics cameraIntrinsics, long timestamp)
    {
        try
        {
            //mText.text = "frame received = " + cameraIntrinsics.ToString();
            rgbMat = mat;
            mnFrame++;
            ImageCatchEvent.RunEvent(new ImageCatchEventArgs(ref rgbMat, mnFrame));
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
         
    }
    void Awake()
    {
        try
        {
            mbInitialized = false;
            mnFrame = 0;
            //webCamTextureToMatHelper.Initialize();
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
