using System.Collections;
using OSG;
using OSG.Debug;
using UnityEngine;

public class TesselatedSpherePatchBuilder : MeshBuilder
{
    [SerializeField] private SphereTesselationParameters parameters;

    private void OnValidate()
    {
        StartCoroutine(BuildMesh());
    }

    private IEnumerator BuildMesh()
    {
        Compute();
        yield break;
    }

    private static  Vector3 CoordinatesToNormal(Vector2 coordinates)
    {
        float cosl = Mathf.Cos(coordinates.y);
        float sinl = Mathf.Sin(coordinates.y);
        float cosL = Mathf.Cos(coordinates.x);
        float sinL = Mathf.Sin(coordinates.x);
        return new Vector3(cosl * cosL, sinl, cosl * sinL);
    }

    private int AddPoint(Vector3 p)
    {
        return triangles.Add(normals.Add(p));
    }

    private void Compute()
    {
        if(!parameters.Validate())
            return;

       

        Camera camera = parameters.camera;

        Vector3 localPos = camera.transform.TransformPoint(transform.position);
        if(localPos.z < 0)
        {
            meshRenderer.enabled=false;
            return;
        }

        meshRenderer.enabled = true;

        Vector3[] corners = new Vector3[4];
        camera.CalculateFrustumCorners(new Rect(0,0,1,1),
            camera.farClipPlane,Camera.MonoOrStereoscopicEye.Mono, corners);
        
        Vector3?[] intersecs = new Vector3?[4];

        Color[] colors =
        {
            Color.red, Color.green, Color.blue, Color.yellow
        };

        Vector3 position = camera.transform.position;
        for(int i = 0; i < 4; ++i)
        {
            var direction = corners[i] - position;
            DebugUtils.DrawArrow(position, direction, colors[i], 10);
            //Vector3 direction, Vector3 sphereCenter, float sphereRadius)
            intersecs[i] = position.RayIntersectSphere(direction, position, parameters.radius);
            if(intersecs[i].HasValue)
            {
                DebugUtils.DrawStar(intersecs[i].Value, Color.white, 0.1f, 10);
            }
        }



        normals = new Container<Vector3>(parameters.pointsCount);
        triangles = new Container<int>(parameters.pointsCount);

        Vector3 start = camera.transform.localPosition.normalized;
            
            //CoordinatesToNormal(parameters.startPosition * Mathf.Deg2Rad);
        float k = Mathf.Tan(parameters.openingAngle*Mathf.Deg2Rad);
        Vector3 p = start + k * camera.transform.forward;
        p.Normalize();
        Quaternion q = Quaternion.AngleAxis(60, start);

        for(int i = 0; i < 6; ++i)
        {
            AddPoint(start);
            AddPoint(p);
            p = q * p;
            AddPoint(p);
        }

        if(parameters.recursionCount > 0)
        {
            Subdivide(parameters.recursionCount);
        }

        vertices = new Container<Vector3>(normals);
        var v = vertices.Array;
        for (var index = 0; index < v.Length; index++)
        {
            v[index] *= parameters.radius;
        }

        AssignMesh();
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.DrawSphere(transform.position, parameters.radius);
    //}

    private void Run()
    {
        Subdivide(parameters.recursionCount);
    }

    private void Subdivide(int r)
    {
        int max = triangles.Count;
        for(int i = 0; i < max; i += 3)
        {
            DoTriangle(i);
        }  
        if(r>1)
        {
            Subdivide(r-1);
        }
    }

    private void DoTriangle(int triangleIndex0)
    {
        int triangleIndex1 = triangleIndex0 + 1;
        int triangleIndex2 = triangleIndex1 + 1;

        int o0 = triangles[triangleIndex0];
        int o1 = triangles[triangleIndex1];
        int o2 = triangles[triangleIndex2];

        var nv0 = Vector3.Lerp(normals[o0], normals[o1], 0.5f).normalized;
        var nv1 = Vector3.Lerp(normals[o1], normals[o2], 0.5f).normalized;
        var nv2 = Vector3.Lerp(normals[o2], normals[o0], 0.5f).normalized;

        // replace old triangle by first new one (first old index remains unchanged)
        triangles[triangleIndex1] = normals.Add(nv0);
        triangles[triangleIndex2] = normals.Add(nv2);

        // Add the new triangles
        AddPoint(nv0); triangles.Add(o1); AddPoint(nv1);
        AddPoint(nv2); AddPoint(nv1); triangles.Add(o2);
        AddPoint(nv0); AddPoint(nv1); AddPoint(nv2);
    }

    public override void BuildMesh(Observer o)
    {
        parameters.camera = o.main;
        StartCoroutine(BuildMesh());
    }
}
