
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OSG;
using UnityEngine;
using Plane = OSG.Plane;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Guylileo
{
    public class FrustumSphereMeshBuilder
    {
        public readonly Vector3 center;
        public readonly float radius;
        public readonly Vector3 seed;

        private List<TriangleData> triangles;
        TriangleData first;

        private Vector3 OnSphere(Vector3 p)
        {
            return center + (p-center).normalized * radius;
        }

        Plane[] plane;

        public FrustumSphereMeshBuilder(Vector3 center, float radius, Camera cam, int triangleRatio)
        {
            this.center = center;
            this.radius = radius;
            triangles = new List<TriangleData>();
            

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
            Vector3 p1,p2;
            plane = new Plane[5];

            float distanceSqr = (p0 - center).sqrMagnitude;
            if (distanceSqr > radius * radius)
            {
                plane[4] = Plane.SphereBackPlaneSeenFromPosition(p0, center, radius);
                PlaneSphereIntersection psi = new PlaneSphereIntersection(plane[4], center, radius);
                seed = psi.ProjectOnCircle(camPosition);
                for (int i = 0; i < 4; ++i)
                {
                    p1 = corners[i];
                    p2 = corners[i + 1];

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

            Vector3 axis = (seed - center).normalized;
            Quaternion q = Quaternion.AngleAxis(120, axis);
            Vector3 direction = Vector3.Cross(axis, camTransform.up).normalized;
            
            direction *= radius/triangleRatio;

            p0 = OnSphere(seed + direction) - center;

            p1 = q * p0;
            p2 = q * p1;
            p0 += center;
            p1 = OnSphere(center + p1);
            p2 = OnSphere(center + p2);
            first = new TriangleData(new List<VerticeData>(), p0, p1, p2, "0");
            first[0].isVisible = first.IsVisible(first[0].pos, plane);
            first[1].isVisible = first.IsVisible(first[1].pos, plane);
            first[2].isVisible = first.IsVisible(first[2].pos, plane);
            
        }

        class WaitForClick : CustomYieldInstruction
        {
            private int count = int.MaxValue;
            public override bool keepWaiting
            {
                get
                {
                    if(Input.GetMouseButtonUp(0))
                    {
                        int frameCount = Time.frameCount;
                        bool wait = frameCount <= count;
                        count = frameCount;
                        return wait;
                    }

                    return true;
                }
            }
        }


        public void Generate()
        {
            var toDo = new List<TriangleData>{first};
            triangles.Clear();
            int count=0;
            while (OneStep(toDo))
            {
                if (++count <= 50000) continue;
                Debug.Log("Wow");
                break;
            }
        }

        public IEnumerator GenerateCoroutine()
        {
            var toDo = new List<TriangleData>{first};
            var clic = new WaitForClick();
            triangles.Clear();
            while (OneStep(toDo))
            {
                yield return clic;
            }
        }

        private bool OneStep(List<TriangleData> toDo)
        {
            if (triangles.Count >= 1000)
                return false;
            int index = toDo.Count - 1;
            TriangleData triangle = toDo[index];
            toDo.RemoveAt(index);
            triangles.Add(triangle);

            TriangleData t0 = triangle.CreateNeighbour(0, plane, center, radius, triangles.Count + ".0"); 
            TriangleData t1 = triangle.CreateNeighbour(1, plane, center, radius, triangles.Count + ".1");
            TriangleData t2 = triangle.CreateNeighbour(2, plane, center, radius, triangles.Count + ".2");
            if (t0 != null)
            {
                toDo.Add(t0);
            }

            if (t1 != null)
            {
                toDo.Add(t1);
            }

            if (t2 != null)
            {
                toDo.Add(t2);
            }
            return toDo.Count>0;
        }

        private bool MoveToPlaneIfOnNegativeSide(ref Plane sidePlane, ref Vector3 p0)
        {
            float distance = sidePlane.Distance(p0);
            if(distance>=0)
                return false;
            p0 -= sidePlane.normal * distance;
            return true;
        }

        public class VerticeData
        {
            public readonly Vector3 pos;
            public readonly List<TriangleData> triangles;
            public bool isVisible;
            public VerticeData(Vector3 pos, TriangleData triangle)
            {
                this.pos = pos;
                triangles = new List<TriangleData>(){triangle};
            }

            public static bool operator==(VerticeData data, Vector3 p)
            {
                return data.pos == p;
            }
            public static bool operator!=(VerticeData data, Vector3 p)
            {
                return data.pos != p;
            }
        }

        public class TriangleData
        {
            public readonly string name;
            private List<VerticeData> vertices;
            
            public override string ToString()
            {
                return name;
            }

            public readonly TriangleData[] neighbour = {
                null,null,null
            };

            public VerticeData this[int i]
            {
                get
                {
                    return vertices[p.Modulo(i)];
                }
            }

            public void SetVertice(int i, Vector3 pos)
            {
                p[i] = GetVectorIndex(pos);
                if(p[i] >= vertices.Count)
                {
                    vertices.Add(new VerticeData(pos, this));
                }
            }

            public int GetVectorIndex(Vector3 value)
            {
                for(int index = 0; index<vertices.Count;++index)
                {
                    if(vertices[index].pos == value)
                    {
                        return index;
                    }
                }                                
                return vertices.Count;
            }

            public TriangleData(TriangleData parent, int i0, int i1, Vector3 p2, string name)
            {
                this.name = name;
                vertices = parent.vertices;
                p[0] = i0;
                p[1] = i1;
                SetVertice(2, p2);
                for(int i =0; i < 3; ++i)
                {
                    neighbour[i] = CheckNeighbour(p[i], p.Modulo(i+1));
                }
            }

            private TriangleData CheckNeighbour(int i0, int i1)
            {
                // a neighbour shares the same 2 indices in his list of indices
                foreach (var triangle in vertices[i0].triangles)
                {
                    if(triangle == this)
                        continue;
                    if(vertices[i1].triangles.Contains(triangle))
                        triangle.SetAsNeighbour(i0, i1, this);
                }
                return null;
            }

            private void SetAsNeighbour(int i0, int i1, TriangleData n)
            {
                for(int i = 0; i <3;++i)
                {
                    if(p[i] == i0 || p[i]==i1)
                    {
                        neighbour[i] = n;
                        return;
                    }
                }
                Debug.Log("Odd " + name + " doesn't have common vertice with " + n);
            }

            public TriangleData(List<VerticeData> vertices, Vector3 p0, Vector3 p1, Vector3 p2, string name)
            {
                this.vertices = vertices;
                this.name = name;
                SetVertice(0, p0);
                SetVertice(1, p1);
                SetVertice(2, p2);
            }

            public readonly int[] p = new int[3];
            private Dictionary<int, List<TriangleData>> verticeTriangles;

            public TriangleData CreateNeighbour(int i, 
                Plane[] planes, 
                Vector3 center, 
                float radius, 
                string name)
            {
                if(neighbour[i] != null)
                {
                    return null;
                }

                VerticeData v0 = this[i];
                VerticeData v1 = this[i+1];
                VerticeData v2 = this[i+2];

                Plane pl = new Plane(v0.pos, v1.pos, center);
                Vector3 p2 = pl.SymetricPoint(v2.pos);
                p2 = center + (p2 - center).normalized * radius;

                bool newPointIsVisible = IsVisible(p2, planes);
                if(!(newPointIsVisible || v0.isVisible || v1.isVisible))
                    return null;
                
                var n = new TriangleData(this, p[i], p[(i+1)%3], p2, name);
                n[2].isVisible = newPointIsVisible;
                return n;
            }

            public bool IsVisible(Vector3 p, Plane[] planes)
            {
                for(int i = planes.Length;--i>=0;)
                {
                    if(planes[i].Distance(p)<0)
                        return false;
                }
                return true;
            }

            public bool IsVisible(Plane[] planes)
            {
                foreach (Plane pl in planes)
                {
                    if(pl.Distance(this[0].pos)<0
                     &&pl.Distance(this[1].pos)<0
                     &&pl.Distance(this[2].pos)<0)
                        return false;
                }
                return true;
            }

            public void OnDrawGizmos(Vector3 center, float radius)
            {
#if UNITY_EDITOR
//                Vector3 middle = Vector3.zero;
                for(int i = 0; i < 3;++i)
                {
                    Handles.DrawLine(this[i].pos, this[i+1].pos);
  //                  middle += this[i].pos;
                }
    /*            middle/=3;
                for (int i = 0; i < 3;++i)
                {
                    Vector3 p = this[i].pos;
                    float distance = Mathf.Abs(Vector3.Distance(p,center) - radius);
                    Handles.Label(Vector3.Lerp(p, middle, 0.1f),i.ToString()+ " " + distance.ToString());
                }
                
                Handles.Label(middle, name);*/
#endif
            }
        }
        public void DrawGizmos()
        {
#if UNITY_EDITOR
            if (triangles == null) return;

            Handles.Label(seed, "Seed");
            Handles.color = Color.cyan;
            foreach (TriangleData t in triangles)
            {
                t.OnDrawGizmos(center, radius);
            }
#endif
        }

    }
}