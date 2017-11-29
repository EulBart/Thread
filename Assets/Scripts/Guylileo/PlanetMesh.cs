using System;
using System.Collections;
using UnityEngine;
using OSG;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PlanetMesh : MonoBehaviour
{
    [SerializeField] private PlanetMeshSettings settings;
    [SerializeField] private bool enableBuild;
    [SerializeField] [Range(1,64)] private int threadCount=8;

    private Container<Vector3> vertices;
    private Container<int> triangles;
    private Container<Vector3> normals;
    private Container<Vector2> uvs;
    
    public bool IsBuilding
    {
        get;
        private set;
    }

    MeshFilter _meshFilter;
    public MeshFilter meshFilter
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

    void OnEnable()
    {
    }

    public void BuildMesh(float degLongitude, float degLatitude)
    {
        if(IsBuilding || !enableBuild)
            return;

        Vector2 newCenter = new Vector2(degLongitude, degLatitude) * Mathf.Deg2Rad;
        if(newCenter != settings.center)
        {
            settings.center = newCenter;
            BuildMesh();
        }
    }

    private void OnValidate()
    {
        if(IsBuilding || !enableBuild)
            return;
        BuildMesh();
    }

    private void BuildMesh()
    {
        settings.Init();
#if UNITY_EDITOR
        if(!EditorApplication.isPlaying)
            SyncBuildMesh();
        else
#endif
            StartCoroutine(BuildMeshCoroutine());
    }
    
    public void SyncBuildMesh()
    {
        IsBuilding = true;
        GetNormalJob().RunSync(threadCount);
        vertices = new Container<Vector3>(GetVertices().RunSync(threadCount));
        GetUVS().RunSync(threadCount);
        GetTriangles().RunSync(threadCount);
        AssignMesh();
        IsBuilding = false;
    }


    private IEnumerator BuildMeshCoroutine()
    {
        IsBuilding=true;
        yield return GetNormalJob().RunAsync(threadCount);
        var result = GetVertices().RunAsync(threadCount);
        yield return result;
        vertices = new Container<Vector3>(result.result);
        yield return GetUVS().RunAsync(threadCount);
        yield return GetTriangles().RunAsync(threadCount);
        AssignMesh();
        IsBuilding = false;
    }

    private void AssignMesh()
    {
        try
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.Array;
            mesh.normals = normals.Array;
            mesh.uv = uvs.Array;
            mesh.triangles = triangles.Array;
            mesh.RecalculateBounds();
            meshFilter.mesh = mesh;
        }
        catch(Exception e)
        {
            Debug.LogException(e);
        }
    }

    private MeshUVBuilderJob GetUVS()
    {
        uvs = new Container<Vector2>(settings.VerticeCount,true);
        return new MeshUVBuilderJob(uvs, settings);
    }

    private MeshVerticesBuilderJob GetVertices()
    {
        normals.readOnly = true;
        MeshVerticesBuilderJob meshVerticesBuilder = new MeshVerticesBuilderJob(normals, settings);
        return meshVerticesBuilder;
    }

    private MeshNormalBuilderJob GetNormalJob()
    {
        normals = new Container<Vector3>(settings.VerticeCount, true);
        return new MeshNormalBuilderJob(normals, settings);
    }

    private MeshTriangleBuilderJob GetTriangles()
    {
        triangles = new Container<int>(settings.TrianglesCount*3, true);
        return new MeshTriangleBuilderJob(triangles, settings);
    }

}
