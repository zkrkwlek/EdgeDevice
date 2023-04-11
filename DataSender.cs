using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DataSender : MonoBehaviour
{
    public SystemManager mSystemManager;
    public Experiment mExperiment;
    string strAddr;
    string strUser;

    UnityWebRequest SetRequest(string keyword, byte[] data, int id, double ts)
    {
        string addr2 = mSystemManager.AppData.Address + "/Store?keyword=" + keyword + "&id=" + id + "&ts=" + ts + "&src=" + mSystemManager.User.UserName;
        //string addr2 = strAddr + "/Store?keyword=" + keyword + "&id=" + id + "&ts=" + ts + "&src=" + strUser;
        //if (ts > 0.0)
        //addr2 += "&type2=" + ts;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        if (data.Length > 0)
        {
            UploadHandlerRaw uH = new UploadHandlerRaw(data);
            uH.contentType = "application/json";
            request.uploadHandler = uH;
        }
        request.downloadHandler = new DownloadHandlerBuffer();
        //request.SendWebRequest();
        return request;
    }

    UnityWebRequest SetRequest(string url, byte[] data)
    {
        UnityWebRequest request = new UnityWebRequest(url);
        request.method = "POST";
        if (data.Length > 0)
        {
            UploadHandlerRaw uH = new UploadHandlerRaw(data);
            uH.contentType = "application/json";
            request.uploadHandler = uH;
        }
        request.downloadHandler = new DownloadHandlerBuffer();
        //request.SendWebRequest();
        return request;
    }
    
    public IEnumerator SendData(UdpData data)
    {
        UnityWebRequest req = SetRequest(data.keyword, data.data, data.id, data.ts);
        
        yield return req.SendWebRequest();
        //if (req.result == UnityWebRequest.Result.Success)
        //{
        //}
        //yield return null;
    }
    public IEnumerator SendData(string url,UdpData data)
    {
        var t1 = DateTime.Now;
        UnityWebRequest req = SetRequest(url, data.data);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
        {
            var t2 = DateTime.Now;
            TimeSpan time2 = t2 - t1;
            mExperiment.Calculate((float)time2.TotalMilliseconds);
        }
    }
    
    bool WantsToQuit()
    {
        //while (mnSendProcessing > 0)
        //    continue;
        return true;
    }

    void Awake()
    {
        strAddr = mSystemManager.AppData.Address;
        strAddr = mSystemManager.User.UserName;
    }

    // Start is called before the first frame update
    void Start()
    {
        enabled = false;
        Application.wantsToQuit += WantsToQuit;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
