using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using TMPro;
using UnityEditor;
using UnityEngine;

internal static class GraphExtensions
{
    public static Vector3 V3(this Point point, float z = 0)
    {
        return new Vector3((float)point.X, (float)point.Y, z);
    }

    public static Vector2 V2(this Point point)
    {
        return new Vector2((float)point.X, (float)point.Y);
    }

    public abstract class NodeInfo
    {
        public static GUIStyle style;
        public static int baseFontSize = 20;
        public readonly Vector2 baseSize;
        public readonly GUIContent content;
        public Color color;
        protected NodeInfo(string name)
        {
            style = style??new GUIStyle(GUI.skin.label)
            {
                fontSize = baseFontSize
            };
            content = new GUIContent(name);
            baseSize = style.CalcSize(content);
        }
        public override string ToString()
        {
            return content.text;
        }
    }

    public static void Draw(this LineSegment ls)
    {
        Handles.DrawLine(ls.Start.V3(), ls.End.V3());
    }

    const int nPoints = 32;//1 + (int)(Mathf.Abs(delta.x) / 10f);
    static Vector3[] line = new Vector3[nPoints];

    public static void DrawGeneric(this ICurve curve)
    {
        //Vector3 delta = Handles.matrix.MultiplyVector(new Vector3((float)curve.Length, 0, 0));
        double delta = (curve.ParEnd-curve.ParStart)/(nPoints-1);
        double t = curve.ParStart;
        for(int i = 0; i < nPoints;++i)
        {
            line[i] = curve[t].V3();
            t += delta;
        }
        Handles.DrawPolyLine(line);        
    }

    public static void DrawBezier(this CubicBezierSegment curve)
    {
        curve.DrawGeneric();
    }

    public static void Draw(this ICurve curve)
    {
        if (curve == null)
        {
            return;
        }

        Curve c = curve as Curve;
        if (c != null)
        {
            foreach (ICurve segment in c.Segments)
            {
                LineSegment ls = segment as LineSegment;
                if (ls != null)
                {
                    ls.Draw();
                    continue;
                }

                CubicBezierSegment cubic = segment as CubicBezierSegment;
                if (cubic != null)
                {
                    cubic.Draw();
                    continue;
                }
                curve.DrawGeneric();
            }
        }
        else
        {
            var ls = curve as LineSegment;
            if (ls != null)
                ls.Draw();
            else
            {
                curve.DrawGeneric();
            }
        }
    } 


    public static void Draw(this Node node)
    {
        NodeInfo info = node.UserData as NodeInfo;
        if(info==null)
            return;

        Color color = Handles.color;
        Handles.color = info.color;
        node.BoundaryCurve.Draw();
        var bb = node.BoundingBox;

        var delta = Handles.matrix.MultiplyVector((bb.RightBottom - bb.LeftTop).V3());
        delta.x = Mathf.Abs(delta.x);
        delta.y = Mathf.Abs(delta.y);

        float ratio = delta.x / info.baseSize.x;
        int fontSize = (int)(NodeInfo.baseFontSize * ratio);

        if(fontSize>300)
            fontSize = 300;

        if(fontSize<2)
            return;

        NodeInfo.style.fontSize = fontSize;

        Vector3 left = 0.5f * (bb.LeftBottom.V3() + bb.LeftTop.V3());
        var pos = left + 0.5f * new Vector3(0, (float)bb.Height);
        Handles.Label(pos, node.UserData.ToString(), NodeInfo.style);
        Handles.color = color;
    }


    public static void Draw(this GeometryGraph graph)
    {
        foreach (Node node in graph.Nodes)
        {
            node.Draw();
        }

        

        foreach (Edge edge in graph.Edges)
        {
            edge.Curve.Draw();
            //if(edge.ArrowheadAtSource)
            //    Handles.DotHandleCap(0, edge.Curve.Start.V3(), Quaternion.identity, 5, Event.current.type);
            //if(edge.ArrowheadAtTarget)
                //Handles.DotHandleCap(0, edge.Curve.End.V3(), Quaternion.identity, 5, Event.current.type);

            Vector3 center = edge.Curve.End.V3();

            Handles.DrawLine(center+new Vector3(5,5,0), center-new Vector3(5,5,0));
            Handles.DrawLine(center+new Vector3(-5,5,0), center-new Vector3(-5,5,0));
        }        
    }




}