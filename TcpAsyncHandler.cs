using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using UnityEngine;

public struct TcpState
{
    public TcpClient tcp;
    public NetworkStream stream;
    public IPEndPoint lep, rep;
    public byte[] buffer;
}

// UdpEventArgs와 UdpData는 그대로 재사용 가능

public class TcpAsyncHandler
{
    static private TcpAsyncHandler m_pInstance = null;
    static public TcpAsyncHandler Instance
    {
        get
        {
            if (m_pInstance == null)
            {
                m_pInstance = new TcpAsyncHandler();
            }
            return m_pInstance;
        }
    }

    static private TcpState stat;
    private const int BUFFER_SIZE = 8192;

    public TcpState Status
    {
        get { return stat; }
    }

    public bool IsConnected
    {
        get { return stat.tcp != null && stat.tcp.Connected; }
    }

    /// <summary>
    /// TCP 서버로 연결
    /// </summary>
    public TcpState TcpSocketConnect(string remoteip, int remoteport)
    {
        stat = new TcpState();
        try
        {
            TcpClient tcp = new TcpClient();
            stat.tcp = tcp;
            stat.buffer = new byte[BUFFER_SIZE];

            // 비동기 연결 시작
            tcp.BeginConnect(remoteip, remoteport, new AsyncCallback(ConnectCallback), stat);

            //Debug.Log($"Connecting to {remoteip}:{remoteport}...");
        }
        catch (Exception e)
        {
            //Debug.Log($"TCP Connect failed: {e.Message}");
        }

        return stat;
    }

    /// <summary>
    /// 연결 완료 콜백
    /// </summary>
    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            TcpState state = (TcpState)ar.AsyncState;
            TcpClient tcp = state.tcp;

            tcp.EndConnect(ar);

            if (tcp.Connected)
            {
                // NetworkStream 얻기
                stat.stream = tcp.GetStream();
                stat.rep = (IPEndPoint)tcp.Client.RemoteEndPoint;
                stat.lep = (IPEndPoint)tcp.Client.LocalEndPoint;

                OnTcpConnected(EventArgs.Empty);

                // 데이터 수신 시작
                stat.stream.BeginRead(stat.buffer, 0, BUFFER_SIZE,
                    new AsyncCallback(ReceiveCallback), stat);
            }
        }
        catch (Exception e)
        {
            //Debug.LogError($"Connect callback error: {e.Message}");
        }
    }

    //연결 완료
    public event EventHandler TcpConnected;
    protected virtual void OnTcpConnected(EventArgs e)
    {
        EventHandler handler = TcpConnected;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    /// <summary>
    /// 데이터 수신 콜백
    /// </summary>
    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            TcpState state = (TcpState)ar.AsyncState;
            NetworkStream stream = state.stream;

            if (stream == null || !stream.CanRead)
                return;

            int bytesRead = stream.EndRead(ar);

            if (bytesRead > 0)
            {
                // 받은 데이터 처리
                byte[] receivedData = new byte[bytesRead];
                Array.Copy(state.buffer, 0, receivedData, 0, bytesRead);

                UdpEventArgs args = new UdpEventArgs();
                args.bdata = receivedData;
                OnTcpDataReceived(args);

                // 다시 수신 대기
                stream.BeginRead(state.buffer, 0, BUFFER_SIZE,
                    new AsyncCallback(ReceiveCallback), state);
            }
            else
            {
                TcpSocketClose();
            }
        }
        catch (Exception e)
        {
            TcpSocketClose();
        }
    }

    /// <summary>
    /// 데이터 전송
    /// </summary>
    public int Send(string src, string keyword, string method, string type)
    {
        if (!IsConnected)
        {
            return -1;
        }

        try
        {
            UdpData data = new UdpData(keyword, method, src);
            data.type2 = type;
            string msg = JsonUtility.ToJson(data);
            byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

            // TCP는 스트림 기반이므로 길이 정보 먼저 전송 (옵션)
            // 4바이트 길이 헤더 + 실제 데이터
            //byte[] lengthPrefix = BitConverter.GetBytes(bdata.Length);
            //byte[] fullData = new byte[4 + bdata.Length];
            //Array.Copy(lengthPrefix, 0, fullData, 0, 4);
            //Array.Copy(bdata, 0, fullData, 4, bdata.Length);

            //stat.stream.Write(fullData, 0, fullData.Length);
            stat.stream.Write(bdata, 0, bdata.Length);
            stat.stream.Flush();

            return bdata.Length;
        }
        catch (Exception e)
        {
            return -1;
        }
    }

    /// <summary>
    /// 바이트 데이터 직접 전송
    /// </summary>
    public void SendData(byte[] data)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            // 길이 헤더 + 데이터
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
            byte[] fullData = new byte[4 + data.Length];
            Array.Copy(lengthPrefix, 0, fullData, 0, 4);
            Array.Copy(data, 0, fullData, 4, data.Length);

            stat.stream.Write(fullData, 0, fullData.Length);
            stat.stream.Flush();
        }
        catch (Exception e)
        {
            //Debug.LogError($"SendData error: {e.Message}");
        }
    }

    /// <summary>
    /// 연결 종료
    /// </summary>
    public void TcpSocketClose()
    {
        try
        {
            if (stat.stream != null)
            {
                stat.stream.Close();
                stat.stream = null;
            }
            if (stat.tcp != null)
            {
                stat.tcp.Close();
                stat.tcp = null;
            }
            //Debug.Log("TCP connection closed");
        }
        catch (Exception e)
        {
            //Debug.LogError($"Close error: {e.Message}");
        }
    }

    // 이벤트 시스템
    public event EventHandler<UdpEventArgs> TcpDataReceived;

    public virtual void OnTcpDataReceived(UdpEventArgs e)
    {
        EventHandler<UdpEventArgs> handler = TcpDataReceived;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    void TcpDataReceivedProcess(object sender, UdpEventArgs e)
    {
        int size = e.bdata.Length;
        string msg = System.Text.Encoding.UTF8.GetString(e.bdata);
        UdpData data = JsonUtility.FromJson<UdpData>(msg);
        data.receivedTime = DateTime.Now;
    }
}