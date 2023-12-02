using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RealObject: MonoBehaviour
{
    //public GameObject obj;
    public int mnObjID;
    public RealObject()
    {
        ConnectedObjs = new List<GameObject>();
    }
    //public RealObject(GameObject prefab, int id, float sx, float sy, float sz)
    //{
    //    //this.mnObjID = id;
    //    //this.obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
    //    ////this.obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    //    //this.obj.transform.localScale = new Vector3(sx, sy, sz);
    //    //this.obj.GetComponent<Renderer>().material.color = Color.green;
    //    //this.obj.transform.tag = "RO";
    //    //var sphereCollider = obj.GetComponent<SphereCollider>();
    //    //sphereCollider.transform.localScale = new Vector3(sx, sy, sz);
    //    //this.obj.AddComponent<RealObject>();
    //}
    public void Init(int id) {
        this.mnObjID = id;
    }
    public void AddObject(GameObject obj)
    {
        if(!ConnectedObjs.Contains(obj))
            ConnectedObjs.Add(obj);
    }
    public void RemoveObject(GameObject obj)
    {
        if(ConnectedObjs.Contains(obj))
            ConnectedObjs.Remove(obj);
    }
    public List<GameObject> ConnectedObjs;
}

public class ObjectManager : MonoBehaviour
{
    public Dictionary<int, GameObject> RealObjDict;
    public Text mText;
    public GameObject prefab;
    void Awake()
    {
        RealObjDict = new Dictionary<int, GameObject>();
        enabled = false;
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    Matrix4x4 invertYMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
    public void UpdateObjectPose(int oid, ref float[] pose) {
        try
        {
            if (RealObjDict.ContainsKey(oid))
            {
                var ro = RealObjDict[oid];
                Matrix3x3 R = new Matrix3x3(
                       pose[0], pose[1], pose[2],
                       pose[3], pose[4], pose[5],
                       pose[6], pose[7], pose[8]
                   );
                Vector3 t = new Vector3(pose[9], pose[10], pose[11]);
                Matrix4x4 ARM = invertYMatrix * Matrix4x4.TRS(t, R.GetQuaternion(), Vector3.one) * invertYMatrix;
                var T = Camera.main.transform.localToWorldMatrix * ARM;
                ro.transform.position = new Vector3(T.m03, T.m13, T.m23);
                var ro2 = ro.GetComponent<RealObject>();
                foreach(GameObject vo in ro2.ConnectedObjs)
                {
                    vo.transform.position = ro.transform.position;
                }
            }
        }
        catch(Exception e)
        {
            mText.text = "obj pose err = "+e.ToString();
        }
    }
    GameObject CreateObj(int id, float sx, float sy, float sz)
    {
        GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        obj.transform.localScale = new Vector3(sx, sy, sz);
        obj.GetComponent<Renderer>().material.color = Color.green;
        obj.transform.tag = "RO";
        var sphereCollider = obj.GetComponent<SphereCollider>();
        sphereCollider.transform.localScale = new Vector3(sx, sy, sz);
        var ro = obj.AddComponent<RealObject>();
        ro.Init(id);
        return obj;
    }
    public void UpdateObjFromServer(int idx, ref float[] fdata)
    {
        try
        {
            int Nobj = (int)fdata[idx + 1];
            int oid = (int)fdata[idx + 2];
            if (RealObjDict.ContainsKey(oid))
            {

            }
            else
            {
                //»ý¼º
                float sx = fdata[idx + 3];
                float sy = fdata[idx + 4];
                float sz = fdata[idx + 5];
                var ro = CreateObj(oid, sx, sy, sz);//new RealObject(sx, sy, sz);
                RealObjDict.Add(oid, ro);
                //ro.obj.AddComponent<SphereCollider>();
                
            }
        }
        catch (Exception e)
        {
            mText.text = "obj cre err = " + e.ToString();
        }
    }
}
