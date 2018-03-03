using System;
using UnityEngine;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.Layered;
using UnityEditor;


public class GraphTestWindow : EditorWindow
{
    [MenuItem("Test/Graph")]
    public static void OpenWindow()
    {
        var editorWindow = EditorWindow.GetWindow<GraphTestWindow>();
        editorWindow.Show();
    }
    private InitialLayout layout;
    private GeometryGraph graph;

    public static void AddNode(string id, GeometryGraph graph, double w, double h)
    {
        graph.Nodes.Add(new Node(CreateCurve(w, h), id));
    }

    public static ICurve CreateCurve(double w, double h)
    {
        return CurveFactory.CreateRectangle(w, h, new Point());
        //return CurveFactory.CreateEllipse(w/2,h/2, new Point());
    }

    internal void CreateGraph()
    {
        graph = new GeometryGraph();

        double width = 100;
        double height = 30;

        foreach (string id in "0 1 2 3 4 5 6 A B C D E F G a b c d e".Split(' '))
        {
            AddNode(id, graph, width, height);
        }

        graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("B")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("C")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("D")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("E")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("B"), graph.FindNodeByUserData("E")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("F")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("0"), graph.FindNodeByUserData("F")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("1"), graph.FindNodeByUserData("F")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("2"), graph.FindNodeByUserData("F")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("3"), graph.FindNodeByUserData("F")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("4"), graph.FindNodeByUserData("F")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("5"), graph.FindNodeByUserData("F")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("6"), graph.FindNodeByUserData("F")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("a"), graph.FindNodeByUserData("b")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("b"), graph.FindNodeByUserData("c")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("c"), graph.FindNodeByUserData("d")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("d"), graph.FindNodeByUserData("e")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("A"), graph.FindNodeByUserData("a")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("B"), graph.FindNodeByUserData("a")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("C"), graph.FindNodeByUserData("a")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("D"), graph.FindNodeByUserData("a")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("E"), graph.FindNodeByUserData("a")));
        graph.Edges.Add(new Edge(graph.FindNodeByUserData("F"), graph.FindNodeByUserData("a")));
        Edge edge = new Edge(graph.FindNodeByUserData("G"), graph.FindNodeByUserData("a"));
        graph.Edges.Add(edge);
    }

    private void RunLayout()
    {
        var settings = new SugiyamaLayoutSettings
        {
            Transformation = PlaneTransformation.Rotation(Math.PI / 2),
            EdgeRoutingSettings = {EdgeRoutingMode = mode},
        };
        var layout = new LayeredLayout(graph, settings);
        layout.Run();
    }

    private static EdgeRoutingMode mode = EdgeRoutingMode.SugiyamaSplines;

    private void OnEnable()
    {
        CreateGraph();
        RunLayout();
    }


    private void OnGUI()
    {
        SettingsUI();
        DrawGraph();
    }

    private void SettingsUI()
    {
        Rect r = new Rect(0, 0, 60, 20);
        if (EditorGUI.DropdownButton(r, new GUIContent(mode.ToString()), FocusType.Passive))
        {
            GenericMenu menu = new GenericMenu();
            foreach (EdgeRoutingMode value in Enum.GetValues(typeof(EdgeRoutingMode)))
            {
                menu.AddItem(new GUIContent(value.ToString()), value == mode, () =>
                {
                    mode = value;
                    RunLayout();
                });
            }
            menu.DropDown(r);
        }
    }

    Vector3 offset;
    private void DrawGraph()
    {
        if(Event.current.type != EventType.Repaint)
            return;

        Point top = graph.BoundingBox.LeftTop;
        offset = new Vector2(-(float)top.X, (float)top.Y);
        Handles.color = Color.black;
        foreach (Node node in graph.Nodes)
        {
            DrawNode(node);
        }
        
        foreach (Edge edge in graph.Edges)
        {
            DrawEdge(edge);
        }
    }

    private void DrawEdge(Edge edge)
    {
        DrawCurve(edge.Curve);
    }

    GUIStyle labelStyle;


    private void DrawNode(Node node)
    {
        DrawCurve(node.BoundaryCurve);
        var bb = node.BoundingBox;
        Rect r = new Rect(
            new Vector2(0, 0),
            new Vector2((float)bb.Width, (float)bb.Height));
        r.center = node.Center.V3() + offset;

        labelStyle = labelStyle ?? new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)(bb.Height * 0.5),
            alignment = TextAnchor.MiddleCenter
        };


        GUI.Label(r, node.UserData.ToString(), labelStyle);
    }
    const int nPoints = 64;
    Vector3[] line = new Vector3[nPoints];

    private void DrawCurve(ICurve curve)
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
                    DrawLineSegment(ls);
                    continue;
                }

                CubicBezierSegment cubic = segment as CubicBezierSegment;
                if (cubic != null)
                {
                    DrawBezier(cubic);
                    continue;
                }
                DrawGenericCurve(curve);
            }
        }
        else
        {
            var ls = curve as LineSegment;
            if (ls != null)
                DrawLineSegment(ls);
            else
            {
                DrawGenericCurve(curve);
            }
        }
    }

    private void DrawGenericCurve(ICurve curve)
    {
        for (int i = 0; i < nPoints; ++i)
        {
            double t = Mathf.Lerp((float)curve.ParStart, (float)curve.ParEnd, (float)i / (nPoints - 1));
            line[i] = curve[t].V3() + offset;
        }
        Handles.DrawPolyLine(line);
    }

    private void DrawBezier(CubicBezierSegment cubic)
    {
        DrawGenericCurve(cubic);
    }

    private void DrawLineSegment(LineSegment ls)
    {
        Handles.DrawLine(ls.Start.V3() + offset, ls.End.V3() + offset);
    }
}

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

}
