using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawInstanced
{
    private Mesh _mesh;
    private Material _material;
    private int _instanceCount = 100;

    private Matrix4x4[] _matrices;
    private Vector4[] _colors;
    private MaterialPropertyBlock _properties;

    public void Init(int instancedCount, Mesh mesh, Material material)
    {
        this._mesh = mesh;
        this._material = material;
        this._instanceCount = instancedCount;

        _matrices = new Matrix4x4[_instanceCount];
        _colors = new Vector4[_instanceCount];
        _properties = new MaterialPropertyBlock();
        
        for (int i = 0; i < _instanceCount; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 100f;
            Quaternion rot = Random.rotation;
            Vector3 scale = Vector3.one * Random.Range(0.5f, 1.5f);
            _matrices[i] = Matrix4x4.TRS(pos, rot, scale);
            _colors[i] = Random.ColorHSV();
        }
        
        _properties.SetVectorArray("_BaseColor", _colors);
    }

    public void Draw()
    {
        Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, _instanceCount, _properties, UnityEngine.Rendering.ShadowCastingMode.On, true, 0, null, UnityEngine.Rendering.LightProbeUsage.Off, null);
    }
}
