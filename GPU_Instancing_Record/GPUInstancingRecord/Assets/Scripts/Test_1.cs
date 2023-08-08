using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_1 : MonoBehaviour
{
    public int instanceCount = 100000;

    public GameObject InstanceGameObject;
    
    private const int MAXSpace = 100;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < instanceCount; i++)
        {
            GameObject go = GameObject.Instantiate(InstanceGameObject);
            go.transform.position = Random.insideUnitSphere * MAXSpace;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
