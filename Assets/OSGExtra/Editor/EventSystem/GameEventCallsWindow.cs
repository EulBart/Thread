using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using UnityEditor;

public class GameEventCallsWindow : EditorWindow
{
    [MenuItem("Test/GameEventCallsWindow")]
    public static void OpenWindow()
    {
        var editorWindow = EditorWindow.GetWindow<GameEventCallsWindow>();
        editorWindow.Show();
    }

    private GeometryGraph graph;

    private static EdgeRoutingMode mode = EdgeRoutingMode.Rectilinear;

    private Matrix4x4 graphToScreen;
    private Matrix4x4 screenToGraph;
    private Vector3 positionInGraph;
    private float zoom = 1;
    private Vector3 graphMousePos;
    private Vector3 downGraphPosition;
    private Vector2 downScreenPosition;
    private Vector3 deltaScreen;
    private Vector3 deltaGraph;
    private int windowId;
    private Rect windowPosition = new Rect(100, 100, 600, 50);
    //private Vector2 reducedWindowSize = new Vector2(400,50);
    //private Vector2 openedWindowSize = new Vector2(600,400);
    private SugiyamaLayoutSettings settings;

    #region GRAPH CREATION

    private enum NodeType
    {
        UnusedEvent,
        UsedEvent,
        User
    }

    private class GameEventNodeInfo : GraphExtensions.NodeInfo
    {
        public NodeType type;

        public GameEventNodeInfo(string name) : base(name)
        {
        }
    }
    
    private Vector2 nodeRadii = new Vector2(10,10);

    private Dictionary<string, Node> eventNodes;
    private Dictionary<string, Node> userNodes;


    internal void CreateCallGraph()
    {
        graph = new GeometryGraph();
        eventNodes = new Dictionary<string, Node>();
        userNodes = new Dictionary<string, Node>();

        GameEventUsageDescription description = new GameEventUsageDescription();

        foreach (var unused in description.unusedEvents)
        {
            CreateEventNode(unused, NodeType.UnusedEvent, Color.red);
        }

        var usedEvents = description.invokers.users.Select(u => u.eventName)
                 .Concat(description.listeners.users.Select(u => u.eventName));

        foreach (var used in usedEvents.Distinct())
        {
            CreateEventNode(used, NodeType.UsedEvent, Color.blue);
        }

        var users = description.invokers.users.Select(u => u.usingType.Name)
            .Concat(description.listeners.users.Select(u => u.usingType.Name));

        foreach (var user in users.Distinct())
        {
            CreateUserNode(user);
        }
        foreach (var user in description.listeners.users)
        {
            
            graph.Edges.Add(new Edge(eventNodes[user.eventName], userNodes[user.usingType.Name]));
        }

        foreach (var user in description.invokers.users)
        {
            graph.Edges.Add(new Edge(userNodes[user.usingType.Name], eventNodes[user.eventName]));
        }
    }

    private Node CreateUserNode(string user)
    {
        Node node;
        if(userNodes.TryGetValue(user, out node))
        {
            Debug.LogError("Node " + user + " already exists");
            return node;
        }

        var info = new GameEventNodeInfo(user)
        {
            type = NodeType.User,
            color = Color.black
        };
        ICurve curve = CurveFactory.CreateRectangleWithRoundedCorners(info.baseSize.x, info.baseSize.y,nodeRadii.x, nodeRadii.y, new Point());
        //ICurve curve = CurveFactory.CreateDiamond(info.baseSize.x, info.baseSize.y, new Point());
//        ICurve curve = CurveFactory.CreateRectangle(info.baseSize.x, info.baseSize.y, new Point());
        node = new Node(curve, info);
        userNodes.Add(user, node);
        graph.Nodes.Add(node);
        return node;
    }

    private Node CreateEventNode(string eventName, NodeType type, Color color)
    {
        Node eventNode;
        if(eventNodes.TryGetValue(eventName, out eventNode))
        {
            Debug.LogError("Event node already exists " + eventName);
            return eventNode;
        }

        var info = new GameEventNodeInfo(eventName)
        {
            type = type,
            color = color
        };
        ICurve curve = CurveFactory.CreateRectangle(info.baseSize.x, info.baseSize.y, new Point());
        eventNode =  new Node(curve, info);
        eventNodes.Add(eventName, eventNode);
        graph.Nodes.Add(eventNode);
        return eventNode;
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
        //graph = null;
    }

    private void OnGUI()
    {
        if (graph == null)
        {
            CreateCallGraph();
            RunLayout();
            ResetPosition();
        }

        Event current = Event.current;
        if (current == null)
            return;
        if (!windowPosition.Contains(current.mousePosition))
        {
            ManageMouse(current);
            ManageScreenSize(current);
           // windowPosition.size = reducedWindowSize;
        }
        //else
        //{
        //    windowPosition.size = openedWindowSize;
        //}
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
           // scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(windowPosition.width-8));

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
/*
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
            }*/
          //  GUILayout.EndScrollView();
            

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
        zoom = 1;
        SetMatrices();
        Vector3 screenCenterInGraph = screenToGraph.MultiplyPoint3x4(screenCenter);
        Vector3 graphCenter = graph.BoundingBox.Center.V3();
        positionInGraph += graphCenter - screenCenterInGraph;
        SetMatrices();
    }

    private void DrawGraph()
    {
        if (Event.current.type != EventType.Repaint)
            return;
        SetMatrices();
        Handles.matrix = graphToScreen;
        Handles.color = Color.black;
        graph.Draw();
    }
}
