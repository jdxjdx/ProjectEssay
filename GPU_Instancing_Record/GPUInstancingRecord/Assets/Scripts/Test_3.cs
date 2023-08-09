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
    
    private ComputeBuffer _matricesBuffer;
    private ComputeBuffer _colorBuffer;
    private ComputeBuffer _argsBuffer;
    private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };

    private const int MAXSpace = 100;

    private int _cachedInstanceCount = -1;
    private int _cachedSubMeshIndex = -1;
    
    // 设置物体包围盒最小点
    public Vector3 objectBoundMin;
    // 设置物体包围盒最大点
    public Vector3 objectBoundMax;
    // 设置ComputeShader
    public ComputeShader cullingComputeShader;
    // ComputeShader中内核函数索引
    private int _kernel = 0;
    // 当前可见物体的instanceID Buffer   
    private ComputeBuffer _visibleIDsBuffer;
    // 相机的视锥平面
    Plane[] cameraFrustumPlanes = new Plane[6];
    // 传入ComputeShader的视锥平面  
    Vector4[] frustumPlanes = new Vector4[6];
    void Start()
    {
        _kernel = cullingComputeShader.FindKernel("CSMain");
    }
    
    void Update()
    {
        // 更新Buffer
        UpdateBuffers();
        
        // 视锥剔除
        GeometryUtility.CalculateFrustumPlanes(Camera.main, cameraFrustumPlanes);
        for (int i = 0; i < cameraFrustumPlanes.Length; i++)
        {
            var normal = -cameraFrustumPlanes[i].normal;
            frustumPlanes[i] = new Vector4(normal.x, normal.y, normal.z, -cameraFrustumPlanes[i].distance);
        }
        
        _visibleIDsBuffer.SetCounterValue(0);//初始化计数器数值
        cullingComputeShader.SetVectorArray("_FrustumPlanes", frustumPlanes);
        cullingComputeShader.Dispatch(_kernel, Mathf.CeilToInt(instanceCount / 640f), 1, 1);
        ComputeBuffer.CopyCount(_visibleIDsBuffer, _argsBuffer, sizeof(uint));//获取计数器的值 从src拷贝到dst中 dstOffectBytes为在dst当中的值 在dx11平台dst类型必须为raw或者indirectArguments 其他平台可任意

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
        //初始化物体复合变换矩阵Buffer
        _matricesBuffer?.Release();
        _matricesBuffer = new ComputeBuffer(instanceCount, sizeof(float) * 16);   // float4x4;
        Matrix4x4[] trs = new Matrix4x4[instanceCount];
        //初始化颜色buffer
        _colorBuffer?.Release();
        _colorBuffer = new ComputeBuffer(instanceCount, sizeof(float) * 4); // float4
        
        Vector4[] colors = new Vector4[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            float size = Random.Range(0.05f, 1f);
            Vector3 pos = Random.insideUnitSphere * MAXSpace;
            colors[i] = Random.ColorHSV();
            trs[i] = Matrix4x4.TRS(pos, Random.rotationUniform, new Vector3(size, size, size));
        }
        
        _matricesBuffer.SetData(trs); 
        instanceMaterial.SetBuffer("matricesBuffer", _matricesBuffer);
        
        // 新增： 可见实例 Buffer
        _visibleIDsBuffer?.Release();
        _visibleIDsBuffer = new ComputeBuffer(instanceCount, sizeof(uint), ComputeBufferType.Append);//	Appends a value to the end of the buffer.允许动态添加删除元素
        instanceMaterial.SetBuffer("visibleIDsBuffer", _visibleIDsBuffer);
        
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
        
        // ComputeShader
        cullingComputeShader.SetVector("_BoundMin", objectBoundMin);
        cullingComputeShader.SetVector("_BoundMax", objectBoundMax);
        cullingComputeShader.SetBuffer(_kernel, "_MatricesBuffer", _matricesBuffer);
        cullingComputeShader.SetBuffer(_kernel, "_VisibleIDsBuffer", _visibleIDsBuffer);
        
        _cachedInstanceCount = instanceCount;
        _cachedSubMeshIndex = subMeshIndex;
    }
    
    void OnDisable()
    {
        _matricesBuffer?.Release();
        _matricesBuffer = null;
        
        _visibleIDsBuffer?.Release();
        _visibleIDsBuffer = null;
        
        _argsBuffer?.Release();
        _argsBuffer = null;
        
        _colorBuffer?.Release();
        _colorBuffer = null;
    }
}
