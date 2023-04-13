using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PathExperiment : MonoBehaviour
{
    public SystemManager mSystemManager;
    public DataCommunicator mSender;
    public ParameterManager mParamManager;
    ExperimentParam mExParam;

    public Text mText;
    ArucoMarker marker = null;
    bool bReqMarker = true;

    void Awake()
    {
        mExParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];

        if (!mExParam.bPathTest)
        {
            enabled = false;
            return;
        }
    }
    void OnEnable()
    {
        MarkerDetectEvent.markerDetected += OnMarkerInteraction;
    }
    void OnDisable()
    {
        MarkerDetectEvent.markerDetected -= OnMarkerInteraction;
    }

    void OnMarkerInteraction(object sender, MarkerDetectEventArgs me)
    {
        marker = me.marker;
        int id = me.marker.id;

        if (marker.mbCreate && bReqMarker)
        {
            var pos = marker.gameobject.transform.position;
            var rot = marker.gameobject.transform.rotation;
            float angle = 0f;
            Vector3 axis = Vector3.zero;
            rot.ToAngleAxis(out angle, out axis);
            //axis = angle * Mathf.Deg2Rad * axis;
            axis = angle * axis;
            float[] fdata = new float[6];
            fdata[0] = pos.x;
            fdata[1] = pos.y;
            fdata[2] = pos.z;
            fdata[3] = axis.x;
            fdata[4] = axis.y;
            fdata[5] = axis.z;
            byte[] bdata = new byte[fdata.Length * 4];
            Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
            UdpData mdata = new UdpData("VO.MARKER.CREATE", mSystemManager.User.UserName, id, bdata, 1.0);
            StartCoroutine(mSender.SendData(mdata));
            bReqMarker = false;
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
