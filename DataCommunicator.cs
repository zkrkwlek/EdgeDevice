using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DataCommunicator : MonoBehaviour
{

    public Text mText;
    //public ScaleAdjuster mScaleAdjuster;
    public PlaneManager mPlaneManager;
    public ContentProcessor mContentManager;
    public SystemManager mSystemManager;
    public ObjectDetection mObjectDetection;
    public Tracker mTracker;
    public UVRSpatialTest mSpatialTest;

    ExperimentParam mExParam;
    EvaluationParam mEvalParam;
    TrackerParam mTrackParam;
    public ParameterManager mParamManager;
    public EvaluationManager mEvalManager;

    public IEnumerator SendData(UdpData data)
    {
        if(mEvalParam.bNetworkTraffic && !mExParam.bEdgeBase && data.keyword == "Image")
        {
            string res = "image," + data.id + "," + mTrackParam.nJpegQuality + "," + mTrackParam.nSkipFrames + "," + data.data.Length;
            mEvalManager.writer_network_traffic.WriteLine(res);
        }
        UnityWebRequest req = SetRequest(data.keyword, data.data, data.id, data.ts);
        yield return req.SendWebRequest();
        //if (req.result == UnityWebRequest.Result.Success)
        //{
        //}
        //yield return null;
    }

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

    UnityWebRequest GetRequest(string keyword, int id)
    {
        string addr2 = mSystemManager.AppData.Address + "/Load?keyword=" + keyword + "&id=" + id + "&src=" + mSystemManager.User.UserName;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        //request.SendWebRequest();
        return request;
    }
    UnityWebRequest GetRequest(string keyword, int id, string src)
    {
        string addr2 = mSystemManager.AppData.Address + "/Load?keyword=" + keyword + "&id=" + id + "&src=" + src;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        //request.SendWebRequest();
        return request;
    }

    void Awake()
    {
        mTrackParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
        mEvalParam = (EvaluationParam)mParamManager.DictionaryParam["Evaluation"];
        mExParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
    }
    void OnEnable()
    {
        UdpAsyncHandler.Instance.UdpDataReceived += Process;
    }
    void OnDisable()
    {
        UdpAsyncHandler.Instance.UdpDataReceived -= Process;
    }


        // Start is called before the first frame update
    void Start()
    {
        
    }
    void Process(object sender, UdpEventArgs e) {
        try {
            int size = e.bdata.Length;
            string msg = System.Text.Encoding.Default.GetString(e.bdata);
            UdpData data = JsonUtility.FromJson<UdpData>(msg);
            data.receivedTime = DateTime.Now;
            StartCoroutine(MessageParsing(data));
        }
        catch(Exception ex)
        {
            mText.text = ex.ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    HashSet<int> cids = new HashSet<int>();
    const int nMapPointInfo = 36;
    const int nSizeServerMP = nMapPointInfo + 32; // 3d x y z + desc // info + desc
    IEnumerator MessageParsing(UdpData data) {

        if (data.keyword == "ReferenceFrame")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                ////
                if (!mSystemManager.mbInit) {
                    var timeSpan = DateTime.Now - mSystemManager.TestTime;
                    double ts = timeSpan.TotalMilliseconds;
                    mSystemManager.writer.WriteLine(ts);
                    mSystemManager.mbInit = true;
                }

                float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
                int Nmp = (int)fdata[0];

                if (mEvalParam.bNetworkTraffic && !mExParam.bEdgeBase)
                {
                    string res = "keyframe,"+data.id + "," + mTrackParam.nJpegQuality + "," + mTrackParam.nSkipFrames + "," + req1.downloadHandler.data.Length;
                    mEvalManager.writer_network_traffic.WriteLine(res);
                }

                try
                {
                    GCHandle handle = GCHandle.Alloc(fdata, GCHandleType.Pinned);
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    //int a = CreateReferenceFrame(data.id, ptr);
                    mTracker.CreateKeyFrame(data.id,ptr);
                    handle.Free();
                    //mText.text = "Queue Test = " + a;
                }
                catch (Exception e)
                {
                    mText.text = "create reference err = " + e.ToString();
                }

                PointCloudReceivedEvent.RunEvent(new PointCloudReceivedEventArgs(data.id, Nmp, fdata));
                
                //Vector3 a = new Vector4(fdata[1], fdata[4], fdata[7],0f);
                //Vector3 b = new Vector4(fdata[2], fdata[5], fdata[8], 0f);
                //Vector3 c = new Vector4(fdata[3], fdata[6], fdata[9], 0f);
                //Vector3 d = new Vector4(fdata[10], fdata[11], fdata[12], 1f);
                //Matrix4x4 P = new Matrix4x4(a,b,c,d);

                ///////
                //Matrix4x4 P = new Matrix4x4();
                //int idx = 1;
                //P.m00 = fdata[idx++];
                //P.m01 = fdata[idx++];
                //P.m02 = fdata[idx++];
                //P.m10 = fdata[idx++];
                //P.m11 = fdata[idx++];
                //P.m12 = fdata[idx++];
                //P.m20 = fdata[idx++];
                //P.m21 = fdata[idx++];
                //P.m22 = fdata[idx++];
                //P.m03 = fdata[idx++];
                //P.m13 = fdata[idx++];
                //P.m23 = fdata[idx++];
                //P.m33 = 1f;
                //var Pinv = P.inverse;

                //Vector4 t = Pinv.GetColumn(3);
                //UVR.transform.position = new Vector3(t.x, t.y, t.z);
                //UVR.transform.rotation = Pinv.rotation;
                ///////

                //스파셜 컨시스텐시 이벤트를 생성.

                //mText.text = Pinv.ToString() + UVR.transform.localToWorldMatrix.ToString();
                //mText.text = P.ToString()+Pinv.ToString() + UVR.transform.position.ToString();

                // mText.text = UVR.transform.position.ToString();


                //mText.text = P.ToString();

                //StatusTxt.text = data.keyword + "=" + fdata[0] + ", " + req1.downloadHandler.data.Length;
                //mScaleAdjuster.SetServerPose(data.id, ref fdata);
                //mScaleAdjuster.CalculateScale(data.id);
            }
        }
        if(data.keyword == "UpdatedLocalMap")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                if (mEvalParam.bNetworkTraffic && mExParam.bEdgeBase)
                {
                    string res = "localmap," + data.id + ",-1,-1," + req1.downloadHandler.data.Length;
                    mEvalManager.writer_network_traffic.WriteLine(res);
                }

                //float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                //Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
                int n = req1.downloadHandler.data.Length / nSizeServerMP;
                try
                {
                    GCHandle handle = GCHandle.Alloc(req1.downloadHandler.data, GCHandleType.Pinned);
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    mTracker.UpdateData(data.id, n, ptr);
                    //UpdateLocalMap(data.id, n, ptr);
                    handle.Free();
                }
                catch (Exception e)
                {
                    mText.text = "Update Local Map Error = " + e.ToString();
                }

            }
        }
        if(data.keyword == "ObjectDetection")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] temp = new float[4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, temp, 0, 4);
                mObjectDetection.Receive(temp);
            }
        }
        if (data.keyword == "ShareSemanticInfo")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] temp = new float[4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, temp, 0, 4);
                int N = (int)temp[0];
                float[] fdata = new float[2*N];
                int idx = fdata.Length * 4+4;
                Buffer.BlockCopy(req1.downloadHandler.data, 4, fdata, 0, fdata.Length*4);

                int Nimg = req1.downloadHandler.data.Length - idx;
                byte[] bdata = new byte[Nimg];
                Buffer.BlockCopy(req1.downloadHandler.data, idx, bdata, 0,Nimg);

                DataTransferEvent.RunEvent(new DataTransferEventArgs(data.id, N, ref bdata, ref fdata));
                //포인트 개수 읽고
                //포인트
                //이미지 디코딩
            }
        }
        if (data.keyword == "ArUcoMarkerDetection")
        {
            //이 시점에서의 자세를 찾아야 함.
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] temp = new float[15];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, temp,0, temp.Length * 4);
                MarkerReceiveEvent.RunEvent(new MarkerReceiveEventArgs(data.id, temp));
                //포인트 개수 읽고
                //포인트
                //이미지 디코딩
            }
        }
        if (data.keyword == "ArUcoMarkerDetection2")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] temp = new float[16];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, temp, 0, temp.Length * 4);
                MarkerReceiveEvent2.RunEvent(new MarkerReceiveEventArgs(data.id, temp));
                //포인트 개수 읽고
                //포인트
                //이미지 디코딩
            }
        }
        if (data.keyword == "PlaneLine") //나중에 키워드 변경
        {

            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
                mPlaneManager.UpdateLocalPlane(data.id, fdata);

                //game object를 생성한 후 시간 지나면 삭제하는 것을 목표로

                //int N = 12;
                //float[] fdata = new float[N * 4];
                //Buffer.BlockCopy(req1.downloadHandler.data, 4, fdata, 0, N * 4);
                //StatusTxt.text = data.keyword + "=" + fdata[0] + ", " + req1.downloadHandler.data.Length;
                //mScaleAdjuster.SetServerPose(data.id, ref fdata);
                //mScaleAdjuster.CalculateScale(data.id);
            }
        }
        if (data.keyword == "LocalContent") //나중에 키워드 변경
        {

            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();   
            
            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
                mContentManager.UpdateVirtualFrame(data.id,fdata);
            }
        }
        if (data.keyword == "VO.MARKER.CREATED") //나중에 키워드 변경
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();

            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
                mSpatialTest.AttachObject(data.id, fdata);
            }
        }
        if (data.keyword == "MarkerRegistrations") //나중에 키워드 변경
        {

            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();

            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
                //int N = (int)fdata[0];
                //int idx = 1;
                //for (int j = 0; j < N; j++)
                //{
                //    int id = (int)fdata[idx];
                //    float a = fdata[idx + 1];
                //    float b = fdata[idx + 2];
                //    float x = fdata[idx + 3];
                //    float y = fdata[idx + 4];
                //    float z = fdata[idx + 5];
                //    float ex = fdata[idx + 6];
                //    float ey = fdata[idx + 7];
                //    float ez = fdata[idx + 8];

                //    idx += 9;
                //    mContentManager.Process(id, x, y, z);
                //}

            }
        }
        
        if(data.keyword == "GetCloudAnchor")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();

            if (req1.result == UnityWebRequest.Result.Success)
            {
                string str = Encoding.Default.GetString(req1.downloadHandler.data);
                AnchorListenEvent.RunEvent(new AnchorListenArgs(data.id, str));
            }
        }
        if (data.keyword == "VO.MOVE") //나중에 키워드 변경
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            mContentManager.Move(data.id);
            yield return null;
        }
        
        if (data.keyword == "dr") //나중에 키워드 변경
        {
            DateTime t1 = DateTime.Now;
            DirectLatencyEvent.RunEvent(new LatencyEventArgs(data.id, t1));
            yield return null;
        }
        if (data.keyword == "ir") //나중에 키워드 변경
        {
            DateTime t1 = DateTime.Now;
            IndirectLatencyEvent.RunEvent(new LatencyEventArgs(data.id, t1));
            yield return null;
        }
        







        //if (data.keyword == "ReferenceFrame")
        //{

        //    UnityWebRequest req1;
        //    req1 = GetRequest(data.keyword, data.id);
        //    DateTime t1 = DateTime.Now;
        //    yield return req1.SendWebRequest();
        //    //1~12까지가 포즈 정보임.
        //    if (req1.result == UnityWebRequest.Result.Success)
        //    {
        //        int N = 12;
        //        float[] fdata = new float[N * 4];
        //        Buffer.BlockCopy(req1.downloadHandler.data, 4, fdata, 0, N * 4);
        //        //StatusTxt.text = data.keyword + "=" + fdata[0] + ", " + req1.downloadHandler.data.Length;
        //        mScaleAdjuster.SetServerPose(data.id, ref fdata);
        //        mScaleAdjuster.CalculateScale(data.id);
        //    }
        //}
        //else if (data.keyword == "LocalContent")
        //{
        //    UnityWebRequest req1;
        //    req1 = GetRequest(data.keyword, data.id);
        //    DateTime t1 = DateTime.Now;
        //    yield return req1.SendWebRequest();
        //    //1~12까지가 포즈 정보임.
        //    if (req1.result == UnityWebRequest.Result.Success)
        //    {
        //        try {
        //            float[] fdata = new float[req1.downloadHandler.data.Length / 4];
        //            Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
        //            if(mScaleAdjuster.mbScaleAdjustment)
        //                mContentManager.UpdateContents(ref fdata, mScaleAdjuster.Tlg);
        //        }
        //        catch(Exception e)
        //        {
        //            StatusTxt.text = e.ToString();
        //        }
        //    }
        //}




        ////try
        ////{
        //if (data.keyword == "LocalContent")
        //{
        //    UnityWebRequest req1;
        //    req1 = GetRequest(data.keyword, data.id);
        //    DateTime t1 = DateTime.Now;
        //    yield return req1.SendWebRequest();


        //    if (req1.result == UnityWebRequest.Result.Success)
        //    {
        //        float[] fdata = new float[req1.downloadHandler.data.Length / 4];
        //        Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);

        //        ////가상 객체 등록
        //        //int N = (int)fdata[0];
        //        //int idx = 1;
        //        //for(int i = 0; i < N; i++)
        //        //{
        //        //    int id = (int)fdata[i*4+1];
        //        //    if (!cids.Contains(id))
        //        //    {
        //        //        cids.Add(id);
        //        //        Vector3 pos = new Vector3(fdata[i*4+2], -fdata[i * 4 + 3], fdata[i * 4 + 4]);
        //        //        Vector3 rot = new Vector3(0.0f, 0.0f, 0.0f);
        //        //        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //        //        go.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        //        //        float fAngle = rot.magnitude * Mathf.Rad2Deg;
        //        //        Quaternion q = Quaternion.AngleAxis(fAngle, rot.normalized);
        //        //        go.transform.SetPositionAndRotation(pos, q);
        //        //    }
        //        //}
        //        ////가상 객체 등록

        //        //int idx = 1 + (N - 1) * 4;
        //        //int cid = (int)fdata[idx++];
        //        //StatusTxt.text = "\t\t\t\t\t" + cid + " " + N+", "+idx+" "+fdata.Length;
        //        //Vector3 pos = new Vector3(fdata[idx++], fdata[idx++], fdata[idx++]);
        //        ////Vector3 rot = new Vector3(rdata[nIDX++], rdata[nIDX++], rdata[nIDX++]);
        //        //Vector3 rot = new Vector3(0.0f, 0.0f, 0.0f);
        //        //string contentname = "CO" + cid;
        //        //var co = GameObject.Find(contentname);
        //        //if (co)
        //        //{
        //        //    StatusTxt.text = "\t\t\t\t already Create " + contentname + co.transform.position.x + " " + co.transform.position.y + " " + co.transform.position.z + "::" + pos.x + " " + pos.y + " " + pos.z;
        //        //}
        //        //else
        //        //{
        //        //    //StatusTxt.text = "\t\t\t\t Create " + contentname + "::" + pos.x + " " + pos.y + " " + pos.z;
        //        //    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //        //    go.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        //        //    float fAngle = rot.magnitude * Mathf.Rad2Deg;
        //        //    Quaternion q = Quaternion.AngleAxis(fAngle, rot.normalized);
        //        //    go.transform.SetPositionAndRotation(pos, q);
        //        //    go.name = data.type2 + (int)data.ts;
        //        //    //StatusTxt.text = "\t\t\t Create asdfasdf aaaaaaa " + pos.ToString() + Camera.main.transform.position.ToString()+" "+fdata[0];
        //        //}

        //        //시각화

        //        //AddContentInfo(data.id, fdata[0], fdata[1], fdata[2]);


        //        //if (data.type2 == SystemManager.Instance.User.UserName)
        //        //{
        //        //    ////update 시간
        //        //    int id = (int)data.ts;
        //        //    //StatusTxt.text = "\t\t\t\t\t asdfasdfasdfasdfasdfasdfasdf = " +id;
        //        //    UdpData data2 = DataQueue.Instance.Get("ContentGeneration" + id);
        //        //    TimeSpan time2 = data.receivedTime - data2.sendedTime;
        //        //    SystemManager.Instance.Experiments["Content"].Update("latency", (float)time2.Milliseconds);
        //        //}
        //    }


        //}
        //else {
        //    //if (data.keyword == "ReferenceFrame")
        //    //{
        //    //    ////update 시간
        //    //    UdpData data2 = DataQueue.Instance.Get("Image" + data.id);
        //    //    TimeSpan time2 = data.receivedTime - data2.sendedTime;
        //    //    SystemManager.Instance.Experiments["ReferenceFrame"].Update("latency", (float)time2.Milliseconds);
        //    //    //StatusTxt.text = "\t\t\t\t\t Reference = " + time2.TotalMilliseconds;
        //    //    ////update 시간

        //    //}
        //    //else if (data.keyword == "VO.Registration")
        //    //{
        //    //    //수행
        //    //}
        //    //else if (data.keyword == "ObjectDetection")
        //    //{
        //    //    UdpData data2 = DataQueue.Instance.Get("Image" + data.id);
        //    //    TimeSpan time2 = data.receivedTime - data2.sendedTime;
        //    //    SystemManager.Instance.Experiments["ObjectDetection"].Update("latency", (float)time2.Milliseconds);
        //    //    //SystemManager.Instance.Experiments["ObjectDetection"].Update("traffic", n1);
        //    //    //SystemManager.Instance.Experiments["ObjectDetection"].Update("download", (float)Dtimespan.Milliseconds);

        //    //}
        //    //else if (data.keyword == "Segmentation")
        //    //{
        //    //    UdpData data2 = DataQueue.Instance.Get("Image" + data.id);
        //    //    TimeSpan time2 = data.receivedTime - data2.sendedTime;
        //    //    SystemManager.Instance.Experiments["Segmentation"].Update("latency", (float)time2.Milliseconds);
        //    //}
        //}

        yield break;
    }

}