using System;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public abstract class SphereMeshBuilder : MonoBehaviour
{
    
    protected Container<Vector3> vertices;
    protected Container<int> triangles;
    protected Container<Vector3> normals;
    protected Container<Vector2> uvs;

    private MeshFilter _meshFilter;

    private MeshRenderer _mR;

    public abstract float radius
    {
        get;
    }

    protected MeshRenderer meshRenderer
    {
        get
        {
            if(!_mR)
                _mR = GetComponent<MeshRenderer>();
            return _mR;
        }
    }

    protected MeshFilter meshFilter
    {
        get
        {
            if(!_meshFilter)
            {
                _meshFilter = GetComponent<MeshFilter>();
            }
            return _meshFilter;
        }
    }


    public abstract void BuildMesh(Observer o);

    protected void AssignMesh()
    {
        try
        {
            Mesh mesh = new Mesh
            {
                vertices = vertices.Array,
                normals = normals.Array,
                triangles = triangles.Array
            };
            mesh.RecalculateBounds();
            meshFilter.mesh = mesh;
        }
        catch(Exception e)
        {
            Debug.LogException(e);
        }
    }



}
