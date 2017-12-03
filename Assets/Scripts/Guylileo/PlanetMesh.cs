using System;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlanetMesh : MeshBuilder
{
    [SerializeField] private PlanetMeshSettings settings;
    [SerializeField] private bool enableBuild;
    [SerializeField] [Range(1,64)] private int threadCount=8;


    private bool IsBuilding
    {
        get;
        set;
    }


    public override void BuildMesh(Observer o)
    {
        if(IsBuilding || !enableBuild)
            return;

        Vector2 newCenter = o.GetCoordinates() * Mathf.Deg2Rad;
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

    private void SyncBuildMesh()
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
