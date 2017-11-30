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
    private int[] tToV;
    private int triangleIndicesPerRow;

    public int TrianglesCount
    {
        get
        {
            return 8*count*count;
        }
    }

    public PlanetMeshSettings(Vector2 center, float delta, int count, float radius) : this()
    {
        this.center = center;
        this.delta = delta;
        this.count = count;
        this.radius = radius;
        tToV = new int[6];
        Init();
    }

    public void Init()
    {
        tToV = tToV??new int[6];
        sideVerticeCount = count * 2 + 1;
        origin = center - count * new Vector2(delta, delta);
        triangleIndicesPerRow = 12 * count;

        tToV[0] = 1;
        tToV[1] = 0;
        tToV[2] = sideVerticeCount;
        tToV[3] = sideVerticeCount+1;
        tToV[4] = 1;
        tToV[5] = sideVerticeCount;
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

    public int TriangleToVerticeIndex(int triangleIndex)
    {
        int rowNumber = triangleIndex / triangleIndicesPerRow;
        triangleIndex = triangleIndex % triangleIndicesPerRow;

        return rowNumber * sideVerticeCount + triangleIndex / 6 + tToV[triangleIndex % 6];
    }
}