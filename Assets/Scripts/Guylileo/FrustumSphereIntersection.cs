

using System;
using System.Collections.Generic;
using OSG;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using Plane = OSG.Plane;

public class FrustumSphereIntersection
{
    private static Color[] colors =
    {
        Color.red, Color.green, Color.blue, Color.yellow
    };

    public readonly Vector3[] corners = new Vector3[5];
    private Camera main;
    private Vector3 center;
    private float radius;

    private Plane backPlane;
    private Plane[] sidePlanes = new Plane[4];
    private PlaneSphereIntersection backPlaneInter;
    private PlaneSphereIntersection[] planeInter = new PlaneSphereIntersection[4];
    //private RaySphereIntersection centerInter;
    private RaySphereIntersection[] cornersInter = new RaySphereIntersection[4];
    private Transform camTransform;
   
    public Vector3 camPosition
    {
        get { return camTransform.position;}
    }
        

    public FrustumSphereIntersection(Camera cam, Vector3 center, float radius)
    {
        main = cam;
        camTransform = cam.transform;
        SetSphere(center, radius);
    }

    public void SetCenter(Vector3 center)
    {
        this.center = center;
        Compute();
    }

    public void SetRadius(float radius)
    {
        this.radius = radius;
        Compute();
    }

    public void SetSphere(Vector3 center, float radius)
    {
        this.radius = radius;
        this.center = center;
        Compute();
    }   

    Vector3 seed;
    RaySphereIntersection seedInter;
    private bool[] moved = new bool[4];

    public void Compute()
    {
        main.CalculateFrustumCorners(main.rect,
            main.farClipPlane,
            Camera.MonoOrStereoscopicEye.Mono,
            corners);
        //centerInter = new RaySphereIntersection(camPosition, center-camPosition, center, radius);
        // compute world space coordinates for frustum
        for (var index = 0; index < 4; index++)
        {
            corners[index] = camTransform.TransformPoint(corners[index]);
        }
        corners[4] = corners[0];

        Vector3 p0 = camPosition;
        backPlane = Plane.SphereBackPlaneSeenFromPosition(p0, center, radius);
        backPlaneInter = new PlaneSphereIntersection(backPlane, center, radius);
        
        seed = center;
        for (int i = 0; i < 4; ++i)
        {
            Vector3 p1 = corners[i];
            Vector3 p2 = corners[i+1];

            Plane p = new Plane(p0, p2, p1);
            p.SetName(Vector3.Lerp(p1, p2, 0.5f), "SidePl" + i);
            sidePlanes[i] = p;
            planeInter[i] = new PlaneSphereIntersection(p, center, radius);
            cornersInter[i] = new RaySphereIntersection(p0, p1-p0, center, radius);
            moved[i] = MoveToPlaneIfOnNegativeSide(ref sidePlanes[i], ref seed);
        }

        seedInter = new RaySphereIntersection(camPosition, seed-camPosition, center, radius);
        if(seedInter.type != RaySphereIntersection.eType.InFront)
        {
            return;
        }

        v0 = v0 ?? new List<Vector3>();
        v0.Clear();
        Q0(v0);
    }

    private List<Vector3> v0;


    // Get the list for generating vertices in quadrant 0
    private void Q0(List<Vector3> result)
    {
        
        //int index0 = 0;
        //int index1 = 3;
        //if(moved[index0] || moved[index1])
        //    return;

        result.Add(seedInter.I);

        // Get a point downwards

        for(int i = 0; i < 4;++i)
        {
            if(moved[i])
                continue;
            Vector3 p = GetPointCLosestToPlane(i);
            result.Add(p);
        }

/*
        var ppI = new PlanePlaneIntersection(sidePlanes[planeIndex], backPlane);
        var lsI = new LineSphereIntersection(ppI.O, ppI.D, center, radius);

        if (lsI.type != LineSphereIntersection.eType.None)
        {
            result.Add(lsI.I0);
            if (lsI.type == LineSphereIntersection.eType.TwoPoints)
            {
                result.Add(lsI.I1);
            }
        }

        break;
        */

    }

    private Vector3 GetPointCLosestToPlane(int planeIndex)
    {
        switch (planeInter[planeIndex].type)
        {
            case PlaneSphereIntersection.eType.None:
                // find the point on the back plane
                // project the sphere's center on the down sidePlane
                // project it on the back intersection circle
                return backPlaneInter.ProjectOnCircle(sidePlanes[planeIndex].Projection(center));
                
            case PlaneSphereIntersection.eType.Circle:
                // the sphere is intersecting the lower plane
                // project the seed intersection on its intersection circle
                return planeInter[planeIndex].ProjectOnCircle(seedInter.I);
            case PlaneSphereIntersection.eType.Point:
                // the sphere is just tangent to the plane
                return planeInter[planeIndex].onPlane;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool MoveToPlaneIfOnNegativeSide(ref Plane sidePlane, ref Vector3 p0)
    {
        float distance = sidePlane.Distance(p0);
        if(distance>=0)
            return false;
        p0 -= sidePlane.normal * distance;
        return true;
    }


#if UNITY_EDITOR
    public void DrawGizmos()
    {
        Handles.color = Color.magenta;
        //backPlane.DrawGizmo(3);
        Handles.zTest = CompareFunction.LessEqual;
        backPlaneInter.DrawGizmo(0.01f);
        const float gizmoSize = 0.006125f;
        for (int i = 0; i < 4; ++i)
        {
            Handles.color = colors[i];
            sidePlanes[i].DrawGizmo(gizmoSize);
            planeInter[i].DrawGizmo(gizmoSize);
        }
        Handles.color = Color.cyan;
        for (var index = 0; index < v0.Count; index++)
        {
            Vector3 v = v0[index];
            Handles.DotHandleCap(0, v, Quaternion.identity, gizmoSize, Event.current.type);
            Handles.Label(v, index.ToString());
        }
    }
#endif
}
