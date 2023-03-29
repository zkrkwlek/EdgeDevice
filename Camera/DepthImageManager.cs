using System;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DepthImageManager : MonoBehaviour
{
    public DataSender mSender;
    public SystemManager mManager;
    public AROcclusionManager _occlusionManager;
    public Text mText;
    Texture2D _depthTexture;
    short[] _depthArray;

    float InvalidDepthValue = 10000f;
    float MillimeterToMeter = 0.0001f;

    void OnEnable()
    {
        _occlusionManager.frameReceived += OnDepthImageReceived;
    }

    void OnDisable()
    {
        _occlusionManager.frameReceived -= OnDepthImageReceived;
    }
    int frameID = 0;
    int mnSkipFrame;
    void Awake()
    {
        mnSkipFrame = mManager.AppData.numSkipFrames;
    }
    unsafe void OnDepthImageReceived(AROcclusionFrameEventArgs eventArgs)
    {

        if (!_occlusionManager.TryAcquireEnvironmentDepthCpuImage(out XRCpuImage image))
        //if (!_occlusionManager.TryAcquireRawEnvironmentDepthCpuImage(out XRCpuImage image))
            return;
        
        try
        {
            UpdateRawImage(ref _depthTexture, image, TextureFormat.R16);
            var byteBuffer = _depthTexture.GetRawTextureData();
            _depthArray = new short[byteBuffer.Length / 2];
            Buffer.BlockCopy(byteBuffer, 0, _depthArray, 0, byteBuffer.Length);

            //mText.text = "depth = " + byteBuffer.Length + " " + image.width + " " + image.height;

            //스킵마다 압축 후 전송
            if(frameID % mnSkipFrame == 0)
            {
                var timeSpan = DateTime.UtcNow - mManager.StartTime;
                double ts = timeSpan.TotalMilliseconds;
                byte[] bytes = _depthTexture.EncodeToPNG();
                UdpData idata = new UdpData("Depth", mManager.User.UserName, frameID, bytes, ts);
                StartCoroutine(mSender.SendData(idata));
            }
            frameID++;
            //////save data
            //var dirPath = Application.persistentDataPath + "/save";
            //if (!Directory.Exists(dirPath))
            //{
            //    Directory.CreateDirectory(dirPath);
            //}
            //File.WriteAllBytes(dirPath + "/d_" + (i++) + ".png", bytes);

        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }
        
    }

    private void UpdateRawImage(ref Texture2D texture, XRCpuImage cpuImage, UnityEngine.TextureFormat textureFormat)
    {
        if (texture == null || texture.width != cpuImage.width || texture.height != cpuImage.height)
        {
            texture = new Texture2D(cpuImage.width, cpuImage.height, textureFormat, false);
        }

        var conversionParams = new XRCpuImage.ConversionParams(cpuImage, TextureFormat.R16);
        var rawTextureData = texture.GetRawTextureData<byte>();
        cpuImage.Convert(conversionParams, rawTextureData);
        texture.Apply();
    }


    //void updateenvironmentdepthimage()
    //{
    //    _occlusionmanager.rec

    //    if (_occlusionmanager &&
    //          _occlusionmanager.tryacquireenvironmentdepthcpuimage(out xrcpuimage image))
    //    {
    //        using (image)
    //        {
    //            updaterawimage(ref _depthtexture, image, textureformat.r16);
    //            _depthwidth = image.width;
    //            _depthheight = image.height;
    //        }
    //    }
    //    var bytebuffer = _depthtexture.getrawtexturedata();
    //    buffer.blockcopy(bytebuffer, 0, _deptharray, 0, bytebuffer.length);
    //}

    // Obtain the depth value in meters at a normalized screen point.
    //public static float GetDepthFromUV(Vector2 uv, short[] depthArray)
    //{
    //    int depthX = (int)(uv.x * (DepthWidth - 1));
    //    int depthY = (int)(uv.y * (DepthHeight - 1));

    //    return GetDepthFromXY(depthX, depthY, depthArray);
    //}

    ////// Obtain the depth value in meters at the specified x, y location.
    //public static float GetDepthFromXY(int x, int y, short[] depthArray)
    //{
    //    if (!Initialized)
    //    {
    //        return InvalidDepthValue;
    //    }

    //    if (x >= DepthWidth || x < 0 || y >= DepthHeight || y < 0)
    //    {
    //        return InvalidDepthValue;
    //    }

    //    var depthIndex = (y * DepthWidth) + x;
    //    var depthInShort = depthArray[depthIndex];
    //    var depthInMeters = depthInShort * MillimeterToMeter;
    //    return depthInMeters;
    //}
}
