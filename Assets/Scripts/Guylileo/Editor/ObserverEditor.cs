using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(Observer))]
public class ObserverEditor : Editor
{
    private Tool previousTool;
    void OnEnable()
    {
        previousTool = Tools.current;
    }

    void OnSceneGUI()
    {
        Tools.current = Tool.None;
        Observer o = target as Observer;

        var c = o.GetCoordinates();
        
        Transform t = o.transform.parent;
        Vector3 center = t.position;
        float radius = o.transform.localPosition.magnitude *1.1f;
        Vector3 normal = -t.up * radius;
        Vector3 from = t.right * radius;
        Handles.zTest = CompareFunction.Less;
        c.x = DrawAngleIndicator(center, normal, from, radius, c.x, Color.cyan);
        

        float longitude = Mathf.Deg2Rad * c.x;
        float cos = radius * Mathf.Cos(longitude);
        float sin = radius * Mathf.Sin(longitude);
        
        from = t.right * cos + t.forward * sin;
        normal = t.forward * cos - t.right *sin;
        c.y = DrawAngleIndicator(center, normal, from, radius, c.y, Color.yellow);
        o.SetCoordinates(c);
    }

    private static float DrawAngleIndicator(Vector3 center, 
        Vector3 normal, 
        Vector3 from, 
        float radius, float angle, Color color)
    {
        Handles.color = color;
        //Handles.DrawWireArc(center, normal, from, 360, radius);
        Quaternion q = Quaternion.AngleAxis(angle, normal);
        Quaternion nQ = Handles.Disc(q, center, normal, radius, false, 0);

        Vector3 axis;
        if(nQ != q)
        {
            nQ.ToAngleAxis(out angle, out axis);
            angle *= Mathf.Sign(Vector3.Dot(axis, normal));
        }
        var p = new Vector3[2]
        {
            center - normal,
            center + normal
        };
        Handles.DrawAAPolyLine(5, p);
        p[0] = center;
        p[1] = center+from;
        Handles.DrawAAPolyLine(5, p);

        color.a = 0.25f;
        Handles.color = color;
        Handles.DrawSolidArc(center, normal, from, angle, radius);
        return angle ;
    }

    void OnDisable()
    {
        Tools.current = previousTool;
    }

}
