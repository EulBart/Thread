
/*
public struct V3 
{
    public float x;
    public float y;
    public float z;

    public V3(V3 other)
    {
        x = other.x;
        y = other.y;
        z = other.z;
    }

    public V3(ref V3 other)
    {
        x = other.x;
        y = other.y;
        z = other.z;
    }

    public V3(params float[] array)
    {
        switch (array.Length)
        {
            case 0: x = y = z = 0;break;
            case 1: x = array[0]; y = z = 0;break;
            case 2:  x = array[0]; y = array[1]; z = 0; break;
            default: x = array[0]; y = array[1]; z = array[2]; break;
        }
    }

    public static V3 operator+ (V3 a, V3 b)
    {
        return new V3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static V3 operator- (V3 a, V3 b)
    {
        return new V3(a.x - b.x, a.y-b.y, a.z - b.z);
    }

    public static V3 operator* (V3 a, float k)
    {
        return new V3(k*a.x, k*a.y, k* a.z);
    }

    public static V3 operator* (float k, V3 a)
    {
        return new V3(k*a.x, k*a.y, k* a.z);
    }

    public static V3 operator/ (V3 a, float k)
    {
        return new V3(a.x/k, a.y/k, a.z/k);
    }

    public static float operator* (V3 a, V3 b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }
}


using UnityEngine;

public struct Entity
{
    public Vector3 position;
    public Color color;
}
*/