

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
        Color.red, Color.green, Color.cyan, Color.yellow
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
        v0 = v0 ?? new List<Vector3>();
        v0.Clear();

        if(seedInter.type != RaySphereIntersection.eType.InFront)
        {
            return;
        }

        GetQuadrantPoints(v0, 0, 3);
        GetQuadrantPoints(v0, 1, 0);
        GetQuadrantPoints(v0, 2, 1);
        GetQuadrantPoints(v0, 3, 2);
    }

    private List<Vector3> v0;


    // Get the list for generating vertices in quadrant defined
    // by the two side planes of indices index0 and index1
    private void GetQuadrantPoints(List<Vector3> result, int index0, int index1)
    {
        if(moved[index0] || moved[index1])
            return;

        result.Add(seedInter.I);

        bool cornerIntersects = cornersInter[index0].type != RaySphereIntersection.eType.None;

        Vector3 p0 = GetSidePoint(index0, index1);
        result.Add(p0);
        if(!cornerIntersects)
        {
            AddPossibleExtraPoint(result, index0, index1);
        }
        else
        {
            result.Add(cornersInter[index0].I);
        }
        if(!cornerIntersects)
        {
            AddPossibleExtraPoint(result, index1, index0);
        }
        Vector3 p1 = GetSidePoint(index1, index0);
        result.Add(p1);
    }

    private void AddPossibleExtraPoint(List<Vector3> result, int index0, int index1)
    {
        if (planeInter[index0].type != PlaneSphereIntersection.eType.None)
        {
            // the plane intersects with the sphere, we have to add another
            // construction point on the intersection of this plane
            // and the backPlane (on the sphere too, obviously)
            var ppI = new PlanePlaneIntersection(sidePlanes[index0], backPlane);
            var lsI = new LineSphereIntersection(ppI.O, ppI.D, center, radius);

            if (lsI.type != LineSphereIntersection.eType.None)
            {
                if (lsI.type == LineSphereIntersection.eType.OnePoint)
                {
                    result.Add(lsI.I0);
                }
                else
                {
                    float d0 = sidePlanes[index1].Distance(lsI.I0);
                    float d1 = sidePlanes[index1].Distance(lsI.I1);
                    // returns the point closest to the other plane
                    result.Add(d0 < d1 ? lsI.I0 : lsI.I1);
                }
            }
        }
    }

    /// <summary>
    /// Get the side point for the main plane (index0) given the other plane of the quadrant
    /// we're building
    /// </summary>
    /// <param name="index0"></param>
    /// <param name="index1"></param>
    /// <returns></returns>
    private Vector3 GetSidePoint(int index0, int index1)
    {
        Vector3 p0 = GetPointClosestToPlane(index0);
        if (sidePlanes[index1].Distance(p0) > 0)
        {
            // the other plane doesn't exclude it from view, so this is it.
            return p0;
        }
        // we now have to find the corresponding point on the other plane
        PlanePlaneIntersection ppI;
        LineSphereIntersection lsI;
        if(planeInter[index0].type != PlaneSphereIntersection.eType.None)
        {
            // main plane d
            ppI = new PlanePlaneIntersection(sidePlanes[index0], sidePlanes[index1]);
            lsI = new LineSphereIntersection(ppI.O, ppI.D, center, radius);
            if (lsI.type != LineSphereIntersection.eType.None)
            {
                return lsI.I0;
                //result.Add(lsI.I0);
                //if (lsI.type == LineSphereIntersection.eType.TwoPoints)
                //{
                //    result.Add(lsI.I1);
                //}

            }
            Debug.LogError("Shit");
        }
        ppI = new PlanePlaneIntersection(sidePlanes[index1], backPlane);
        lsI = new LineSphereIntersection(ppI.O, ppI.D, center, radius);
        if(lsI.type != LineSphereIntersection.eType.None)
            return lsI.I0;
        Debug.LogError("And shit");
        return p0;
    }
    /// <summary>
    /// returns the point on the sphere, that's closest to the side plane of
    /// given index AND on the correct side of the plane (i.e. the side visible by the camera)
    /// </summary>
    /// <param name="planeIndex"></param>
    /// <returns></returns>
    private Vector3 GetPointClosestToPlane(int planeIndex)
    {
        switch (planeInter[planeIndex].type)
        {
            case PlaneSphereIntersection.eType.None:

                // the plane doesn't intersect with the sphere, we have to 
                // find the point on the back plane:
                // 1. project the sphere's center on the sidePlane
                // 2. project the result on the back intersection circle
                return backPlaneInter.ProjectOnCircle(
                    sidePlanes[planeIndex].Projection(center));
                
            case PlaneSphereIntersection.eType.Circle:
                // the sphere is intersecting the plane:
                // just project the seed intersection on its intersection circle
                return planeInter[planeIndex].ProjectOnCircle(seedInter.I);
            case PlaneSphereIntersection.eType.Point:
                // the sphere is just tangent to the plane, the
                // contact point is what we want
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
