using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LatencyEventArgs : EventArgs
{
    public LatencyEventArgs(int _id, DateTime _rtime)
    {
        id = _id;
        rtime = _rtime;
    }
    public int id { get; set; }
    public DateTime rtime { get; set; }
}

class DirectLatencyEvent
{
    public static event EventHandler<LatencyEventArgs> latencyEvent;
    public static void RunEvent(LatencyEventArgs e)
    {
        if (latencyEvent != null)
        {
            latencyEvent(null, e);
        }
    }
}
class IndirectLatencyEvent
{
    public static event EventHandler<LatencyEventArgs> latencyEvent;
    public static void RunEvent(LatencyEventArgs e)
    {
        if (latencyEvent != null)
        {
            latencyEvent(null, e);
        }
    }
}


public class TemporalConsistency : MonoBehaviour
{
    StreamWriter writer_direct;
    StreamWriter writer_indirect;

    public SystemManager mSystemManager;
    public DataSender mSender;

    public Text mText;

    bool WantsToQuit()
    {
        writer_direct.Close();
        writer_indirect.Close();
        return true;
    }
    // Start is called before the first frame update
    void Awake()
    {
        mDictTouch = new Dictionary<int, DateTime>();
        var dirPath = Application.persistentDataPath + "/data";
        var filePath = dirPath + "/direct_latency.csv";
        writer_direct = new StreamWriter(filePath, true);
        var filePath2 = dirPath + "/indirect_latency.csv";
        writer_indirect = new StreamWriter(filePath2, true);
        Application.wantsToQuit += WantsToQuit;
    }

    void OnEnable()
    {
        DirectLatencyEvent.latencyEvent += OnDirectEvent;
        IndirectLatencyEvent.latencyEvent += OnInDirectEvent;
    }
    void OnDisable()
    {
        DirectLatencyEvent.latencyEvent -= OnDirectEvent;
        IndirectLatencyEvent.latencyEvent -= OnInDirectEvent;
    }

    void OnDirectEvent(object sender, LatencyEventArgs args)
    {
        try
        {
            int id = args.id;
            var timeSpan = args.rtime - mDictTouch[id];
            double ts = timeSpan.TotalMilliseconds;
            writer_direct.WriteLine(ts);
        }
        catch (Exception e)
        {
            mText.text = "direct err = " + e.ToString();
        }
    }
    void OnInDirectEvent(object sender, LatencyEventArgs args)
    {
        try
        {
            int id = args.id;
            var timeSpan = args.rtime - mDictTouch[id];
            double ts = timeSpan.TotalMilliseconds;
            writer_indirect.WriteLine(ts);
        }
        catch (Exception e)
        {
            mText.text = "indirect err = " + e.ToString();
        }
    }

    Dictionary<int, DateTime> mDictTouch;
    int touchID = 0;
    // Update is called once per frame
    void Update()
    {

        try
        {
            bool bTouch = false;
            Vector2 touchPos = Vector2.zero;
            if (Input.touchCount > 0) {
                //데이터 전송
                float[] fdata = new float[1000];
                byte[] bdata = new byte[fdata.Length * 4];
                Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                UdpData ddata = new UdpData("ds", mSystemManager.User.UserName, touchID, bdata, 1.0);
                StartCoroutine(mSender.SendData(ddata));
                mDictTouch.Add(touchID, DateTime.Now);
                touchID++;
            }

        }
        catch (Exception ex)
        {
            ex.ToString();
        }
    }

}
