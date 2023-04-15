using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectPath : MonoBehaviour
{
    public int contentID;
    bool mbInit;
    public ObjectState mObjState;
    Vector3 s, e;
    float speed;
    float startTime;
    string[] logString = new string[4];
    Text mText;

    LineRenderer lineRenderer;
    float journeyLength;

    void Awake()
    {
        mbInit = false;
        mObjState = ObjectState.None;
        logString[0] = "NONE";
        logString[1] = "Manipulation";
        logString[2] = "Moving";
        logString[3] = "OnPath";
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (mbInit)
        {
            lineRenderer.SetPosition(0, s);
            lineRenderer.SetPosition(1, e);
            Move();
        }
    }
    public void Init(int id, Vector3 _s, Vector3 _e, Text _text)
    {
        contentID = id;
        s = _s;
        e = _e;
        speed = 0.2f;
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material.color = new Color(0f, 1f, 1f, 0.2f);
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        journeyLength = Vector3.Distance(s,e);
        mbInit = true;
        mText = _text;
    }

    public void MoveStart()
    {
        //move를 받으면 시작
        mObjState = ObjectState.Moving;
        startTime = Time.time;

        try {
            //var animator = GetComponentInParent<Animation>();
            //animator.enabled = true;
            //animator.wrapMode = WrapMode.Loop;
            ////animator.SetBool("mixamo.com", true);
            //animator.Play("mixamo_com");

        }
        catch(Exception ex)
        {
            mText.text = ex.ToString();
        }
    }
    public void MoveEnd()
    {
        mObjState = ObjectState.None;
    }

    public void Move()
    {
        if (mObjState == ObjectState.Moving)
        {
            float distCovered = (Time.time - startTime) * speed;
            float fractionOfJourney = distCovered / journeyLength;
            transform.position = Vector3.Lerp(s,e, fractionOfJourney);
        }
    }
}
