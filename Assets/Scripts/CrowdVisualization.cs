using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CrowdVisualization : MonoBehaviour {
    public GameObject PersonPrefab;
    public string DataDay = "ATC-1";
    public string DataTime = "1000";
    public double CurrentTime;//模拟中的当前时间
    public Transform Root;
    public bool ShowTime = false;
    public float SimSpeed = 3.0f;
    private List<Person> Crowd = new List<Person>();
    private double LastOutputTime; //上次输出的时间
    private double Interval = 3.0; //输出的时间间隔
    private Dictionary<int, int> pidToGid = new Dictionary<int, int>();
    private Dictionary<int, Color> gidToColor = new Dictionary<int, Color>();

    void Start()
    {
        //读组信息
        string groupFile = Application.streamingAssetsPath + "/groups_" + DataDay + ".csv";
        string[] lines = File.ReadAllLines(groupFile);
        foreach (string line in lines)
        {
            string[] items = line.Split(',');
            int pid = int.Parse(items[0]);
            int gid = int.Parse(items[1]);
            pidToGid.Add(pid, gid);
            if (!gidToColor.ContainsKey(gid))
            {
                gidToColor.Add(gid, new Color(Random.value, Random.value, Random.value, 1.0f));
            }
        }

        string dataPath = Application.streamingAssetsPath + "/person_" + DataDay + "_" + DataTime;
        //所有CSV数据读入内存
        foreach (string filename in Directory.GetFiles(dataPath))
        {
            if (Path.GetExtension(filename) == ".csv")
            {
                ReadCSVData(filename);
            }
        }

        GetCurTime();
        LastOutputTime = CurrentTime;
    }
    
    void ReadCSVData(string filename)
    {
        Person person = new Person();
        string[] lines = File.ReadAllLines(filename);
        //跳过列名
        for (int i = 1; i < lines.Length; i++)
        {
            string[] items = lines[i].Trim().Split(',');
            person.Id = int.Parse(items[1]);

            Position pos = new Position();
            pos.Time = double.Parse(items[0]);
            pos.PosX = float.Parse(items[2]) / 1000f;
            pos.PosZ = float.Parse(items[3]) / 1000f;
            pos.PosY = float.Parse(items[4]) / 1000f;
            pos.Velocity = double.Parse(items[5]) / 1000.0;
            pos.VelocityDir = float.Parse(items[6]);
            pos.FacingDir = float.Parse(items[7]);
            person.Track.Add(pos);
        }

        if (pidToGid.ContainsKey(person.Id))//这句注掉，就是所有人都在，包括单人
        {
            person.GameObject = Instantiate(PersonPrefab, new Vector3(person.Track[person.CurrentIndex].PosX,
                0.0f, person.Track[person.CurrentIndex].PosZ), Quaternion.identity, Root);
            person.GameObject.name = person.Id.ToString();
            person.GameObject.transform.parent = Root;

            TrailRenderer tr = person.GameObject.AddComponent<TrailRenderer>();
            tr.material = new Material(Shader.Find("Particles/Additive"));

            Color color;
            //有单人的情况，并没有记录在组内
            if (pidToGid.ContainsKey(person.Id))
            {
                color = gidToColor[pidToGid[person.Id]];
            }
            else
            {
                color = new Color(Random.value, Random.value, Random.value, 1.0f);
            }
            tr.material.SetColor("_TintColor", color);
            tr.time = 1000;
            tr.startWidth = 0.1f;
            tr.endWidth = 0.1f;
            person.GameObject.SetActive(false);
            Crowd.Add(person);

        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Person person in Crowd)
        {
            //这个人已经结束了
            if (person.CurrentIndex >= person.Track.Count)
            {
                //结束了就关掉
                person.GameObject.SetActive(false);

                //结束了颜色变浅
//                TrailRenderer tr = person.GameObject.GetComponent<TrailRenderer>();
//                Color color = tr.material.GetColor("_TintColor");
//                color.a = 0.2f;
//                tr.material.SetColor("_TintColor", color);
                continue;
            }

            //到时间了开始移动
            if (CurrentTime >= person.Track[person.CurrentIndex].Time)
            {
                if (!person.GameObject.activeSelf)
                {
                    person.GameObject.SetActive(true);
                }
                Vector3 newPos = new Vector3(person.Track[person.CurrentIndex].PosX,
                    0.0f, person.Track[person.CurrentIndex].PosZ);
                Quaternion newRot = Quaternion.Euler(0.0f,
                    180f - person.Track[person.CurrentIndex].FacingDir * 180f / Mathf.PI, 
                    0.0f);
                //                person.GameObject.transform.localPosition = Vector3.Slerp(person.GameObject.transform.localPosition, newPos, Time.deltaTime);
                //                person.GameObject.transform.localRotation = Quaternion.Lerp(person.GameObject.transform.localRotation, newRot, Time.deltaTime);
                person.GameObject.transform.localPosition = newPos;
                person.GameObject.transform.localRotation = newRot;
                person.CurrentIndex++;
            }
        }

        CurrentTime = CurrentTime + Time.deltaTime*SimSpeed;
        if (ShowTime && CurrentTime - LastOutputTime > Interval)
        {
            Debug.Log(CurrentTime);
            LastOutputTime = CurrentTime;
        }
    }

    //从所有人的轨迹数据中提取最早的时间点作为模拟的当前时间起点
    void GetCurTime()
    {
        if (Crowd.Count < 1)
            return;

        CurrentTime = Crowd[0].Track[0].Time;

        foreach (Person person in Crowd)
        {
            CurrentTime = CurrentTime < person.Track[0].Time ? CurrentTime : person.Track[0].Time;
        }

    }
}
