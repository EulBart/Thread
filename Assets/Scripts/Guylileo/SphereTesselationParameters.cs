using System;
using UnityEngine;

[Serializable]
public struct SphereTesselationParameters
{
    [SerializeField] [Range(0, 5)] public int recursionCount;
    [SerializeField] [Range(0, 90)] public float openingAngle;
    [SerializeField] public float radius;
    [SerializeField] public bool whole;

    [HideInInspector] public int triangleCount;
    [HideInInspector] public int pointsCount;
    [HideInInspector] public Camera camera;

    public bool Validate()
    {
        if(recursionCount < 0)
            recursionCount = 0;

        if(openingAngle > 90)
            openingAngle = 90;
        if(openingAngle < 0)
            openingAngle = 0;
        triangleCount = 6 * (1<<(2*recursionCount)); // 6 * 4 ^ recursionCount

        pointsCount = 3 * triangleCount;

        if(whole)
        {
            pointsCount *= 2;
            triangleCount *= 2;
        }

        return camera;
    }

}