using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Test_3 : MonoBehaviour
{
    public int instanceCount = 100000;
    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;
    
    private ComputeBuffer _positionBuffer;
    private ComputeBuffer _colorBuffer;
    private ComputeBuffer _argsBuffer;
    private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };

    private const int MAXSpace = 100;

    private int _cachedInstanceCount = -1;
    private int _cachedSubMeshIndex = -1;
    
    void Update()
    {
        // 更新Buffer
        UpdateBuffers();
        
        // 设置渲染包围盒 影响culling
        Bounds renderBounds = new Bounds(Vector3.zero, new Vector3(MAXSpace, MAXSpace, MAXSpace));
        Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, renderBounds, _argsBuffer);
    }

    void UpdateBuffers()
    {
        // 不需要更新时返回
        if ((_cachedInstanceCount == instanceCount || _cachedSubMeshIndex != subMeshIndex)
            && _argsBuffer != null)
            return;
        // 规范subMeshIndex
        if (instanceMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);
        //初始化位置Buffer
        _positionBuffer?.Release();
        _positionBuffer = new ComputeBuffer(instanceCount, sizeof(float) * 4); // float4 ==这东西也不知道有没有上线
        //初始化颜色buffer
        _colorBuffer?.Release();
        _colorBuffer = new ComputeBuffer(instanceCount, sizeof(float) * 4); // float4
        
        Vector4[] positions = new Vector4[instanceCount];
        Vector4[] colors = new Vector4[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            float size = Random.Range(0.05f, 1f);
            Vector3 pos = Random.insideUnitSphere * MAXSpace;
            positions[i] = new Vector4(pos.x, pos.y, pos.z, size);
            colors[i] = Random.ColorHSV();
        }
        
        _positionBuffer.SetData(positions); 
        instanceMaterial.SetBuffer("positionBuffer", _positionBuffer);
        
        _colorBuffer.SetData(colors);
        instanceMaterial.SetBuffer("colorsBuffer", _colorBuffer);
        
        // Indirect args 直接复制官方案例
        _argsBuffer?.Release();
        _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (instanceMesh != null)
        {
            _args[0] = (uint) instanceMesh.GetIndexCount(subMeshIndex);
            _args[1] = (uint) instanceCount;
            _args[2] = (uint) instanceMesh.GetIndexStart(subMeshIndex);
            _args[3] = (uint) instanceMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            _args[0] = _args[1] = _args[2] = _args[3] = 0;
        }

        _argsBuffer.SetData(_args);
        
        _cachedInstanceCount = instanceCount;
        _cachedSubMeshIndex = subMeshIndex;
    }
    
    void OnDisable()
    {
        _argsBuffer?.Release();
        _argsBuffer = null;
        
        _colorBuffer?.Release();
        _colorBuffer = null;
    }
}