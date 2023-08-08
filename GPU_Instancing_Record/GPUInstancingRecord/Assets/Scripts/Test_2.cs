using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_2 : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public int instanceCount = 100000;
    private const int MAXOneTimeInstanceCount = 1023;

    private List<DrawInstanced> _drawInstancedList = new List<DrawInstanced>();
    void Start()
    {
        int count = instanceCount / MAXOneTimeInstanceCount;
        int remainder = instanceCount % MAXOneTimeInstanceCount;

        for (int i = 0; i < count; i++)
        {
            DrawInstanced drawInstanced = new DrawInstanced();
            drawInstanced.Init(MAXOneTimeInstanceCount, mesh, material);
            _drawInstancedList.Add(drawInstanced);
        }
        
        DrawInstanced remainderDrawInstanced = new DrawInstanced();
        remainderDrawInstanced.Init(remainder, mesh, material);
        _drawInstancedList.Add(remainderDrawInstanced);
    }

    void Update()
    {
        for (int i = 0; i < _drawInstancedList.Count; i++)
        {
            _drawInstancedList[i].Draw();
        }
    }
}
