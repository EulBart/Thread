

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
    private Plane[] plane = new Plane[4];

    // at index i, find the intersection between circle i and circle i+1
    private Vector3?[] circleInter = new Vector3?[4];

    private PlaneSphereIntersection backPlaneInter;
    private PlaneSphereIntersection[] planeInter = new PlaneSphereIntersection[4];
    //private RaySphereIntersection centerInter;
    private RaySphereIntersection[] cornersInter = new RaySphereIntersection[4];
    private Transform camTransform;

    /// <summary>
    /// starting point of mesh generation...
    /// basically, the closest point to the sphere's center
    /// that's in the frustum.
    /// </summary>
    Vector3 seed;
    /// <summary>
    /// Intersection of the vector camera to seed and sphere
    /// </summary>
    RaySphereIntersection seedInter;

    /// <summary>
    /// moved[i] is true if center of the sphere is on the non visible
    /// side of sidePlane[i]
    /// </summary>
    private bool[] moved = new bool[4];

    private readonly SegmentList allSegments;
    public class CircleSegment
    {
        public readonly Vector3 center;
        public readonly Vector3 normal;
        public readonly Vector3 from;
        public readonly Vector3 to;

        public readonly float radius;
        public readonly float angle;

        public CircleSegment(Vector3 center, Vector3 n, Vector3 start, Vector3 end)
        {
            this.center = center;
            @from = start-center;
            to = (end-center).normalized;
            radius = @from.magnitude;
            @from /= radius;
            //Quaternion q=Quaternion.FromToRotation(@from, to);
            //q.ToAngleAxis(out angle, out normal);

            normal = Vector3.Cross(@from, to);
            float sin = normal.magnitude;
            normal /= sin;
            float cos = Vector3.Dot(@from, to);
            angle = Mathf.Atan2(sin, cos) * Mathf.Rad2Deg;
            if(Vector3.Dot(n, normal)<0)
            {
                normal = n;
                angle = 360-angle;
            }
            
        }
        /*
        public CircleSegment(Vector3 center, Vector3 start, Vector3 end)
        {
            this.center = center;
            @from = start-center;
            to = (end-center).normalized;
            radius = @from.magnitude;
            @from /= radius;
            //Quaternion q=Quaternion.FromToRotation(@from, to);
            //q.ToAngleAxis(out angle, out normal);

            normal = Vector3.Cross(@from, to);
            float sin = normal.magnitude;
            normal /= sin;
            float cos = Vector3.Dot(@from, to);
            angle = Mathf.Atan2(sin, cos) * Mathf.Rad2Deg;
        }
    */
        public CircleSegment(Vector3 center, Vector3 normal, Vector3 start, float angle)
        {
            this.center = center;
            this.normal = normal;
            @from = start-center;
            this.angle = angle;
            radius = from.magnitude;
            from /= radius;
            Quaternion q = Quaternion.AngleAxis(angle, normal);
            to = q * from;
        }
#if UNITY_EDITOR
        public void DrawGizmo()
        {
            const float extra = 1f;
            Handles.DrawWireArc(center, normal, from, angle, radius * extra);
            Vector3 start = center + @from*radius*extra;
            Handles.DotHandleCap(0, start, Quaternion.identity, 0.01f, Event.current.type );

            if(angle < 360)
            {
                Vector3 end = center + to*radius*extra;
                Handles.DotHandleCap(0, end, Quaternion.identity, 0.01f, Event.current.type );
            }
        }
#endif


    }

    public class SegmentList : List<CircleSegment>
    {
#if UNITY_EDITOR
        public void DrawGizmo()
        {
            int i=0;
            foreach (CircleSegment segment in this)
            {
                ++i;
                Handles.Label(segment.center+segment.@from*segment.radius, i.ToString());
                segment.DrawGizmo();
            }
        }
#endif
    }

   
    public Vector3 camPosition
    {
        get { return camTransform.position;}
    }

    public FrustumSphereIntersection(Camera cam, Vector3 center, float radius)
    {
        main = cam;
        camTransform = cam.transform;
        allSegments = new SegmentList();
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
            plane[i] = p;
            planeInter[i] = new PlaneSphereIntersection(p, center, radius);
            cornersInter[i] = new RaySphereIntersection(p0, p1-p0, center, radius);
            moved[i] = MoveToPlaneIfOnNegativeSide(ref plane[i], ref seed);
        }

        seedInter = new RaySphereIntersection(camPosition, seed-camPosition, center, radius);
        ComputeAllSegments();
    }

    public class SegmentBuilder
    {
        private readonly FrustumSphereIntersection owner;
        public int planeIndex;
        public Vector3 start;
        public Vector3 end;

        public SegmentBuilder(int i, Vector3 start, Vector3 end)
        {
            planeIndex = i;
            this.start = start;
            this.end = end;
        }

        public void BuildSegment(FrustumSphereIntersection owner)
        {
            PlaneSphereIntersection psI = planeIndex >=0 
                ? owner.planeInter[planeIndex] 
                : owner.backPlaneInter;
            owner.allSegments.Add(new CircleSegment(psI.onPlane, psI.normal, start, end));
        }

    }

    private int GetFirstIntersectingPlane(int index)
    {
        for(int i = 0; i < 4;++i)
        {
            index = (index+1)%4;
            if (planeInter[index].type != PlaneSphereIntersection.eType.Circle)
                continue;
            return index;
        }
        return -1;
    }

    private void ComputeAllSegments()
    {
        allSegments.Clear();
        for(int i = 0; i < 4;++i)
        {
            circleInter[i] = GetInterWithNextCircle(i);
        }

        if(seedInter.type == RaySphereIntersection.eType.None)
            return;

        
        int firstIndex = GetFirstIntersectingPlane(-1);
        if(firstIndex == -1)
        {
        // sphere is fully visible, the backCircle is the only circle segment
        // just take the point closest to any side plane as start
            Vector3 start =  backPlaneInter.ProjectOnCircle(plane[0].ProjectedPoint(center));
            allSegments.Add(new CircleSegment(backPlaneInter.onPlane, backPlane.normal, start, 360));
            return;
        }
        var lsb = new List<SegmentBuilder>();
        for(int i = 0; i < 4; ++i)
        {
            if (planeInter[i].type != PlaneSphereIntersection.eType.Circle)
                continue;
            Vector3 s,e;
            GetPointsForCircleIntersection(i, out s, out e);

            if(IsVisible(e) || IsVisible(s))
                lsb.Add(new SegmentBuilder(i, s, e));
        }

        for (var index = 0; index < lsb.Count; index++)
        {
            SegmentBuilder builder = lsb[index];
            builder.BuildSegment(this);
            if(!circleInter[builder.planeIndex].HasValue)
            {
                // that segment doesn't end on a side circle,
                // we have to make the junction with the next one 
                SegmentBuilder nextBuilder = lsb[(index+1)%lsb.Count];
                SegmentBuilder junction = new SegmentBuilder(-1, builder.end, nextBuilder.start);
                junction.BuildSegment(this);
            }
        }
    }

    private bool IsVisible(Vector3 p0)
    {
        foreach (Plane p in plane)
        {
            if(p.Distance(p0) < -0.0001f)
                return false;
        }
        return true;
    }

    private bool GetPointsForCircleIntersection(int index, out Vector3 start, out Vector3 end)
    {
        PlanePlaneIntersection ppI = new PlanePlaneIntersection(plane[index], backPlane);
        LineSphereIntersection lsI = new LineSphereIntersection(ppI.O, ppI.D, center, radius);
        if(lsI.type == LineSphereIntersection.eType.None)
        {
            Debug.LogError("This should not be possible");
            start = end = Vector3.zero;
            return false;
        }

        if(lsI.type == LineSphereIntersection.eType.OnePoint)
        {
            // just grazing the surface, don't add a segment for that
            start = end = Vector3.zero;
            return false;
        }

        int previousIndex = (index + 3) % 4;
        if(plane[previousIndex].Distance(lsI.I0) < plane[previousIndex].Distance(lsI.I1))
        {
            start = circleInter[previousIndex].HasValue ? circleInter[previousIndex].Value : lsI.I0;
            end = circleInter[index].HasValue ? circleInter[index].Value : lsI.I1;
        }
        else
        {
            start = circleInter[previousIndex].HasValue ? circleInter[previousIndex].Value : lsI.I1;
            end = circleInter[index].HasValue ? circleInter[index].Value : lsI.I0;
        }
        return true;
    }

    private Vector3? GetInterWithNextCircle(int i)
    {

        if(planeInter[i].type != PlaneSphereIntersection.eType.Circle)
            return null;

        int nextIndex = (i+1)%4;
        if(planeInter[nextIndex].type != PlaneSphereIntersection.eType.Circle)
            return null;

        PlanePlaneIntersection ppI = new PlanePlaneIntersection(plane[i], plane[nextIndex]);
        LineSphereIntersection lsI = new LineSphereIntersection(ppI.O, ppI.D, center, radius);
        if(lsI.type == LineSphereIntersection.eType.None)
            return null;

        if(lsI.type == LineSphereIntersection.eType.OnePoint)
            return lsI.I0;
        float d0 = (lsI.I0 - camPosition).sqrMagnitude;
        float d1 = (lsI.I1 - camPosition).sqrMagnitude;
        return d0 > d1 ? lsI.I1 : lsI.I0;
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
        //Handles.zTest = CompareFunction.LessEqual;
        backPlaneInter.DrawGizmo(0.01f);
        float gizmoSize = 0.006125f;
        for (int i = 0; i < 4; ++i)
        {
            Handles.color = colors[i];
            //sidePlanes[i].DrawGizmo(gizmoSize);
            Handles.DrawLine(corners[i], camPosition);
            //planeInter[i].DrawGizmo(gizmoSize);
        }


        Handles.color = Color.white;
        allSegments.DrawGizmo();
    }
#endif
}
