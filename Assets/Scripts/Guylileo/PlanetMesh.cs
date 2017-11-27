using System.Collections;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class PlanetMesh : MonoBehaviour
{
    [SerializeField] private PlanetMeshSettings settings;

    private Container<Vector3> vertices;
    private Container<int> triangles;
    private Container<Vector3> normals;
    private Container<Vector2> uvs;

    MeshRenderer renderer;
    void OnEnable()
    {
        renderer = GetComponent<MeshRenderer>();
        

    }

    public void BuildMesh()
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
        GetNormalJob().RunSync(8);
        vertices = new Container<Vector3>(GetVertices().RunSync(8));
        GetUVS().RunSync(8);
    }

    private IEnumerator BuildMeshCoroutine()
    {
        yield return GetNormalJob().RunAsync(8);

        var result = GetVertices().RunAsync(8);
        yield return result;
        vertices = new Container<Vector3>(result.result);
        yield return GetUVS().RunAsync(8);
    }

    private MeshUVBuilderJob GetUVS()
    {
        uvs = new Container<Vector2>(settings.VerticeCount);
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
        normals.readOnly = false;
        normals = new Container<Vector3>(settings.VerticeCount);
        return new MeshNormalBuilderJob(normals, settings);
    }
}
