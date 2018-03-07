using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Initial;
using Microsoft.Msagl.Layout.Layered;
using OSG;
using UnityEditor;


public class GraphTestWindow : EditorWindow
{
    [MenuItem("Test/Graph")]
    public static void OpenWindow()
    {
        var editorWindow = EditorWindow.GetWindow<GraphTestWindow>();
        editorWindow.Show();
    }

    private GeometryGraph graph;


    private static EdgeRoutingMode mode = EdgeRoutingMode.Rectilinear;

    private Matrix4x4 graphToScreen;
    private Matrix4x4 screenToGraph;
    private Vector3 positionInGraph;
    private float zoom = 1;
    GUIStyle labelStyle;
    private Vector3 graphMousePos;
    private Vector3 downGraphPosition;
    private Vector2 downScreenPosition;
    private Vector3 deltaScreen;
    private Vector3 deltaGraph;
    private int windowId;
    private Rect windowPosition = new Rect(100, 100, 600, 400);
    private Vector2 reducedWindowSize = new Vector2(400,50);
    private Vector2 openedWindowSize = new Vector2(600,400);
    private SugiyamaLayoutSettings settings;

    #region GRAPH CREATION

    public static void AddNode(string id, GeometryGraph graph, double w, double h)
    {
        graph.Nodes.Add(new Node(CreateCurve(w, h), id));
    }

    public static ICurve CreateCurve(double w, double h)
    {
        //return CurveFactory.CreateRectangle(w, h, new Point());
        //return CurveFactory.CreateEllipse(w/2,h/2, new Point());
        return CurveFactory.CreateRectangleWithRoundedCorners(w, h, 10, 10, new Point());
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



    /// <summary>
    /// this FuckingShit of dictionnary doesn't find Singleton<T> as parent of AutoSingleton<T>
    /// so we have to use the name string to find out
    /// </summary>
    class FuckingShit
    {
        public readonly Type type;
        public readonly string name;
        public FuckingShit(Type type)
        {
            name = GetTypeName(type);
            this.type = type;
        }
        public override string ToString()
        {
            return name;
        }
    }
    internal void CreateClassGraph()
    {
        graph = new GeometryGraph();

        var typeNodes = new Dictionary<string, Node>();


        GUIContent content = new GUIContent();

        ProcessTypeDelegate processType = type =>
        {
            string typeNamespace = type.Namespace;
            //if (!string.IsNullOrEmpty(typeNamespace))
            //{
            //    if (typeNamespace.StartsWith("Unity")
            //    || typeNamespace.StartsWith("TMPro")
            //       || typeNamespace.StartsWith("TMPro")
            //    )
            //        return;
            //}

            char start = type.Name[0];
            if (start == '<' || start == '$')
                return;

            //if(!type.Name.Contains("Singleton"))
            //    return;

            FuckingShit shit = new FuckingShit(type);

            content.text = shit.name;
            Vector2 size = GUI.skin.label.CalcSize(content);
            double w = size.x * 2.5;
            double h = size.y + 20;
            var node = new Node(CurveFactory.CreateRectangle(w, h, new Point())) { UserData = shit };
            graph.Nodes.Add(node);

            Node fuckingPieceOfMotherFuckerCrap;
            if (typeNodes.TryGetValue(content.text, out fuckingPieceOfMotherFuckerCrap))
            {
                Debug.Log("fuckingPieceOfMotherFuckerCrap " + fuckingPieceOfMotherFuckerCrap);
            }
            else
            {
                typeNodes.Add(content.text, node);
            }
        };

        AssemblyScanner.Register(processType, AssemblyScanner.OnlyProject);
        AssemblyScanner.Scan();

        foreach (var kvp in typeNodes)
        {
            Node node = kvp.Value;
            Type baseType = (node.UserData as FuckingShit).type.BaseType;
            if (baseType == null)
                continue;

            string baseTypeName = GetTypeName(baseType);

            Node parentNode;
            if (typeNodes.TryGetValue(baseTypeName, out parentNode))
            {
                graph.Edges.Add(new Edge(node, parentNode));
            }
        }

        for(int i = graph.Nodes.Count;--i>=0;)
        {
            if(graph.Nodes[i].Edges.Any())
                continue;
            graph.Nodes.RemoveAt(i);
        }
    }



    private static string GetTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;
        var par = type.GetGenericArguments();
        int indexOf = type.Name.IndexOf('`');
        StringBuilder b = new StringBuilder();
        b.Clear();
        b.Append(indexOf >= 0 ? type.Name.Substring(0, indexOf) : type.Name);

        for (int i = 0; i < par.Length; ++i)
        {
            b.AppendFormat("{0}{1}", i == 0 ? "<" : ", ", GetTypeName(par[i]));
        }
        b.Append('>');
        return b.ToString();
    }

    private void RunLayout()
    {
        settings = settings??new SugiyamaLayoutSettings
        {
            Transformation = PlaneTransformation.Rotation(Math.PI / 2),
            PackingMethod = PackingMethod.Columns,
            EdgeRoutingSettings =
            {
                Padding = 100,
                
            },
            GroupSplit = 10,
            
        };
        
        settings.EdgeRoutingSettings.EdgeRoutingMode = mode;
        var layout = new LayeredLayout(graph, settings);
        layout.Run();
    }


    #endregion


    private void OnEnable()
    {
        windowId = GetInstanceID();
        graph = null;

    }
    private void OnGUI()
    {
        if (graph == null)
        {
            CreateClassGraph();
            RunLayout();
        }

        Event current = Event.current;
        if (current == null)
            return;
        if (!windowPosition.Contains(current.mousePosition))
        {
            ManageMouse(current);
            ManageScreenSize(current);
            windowPosition.size = reducedWindowSize;
        }
        else
        {
            windowPosition.size = openedWindowSize;
        }
        DrawGraph();
        Handles.matrix = Matrix4x4.identity;
        SettingsUI();
    }

    private void SetMatrices()
    {
        screenToGraph.SetTRS(positionInGraph, Quaternion.identity, new Vector3(zoom, -zoom, zoom));
        graphToScreen = screenToGraph.inverse;
    }

    private void ManageScreenSize(Event evt)
    {

    }
    private void ManageMouse(Event evt)
    {
        graphMousePos = screenToGraph.MultiplyPoint3x4(evt.mousePosition);
        if (evt.isScrollWheel)
        {
            if (evt.delta.y > 0)
            {
                zoom *= 1.1f;
            }
            else
            {
                zoom /= 1.1f;
            }

            zoom = Mathf.Clamp(zoom, 0.2f, 50f);

            SetMatrices();
            positionInGraph += graphMousePos - screenToGraph.MultiplyPoint3x4(evt.mousePosition);
            SetMatrices();
            Repaint();
            return;
        }

        if (evt.button != 0)
            return;
        if (evt.type == EventType.MouseDown)
        {
            downGraphPosition = positionInGraph;
            downScreenPosition = evt.mousePosition;
        }
        else if (evt.type == EventType.MouseDrag)
        {
            deltaScreen = evt.mousePosition - downScreenPosition;
            deltaGraph = screenToGraph.MultiplyVector(deltaScreen);
            positionInGraph = downGraphPosition - deltaGraph;
            Repaint();
        }

    }
    private PropertyInfo[] infos;
    private Vector2 scrollPos;
    

    private void SettingsUI()
    {
        BeginWindows();
        windowPosition = GUI.Window(windowId, windowPosition, id =>
        {
            if (id != windowId)
                return;
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(windowPosition.width-8));

            GUIContent guiContent = new GUIContent(mode.ToString());
            GUILayout.BeginHorizontal();
            if (EditorGUILayout.DropdownButton(guiContent, FocusType.Passive))
            {
                Rect r = GUILayoutUtility.GetLastRect();
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
            if (GUILayout.Button("CENTER"))
            {
                ResetPosition();
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow();

            infos = infos??GetInfos(typeof(SugiyamaLayoutSettings));

            foreach (MemberInfo info in infos)
            {
                string name  = info.Name;
                object value = info.GetValue(settings);
                if(value != null)
                {
                    name += " " + value;
                }

                IEnumerable<Attribute> att = info.GetCustomAttributes(typeof(DescriptionAttribute));
                if(att.Any())
                {
                    DescriptionAttribute desc = att.First() as DescriptionAttribute;
                    if(desc!=null)
                    {
                        name += " (" + desc.Description + ")";
                    }
                }
                GUILayout.Label(name);
            }
            GUILayout.EndScrollView();
            

        }, "Settings");
        EndWindows();
    }

    private PropertyInfo[] GetInfos(Type type)
    {
        return type.GetProperties(BindingFlags.Public|BindingFlags.Instance);
    }

    private void ResetPosition()
    {
        Vector3 screenCenter = new Vector3(Screen.width/2, Screen.height/2);

        Vector3 screenCenterInGraph = screenToGraph.MultiplyPoint3x4(screenCenter);
        Vector3 graphCenter = graph.BoundingBox.Center.V3();
        positionInGraph += graphCenter - screenCenterInGraph;
        SetMatrices();
     //   zoom = 1;
     //   SetMatrices();
    }

    private void DrawGraph()
    {
        if (Event.current.type != EventType.Repaint)
            return;
        SetMatrices();
        Handles.matrix = graphToScreen;
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
    private void DrawNode(Node node)
    {
        DrawCurve(node.BoundaryCurve);
        var bb = node.BoundingBox;

        float bbHeight = (float)bb.Height;
        Vector3 h = graphToScreen.MultiplyVector(new Vector3(0, bbHeight, 0));
        int fontSize = (int)Mathf.Abs(h.y * 0.8f);
        labelStyle = labelStyle ?? new GUIStyle(GUI.skin.label);
        if(fontSize==0)
            return;

        if(fontSize>300)
            fontSize = 300;

        labelStyle.fontSize = fontSize;

        Vector3 left = 0.5f * (node.BoundingBox.LeftBottom.V3() + node.BoundingBox.LeftTop.V3());
        var pos = left + 0.5f * new Vector3(0, bbHeight);


        Handles.Label(pos, node.UserData.ToString(), labelStyle);

    }
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
        Vector3 delta = graphToScreen.MultiplyVector(new Vector3((float)curve.Length, 0, 0));
        int nPoints = 1 + (int)(Mathf.Abs(delta.x) / 10f);
        Vector3[] line = new Vector3[nPoints];
        for (int i = 0; i < nPoints; ++i)
        {
            double t = Mathf.Lerp((float)curve.ParStart, (float)curve.ParEnd, (float)i / (nPoints - 1));
            line[i] = curve[t].V3();
        }
        Handles.DrawPolyLine(line);
    }
    private void DrawBezier(CubicBezierSegment cubic)
    {
        DrawGenericCurve(cubic);
    }
    private void DrawLineSegment(LineSegment ls)
    {
        Handles.DrawLine(ls.Start.V3(), ls.End.V3());
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
