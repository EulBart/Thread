
using System.Collections.Generic;
using OSG;
using UnityEngine;
using Plane = OSG.Plane;

namespace Guylileo
{
    public class FrustumSphereMeshBuilder
    {
        public readonly Vector3 center;
        public readonly float radius;
        public readonly Vector3 seed;

        private List<TriangleData> triangles;
        private List<TriangleData> toDo;

        private Vector3 OnSphere(Vector3 p)
        {
            return center + (p-center).normalized * radius;
        }

        
        Plane[] plane;

        public FrustumSphereMeshBuilder(Vector3 center, float radius, Camera cam)
        {
            this.center = center;
            this.radius = radius;
            triangles = new List<TriangleData>();
            toDo = new List<TriangleData>();

            TriangleData first = new TriangleData();
            Vector3 axis = seed - center;

            Quaternion q = Quaternion.AngleAxis(120, axis);

            Vector3[] corners = new Vector3[5];
            cam.CalculateFrustumCorners(cam.rect,
                cam.farClipPlane,
                Camera.MonoOrStereoscopicEye.Mono,
                corners);

            Transform camTransform = cam.transform;
            Vector3 camPosition = cam.transform.position;

            //centerInter = new RaySphereIntersection(camPosition, center-camPosition, center, radius);
            // compute world space coordinates for frustum
            for (var index = 0; index < 4; index++)
            {
                corners[index] = camTransform.TransformPoint(corners[index]);
            }
            corners[4] = corners[0];

            Vector3 p0 = camPosition;
            plane = new Plane[5];

            float distanceSqr = (p0 - center).sqrMagnitude;
            if (distanceSqr > radius * radius)
            {
                plane[4] = Plane.SphereBackPlaneSeenFromPosition(p0, center, radius);
                seed = center;
                for (int i = 0; i < 4; ++i)
                {
                    Vector3 p1 = corners[i];
                    Vector3 p2 = corners[i + 1];

                    Plane p = new Plane(p0, p2, p1);
                    p.SetName(Vector3.Lerp(p1, p2, 0.5f), "SidePl" + i);
                    plane[i] = p;
                    MoveToPlaneIfOnNegativeSide(ref plane[i], ref seed);
                }
            }
            else
            {
                return;
            }

            Vector3 direction = Vector3.Cross(seed - center, camTransform.up).normalized;
            float size = radius / 10;
            direction /= size;

            first.p[0] = OnSphere(seed + direction);
            first.p[1] = q * first.p[0];
            first.p[2] = q * first.p[1];

            toDo.Add(first);
            Generate();

        }

        private void Generate()
        {
            while (toDo.Count>0)
            {
                int index = toDo.Count-1;
                TriangleData triangle = toDo[index];
                toDo.RemoveAt(index);
                triangles.Add(triangle);
                TriangleData t0 = triangle.CreateNeighbour(0, plane, center, radius);
                TriangleData t1 = triangle.CreateNeighbour(1, plane, center, radius);
                TriangleData t2 = triangle.CreateNeighbour(2, plane, center, radius);
                if(t0!=null)
                {
                    toDo.Add(t0);
                }

                if(t1!=null)
                {
                    toDo.Add(t1);
                }

                if(t2!=null)
                {
                    toDo.Add(t2);
                }


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


        public class TriangleData
        {
            public TriangleData[] neighbour = new TriangleData[]
            {
                null,null,null
            };

            public Vector3 this[int i]
            {
                get
                {
                    return p.Modulo(i);
                }
                set
                {
                    p[i%3] = value;
                }
            }


            public readonly Vector3[] p = new Vector3[3];


            public TriangleData CreateNeighbour(int i, Plane[] planes, Vector3 center, float radius)
            {
                if(neighbour[i] != null)
                {
                    return null;
                }

                var n = new TriangleData();

                
                return n;
            }

            public bool IsVisible(Plane[] planes)
            {
                foreach (Plane pl in planes)
                {
                    if(pl.Distance(p[0])<0
                     &&pl.Distance(p[1])<0
                     &&pl.Distance(p[2])<0)
                        return false;
                }
                return true;
            }
        }
    }
}