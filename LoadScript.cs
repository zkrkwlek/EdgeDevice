using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

//������� ���� �����ϱ� ���ؼ�
//��ũ��Ʈ �Ķ���͵� ���̽����� �����ϵ��� ����
//abstract class�� �ʿ���.
//�⺻�н�/data/param/���� ��ũ��Ʈ ���� ������ �о������ �ϱ�.

[Serializable]
public class LoadScriptParam
{
    public string load_scripts;
}

public class LoadScript : MonoBehaviour
{
    public Text mText;
    public string load_scripts;
    string dirPath, filename;
    LoadScriptParam param;
    bool WantsToQuit()
    {
        File.WriteAllText(filename, JsonUtility.ToJson(param));
        return true;
    }

    void Awake()
    {
        dirPath = Application.persistentDataPath + "/data";
        filename = dirPath + "/Scripts.json";
        try
        {
            string strAddData = File.ReadAllText(filename);
            param = JsonUtility.FromJson<LoadScriptParam>(strAddData);
            this.load_scripts = param.load_scripts;
            //mText.text = "success load " + load_scripts;
        }
        catch (Exception e)
        {
            param = new LoadScriptParam();
            param.load_scripts= "UVR.TestManager";
            mText.text = e.ToString();   
            this.load_scripts = "UVR.TestManager";
        }

        try {
            //�� ������ �Ľ��ؾ� ��
            //','
            //'.'
            
            string[] splited = load_scripts.Split(','); //
            for (int i = 0; i < splited.Length; i++)
            {
                string[] scripts = splited[i].Split('.');
                int len = scripts.Length;
                string objName = scripts[0];
                string scriptName = "";

                var obj = GameObject.Find(objName);
                if (!obj.activeSelf)
                {
                    obj.SetActive(true);
                }
               
                if (len == 2)
                {
                    //mText.text = "enable = " + scriptName;
                    scriptName = scripts[1];
                    (obj.GetComponent(scriptName) as MonoBehaviour).enabled = true;
                }
                //���̰� 1�̸� ���ӿ�����Ʈ
                //���̰� 2�̸� ��ũ��Ʈ ������.
            }
        }
        catch (Exception e)
        {
            //mText.text = "load script = "+e.ToString();
        }
        

        Application.wantsToQuit += WantsToQuit;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        enabled = false;
    }
}
