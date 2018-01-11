
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
        PlaneSphereIntersection psi;
        Transform camPos;
        public FrustumSphereMeshBuilder(Vector3 center, float radius, Camera cam, int triangleRatio)
        {
            this.center = center;
            this.radius = radius;
            triangles = new List<TriangleData>();
            camPos = cam.transform;

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
                psi = new PlaneSphereIntersection(plane[4], center, radius);
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

            Vector3 axis = plane[4].normal;
            Quaternion q = Quaternion.AngleAxis(120, axis);
            Vector3 direction = Vector3.Cross(axis, camTransform.up).normalized;
            
            direction *= radius/triangleRatio;

            p0 = direction;

            p1 = q * p0;
            p2 = q * p1;
            p0 += seed;
            p1 += seed;
            p2 += seed;
            first = new TriangleData(new List<VerticeData>(), p0, p1, p2, "0", psi);
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


        List<TriangleData> toDo;
        public void Generate()
        {
            toDo = new List<TriangleData>{first};
            triangles.Clear();
            int count=0;
            while (OneStep(toDo))
            {
                if (++count <= 50000) continue;
                Debug.Log("Wow");
                break;
            }
            foreach (VerticeData data in first.vertices)
            {
                data.ToSphere(center, radius, plane, psi, camPos);
            }
        }

        public IEnumerator GenerateCoroutine()
        {
            toDo = new List<TriangleData>{first};
            var clic = new WaitForClick();
            triangles.Clear();
            yield return clic;
            while (OneStep(toDo))
            {
                yield return clic;
            }
            foreach (VerticeData data in first.vertices)
            {
                data.ToSphere(center, radius, plane, psi, camPos);
            }
        }

        private bool OneStep(List<TriangleData> toDo)
        {
            int index = toDo.Count - 1;
            TriangleData triangle = toDo[index];
            toDo.RemoveAt(index);
            triangles.Add(triangle);

            TriangleData t0 = triangle.CreateNeighbour(0, plane, triangles.Count + ".0"); 
            TriangleData t1 = triangle.CreateNeighbour(1, plane, triangles.Count + ".1");
            TriangleData t2 = triangle.CreateNeighbour(2, plane, triangles.Count + ".2");
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
            public Vector3 pos;
            public readonly List<TriangleData> triangles;
            public bool isVisible;
            public VerticeData(Vector3 pos, TriangleData triangle)
            {
                this.pos = pos;
                triangles = new List<TriangleData>(){triangle};
            }

            public void ToSphere(Vector3 center, float radius, Plane[] plane, PlaneSphereIntersection psi, Transform camPos)
            {
                if(isVisible)
                {
                    // just project visible vertices on the sphere

                    ProjectOnSphere(center, radius, plane[4].normal);
                    return;
                }
                ProjectOnCircle(psi,plane);
                //int projCount = 0;
                //for(int i = 4; --i>=0;)
                //{
                //    if(plane[i].Distance(pos) < 0)
                //    {
                //        pos = plane[i].ProjectedPoint(pos);
                //    }
                //}

                ProjectOnSphere(center, radius, camPos.position - pos);

            }

            private void ProjectOnSphere(Vector3 center, float radius, Vector3 normal)
            {
                RaySphereIntersection rsi =
                    new RaySphereIntersection(pos + 2 * radius * normal, -normal, center, radius);
                if (rsi.type == RaySphereIntersection.eType.None)
                {
                    Debug.LogError("Something went wrong");
                    return;
                }
                pos = rsi.I;
            }

            private bool ProjectOnCircle(PlaneSphereIntersection psi, Plane[] plane)
            {
                pos = plane[4].ProjectedPoint(pos);
                Vector3 v = pos - psi.onPlane;
                float d2 = v.sqrMagnitude;
                if (d2 > psi.circleRadius * psi.circleRadius)
                {
                    v /= Mathf.Sqrt(d2);
                    v *= psi.circleRadius;
                    pos = psi.onPlane + v;
                    return true;
                }
                return false;
            }


          //  public static bool operator==(VerticeData data, Vector3 p)
          //  {
          //      return Vector3.Distance(data.pos, p) < 0.01f;
          //  }
          //  public static bool operator!=(VerticeData data, Vector3 p)
          //  {
          //      return !(data == p);
          //  }
        }

        public class TriangleData
        {
            public readonly string name;
            public List<VerticeData> vertices;
            private readonly PlaneSphereIntersection psi;
            public readonly Vector3 com;
            
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
                else
                {
                    vertices[p[i]].triangles.Add(this);
                }
            }

            public int GetVectorIndex(Vector3 value)
            {
                for(int index = 0; index<vertices.Count;++index)
                {
                    //if(vertices[index].pos == value)
                    Vector3 delta = vertices[index].pos - value;
                    if(delta.sqrMagnitude < 0.0001f)
                    {
                        return index;
                    }
                }                                
                return vertices.Count;
            }

            public TriangleData(TriangleData parent, int i0, int i1, Vector3 p2, string name)
            {
                this.name = name;
                psi = parent.psi;
                vertices = parent.vertices;
                p[0] = i0;
                p[1] = i1;
                vertices[i0].triangles.Add(this);
                vertices[i1].triangles.Add(this);
                SetVertice(2, p2);
                com = Vector3.zero;
                for(int i =0; i < 3; ++i)
                {
                    com += this[i].pos;
                    neighbour[i] = CheckNeighbour(p[i], p.Modulo(i+1));
                }
                com /= 3;

                for(int i = 0; i < 3; ++i)
                {
                    display[i] = Vector3.Lerp(com, this[i].pos, 0.9f);
                }
            }

            private TriangleData CheckNeighbour(int i0, int i1)
            {
                // a neighbour shares the same 2 indices in his list of indices
                foreach (var triangle in vertices[i0].triangles)
                {
                    if(triangle == this)
                        continue;
                    if (!vertices[i1].triangles.Contains(triangle))
                        continue;
                    //this triangle has i0 and i1 as vertices, it's the neighbour
                    triangle.SetAsNeighbour(i0, i1, this);
                    return triangle;
                }
                return null;
            }

            private void SetAsNeighbour(int i0, int i1, TriangleData n)
            {
                for(int i = 0; i <3;++i)
                {
                    if(neighbour[i]==null && (p[i] == i0 || p[i]==i1))
                    {
                        neighbour[i] = n;
                        return;
                    }
                }
                Debug.Log("Odd " + name + " doesn't have common vertice with " + n);
            }

            public TriangleData(List<VerticeData> vertices, Vector3 p0, Vector3 p1, Vector3 p2, string name, PlaneSphereIntersection psi)
            {
                this.vertices = vertices;
                this.name = name;
                this.psi = psi;
                SetVertice(0, p0);
                SetVertice(1, p1);
                SetVertice(2, p2);

                com = (p0+p1+p2)/3;
                for(int i = 0; i < 3; ++i)
                {
                    neighbour[i] = null;
                    display[i] = Vector3.Lerp(com, this[i].pos, 0.9f);
                }
            }



            public readonly int[] p = new int[3];
            private Dictionary<int, List<TriangleData>> verticeTriangles;
            private Vector3[] display = new Vector3[3];

            public TriangleData CreateNeighbour(int i, 
                Plane[] planes, 
                string name)
            {
                if(neighbour[i] != null)
                {
                    return null;
                }

                VerticeData v0 = this[i];
                VerticeData v1 = this[i+1];
                VerticeData v2 = this[i+2];
                
                Vector3 p2 = v0.pos  + v1.pos - v2.pos;

                bool newPointIsVisible = IsVisible(p2, planes);
                if(!(newPointIsVisible || v0.isVisible || v1.isVisible))
                    return null;
                
                var n = new TriangleData(this, p[i], p[(i+1)%3], p2, name);
                n[2].isVisible = newPointIsVisible;
                return n;
            }

            public bool IsVisible(Vector3 p, Plane[] planes)
            {
                Vector3 d = p-psi.onPlane;
                if(d.sqrMagnitude > psi.circleRadius*psi.circleRadius)
                    return false;
                for(int i = planes.Length-1;--i>=0;)
                {
                    if(planes[i].Distance(p)<0)
                        return false;
                }
                return true;
            }

            public void OnDrawGizmos(int index)
            {
#if UNITY_EDITOR
//                Vector3 middle = Vector3.zero;
                for(int i = 0; i < 3;++i)
                {
                    if(index>=0)
                    {
                        //string text = i.ToString();
                        if(neighbour[i]!=null)
                        {
                           // Handles.color = Color.cyan;
                           // Vector3 n = 0.5f*(com+neighbour[i].com);
                           // Handles.DrawDottedLine(com, n, 2);
                           // Handles.Label(0.5f*(n+com), text);
                            Handles.color = Color.yellow;
                        }
                        else
                        {
                            Handles.color = Color.green;
                        }
                        //Handles.Label(display[i], text);
                        //Handles.DrawLine(display[i], display[(i+1)%3]);
                        //Handles.Label(com, index.ToString());
                        Handles.DrawLine(this[i].pos, this[i+1].pos);
                    }
                  //  else
                  //  {
                  //      Handles.color = neighbour[i] == null ? Color.magenta: Color.cyan;
                  //      Handles.DrawDottedLine(display[i], display[(i+1)%3], 1);
                  //      Handles.Label(com, name);
                  //  }

                    //Handles.Label((display[i] + com)*0.5f, p[i].ToString());
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

            for (var index = 0; index < triangles.Count; index++)
            {
                triangles[index].OnDrawGizmos(index);
            }
            
            return;
#pragma warning disable 162
            foreach (var data in toDo)
            {
                data.OnDrawGizmos(-1);
            }

            Handles.color = Color.cyan;
            for (var index = 0; index < first.vertices.Count; index++)
            {
                var vd = first.vertices[index];
                foreach (var triangleData in vd.triangles)
                {
                    Handles.DrawLine(vd.pos, triangleData.com);
                }

                //if (vd.isVisible)
                //    Handles.Label(vd.pos, index.ToString());
            }
#pragma warning restore 162
#endif
        }

    }
}