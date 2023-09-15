using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchTest : MonoBehaviour
{
    public Text mText;
    public RawImage rawImage;

    public SystemManager mManager;
    public DataCommunicator mSender;

    // Start is called before the first frame update
    void Start()
    {
    }

    IEnumerator ChangeColor(Vector4 c)
    {
        rawImage.color = c;
        yield return new WaitForSecondsRealtime(0.3f);
        rawImage.color = new Vector4(0f, 0f, 0f, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        int nTouchCount = Input.touchCount;

        if (nTouchCount > 0)
        {
            //StartCoroutine(ChangeColor(new Vector4(1f, 0f, 0f, 0.3f)));

            Touch touch = Input.GetTouch(0);
            var phase = touch.phase;

            Ray raycast = Camera.main.ScreenPointToRay(touch.position);
            RaycastHit raycastHit;
            GameObject obj = null;
            bool bHit = Physics.Raycast(raycast, out raycastHit);
            if (bHit)
            {
                var btouchObject = raycastHit.collider.gameObject;
                if(btouchObject.transform.parent.tag == "Path")
                {
                      
                    var objPath = btouchObject.GetComponentInParent<ObjectPath>();
                    //var animator = btouchObject.GetComponentInParent<Animator>();
                    //objPath.MoveStart();
                    //mText.text = "path test "+ animator.GetCurrentAnimatorClipInfoCount(0);
                    //move and 전송
                    float[] fdata = new float[5];
                    byte[] bdata = new byte[fdata.Length * 4];
                    Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                    UdpData mdata = new UdpData("VO.REQMOVE", mManager.User.UserName, objPath.contentID, bdata, 1.0);
                    StartCoroutine(mSender.SendData(mdata));
                }
                if (btouchObject.transform.tag == "RO")
                {
                    mText.text = "RO RO RO RO aaaaaaaaaaaaaaa";
                }
                if (btouchObject.transform.parent.tag == "RO")
                {
                    mText.text = "RO RO RO RO";
                }
                mText.text = btouchObject.transform.tag;
                //mText.text = "Touch "+ parentObj.tag;
            }
            //else
            //{
            //    mText.text = "hit error";
            //}
        }
    }
}
