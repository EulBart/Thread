using System;
using OSG;
using UnityEngine;

public abstract class MeshBuilderJob<T> : Job<T> where T : struct
{
    protected readonly PlanetMeshSettings settings;
    protected MeshBuilderJob(Container<T> c, PlanetMeshSettings s) : base(c)
    {
        settings = s;
    }



}


public class MeshNormalBuilderJob : MeshBuilderJob<Vector3>
{
    public MeshNormalBuilderJob(Container<Vector3> source, PlanetMeshSettings settings) : base(source,settings)
    {
    }

    protected override ExecuteDelegate callback
    {
        get { return settings.IndexToNormal; }
    }
}

public class MeshUVBuilderJob : MeshBuilderJob<Vector2>
{
    public MeshUVBuilderJob(Container<Vector2> c, PlanetMeshSettings s) : base(c, s)
    {
    }

    protected override ExecuteDelegate callback
    {
        get { return ComputeUV; }
    }

    private void ComputeUV(int index, ref Vector2 e)
    {
        e = settings.IndexToCoordinates(index);
        e.x /= Mathf.PI;
        e.y /= Mathf.PI;
    }
}


public class MeshVerticesBuilderJob : MeshBuilderJob<Vector3>
{
    public MeshVerticesBuilderJob(Container<Vector3> source, PlanetMeshSettings settings) : base(source, settings)
    {
        if(!source.readOnly)
        {
            throw new Exception("Source must be readonly");
        }
    }

    protected override ExecuteDelegate callback
    {
        get {return ComputePosition;}
    }

    private void ComputePosition(int index, ref Vector3 e)
    {
        e *= settings.radius;
    }
}


