using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class TimeServer : MonoBehaviour
{
    public ParameterManager mParamManager;
    Param param;
    public Text mText;
    private DateTime mLocalStartTime;
    private DateTime mServerStartTime { get; set; } = DateTime.MinValue;
    private DateTime mCurrentTime;
    public DateTime ServerTime => mCurrentTime;
    public Action<DateTime> OnTimeUpdated { get; set; }

    void OnEnable()
    {
        
    }
    void OnDisable()
    {
        
    }

    public void GetNetworkTime()
    {
        try {
            //default Windows time server
            const string ntpServer = "time.windows.com";

            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;

            //The UDP port number assigned to NTP is 123
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            //NTP uses UDP

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);

                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                mLocalStartTime = DateTime.UtcNow;//new DateTime(1900, 1, 1, 0, 0, 0);
                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

            //Offset to get to the "Transmit Timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format."
            const byte serverReplyTime = 40;

            //Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //Convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            //**UTC** time
            
            mServerStartTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);
            //Debug.Log(networkDateTime.ToLocalTime()+"::"+networkDateTime.ToLocalTime().Millisecond.ToString());
            Debug.Log(mLocalStartTime.ToLocalTime().ToString()+":"+mLocalStartTime.Millisecond);
            Debug.Log(mServerStartTime.ToLocalTime().ToString() + ":" + mServerStartTime.Millisecond);
            //return mServerStartTime.ToLocalTime();
        }
        catch(Exception e)
        {
            //Debug.Log("ntp=" + e.ToString());
            mText.text = "TimeServer = " + e.ToString();
        }
        //return DateTime.Now;
    }

    uint SwapEndianness(ulong x)
    {
        return (uint)(((x & 0x000000ff) << 24) +
                       ((x & 0x0000ff00) << 8) +
                       ((x & 0x00ff0000) >> 8) +
                       ((x & 0xff000000) >> 24));
    }

    // Start is called before the first frame update
    void Awake()
    {
        param = mParamManager.DictionaryParam["TimeServer"];
        if(!param.bEnable)
        {
            enabled = false;
            return;
        }
        GetNetworkTime();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var timeSpan = DateTime.UtcNow - mLocalStartTime;
        mCurrentTime = mServerStartTime.AddMilliseconds(timeSpan.TotalMilliseconds);
        if (param.bShowLog)
            mText.text = "Time = " + mCurrentTime.ToLocalTime() +"="+ mCurrentTime.Millisecond;

    }
}
