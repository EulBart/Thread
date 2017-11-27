using System;
using UnityEngine;
[Serializable]
public struct PlanetMeshSettings
{
    public Vector2 center; // x = longitude, y = latitude
    public float delta;
    public int count; // number of squares in all 4 directions
    public float radius;

    private int sideVerticeCount;
    private Vector2 origin;

    public PlanetMeshSettings(Vector2 center, float delta, int count, float radius) : this()
    {
        this.center = center;
        this.delta = delta;
        this.count = count;
        this.radius = radius;
        Init();
    }

    public void Init()
    {
        sideVerticeCount = count * 2 + 1;
        origin = center - count * new Vector2(delta, delta);
    }

    public int SideVerticeCount
    {
        get { return sideVerticeCount; }
    }

    public int VerticeCount
    {
        get{return sideVerticeCount*sideVerticeCount;}
    }

    public void CoordinatesToNormal(Vector2 coordinates, ref Vector3 normal)
    {
        float cosl = Mathf.Cos(coordinates.y);
        float sinl = Mathf.Sin(coordinates.y);
        float cosL = Mathf.Cos(coordinates.x);
        float sinL = Mathf.Sin(coordinates.x);
        normal.x = cosl * cosL;
        normal.y = sinl;
        normal.z = cosl * sinL;
    }

    public Vector2 IndexToCoordinates(int index)
    {
        int x = index % sideVerticeCount;
        int y = index / sideVerticeCount;
        return origin + new Vector2(x*delta, y*delta);
    }

    public void IndexToNormal(int index, ref Vector3 normal)
    {
        CoordinatesToNormal(IndexToCoordinates(index), ref normal);
    }
}