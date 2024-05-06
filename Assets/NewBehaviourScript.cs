using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        List<ME> mEs = new List<ME>();
        mEs.Add(new ME("aaa"));
        mEs.Add(new ME("bbb"));
        mEs.Add(new ME("ccc"));
        mEs[0] = mEs[2];
        mEs.RemoveAt(2);
        Debug.Log(mEs.Count);
        Debug.Log(mEs[0].name);
        Debug.Log(mEs[1].name);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
public class ME
{
    public string name;
    public ME(string na)
    {
        name = na;
    }
}
