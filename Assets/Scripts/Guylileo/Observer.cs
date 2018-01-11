
using System.Collections;
using Guylileo;
using OSG.Debug;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class Observer : MonoBehaviour
{
    [SerializeField] float longitude;
    [SerializeField] float latitude;
    [SerializeField] float altitude;
    [SerializeField] float cameraSpeed = 0.25f;
    [SerializeField] float bearing = 0;
    [SerializeField] float elevation;
    [SerializeField] float yaw;
    [SerializeField] float roll;
    [Header("UI")]
    [SerializeField] Image compass;
    [SerializeField] TextMeshProUGUI bearingText;
    [SerializeField] TextMeshProUGUI positionText;
    [SerializeField] Slider latitudeSlider;
    [SerializeField] Slider longitudeSlider;
    [Header("MeshControl")]
    [SerializeField] SphereMeshBuilder mesh;

    public bool showHandles;

    Vector3 localLookAt;
    Vector3 lookAtPosition;
    bool fromSlider;

    Vector3 downPosition;
    float downBearing;
    float downElevation;
    Camera _main;

    public Camera main
    {
        get
        {
            if(!_main)
                _main = GetComponent<Camera>();
            return _main;
        }
    }


    void OnEnable()
    {
        SetPos();
    }

    void OnValidate()
    {
        parent = transform.parent;

        if(altitude<0) altitude = 0;
        
        while(longitude < -180)
            longitude += 360;
        while(longitude > 180)
            longitude -= 360;
        while (bearing<0)
        {
            bearing += 360;
        }
        while (bearing>360)
        {
            bearing -= 360;
        }
        latitude = Mathf.Clamp(latitude,-89.9f, 89.9f);
        elevation = Mathf.Clamp(elevation, -180, 180);

        if(!fromSlider)
        {
            if(latitudeSlider)
                latitudeSlider.value = latitude;
            if(longitudeSlider)
                longitudeSlider.value = longitude;
        }

        SetPos();

    }

	void Update ()
	{
	    if(Input.GetMouseButtonDown(1))
	    {
	        downPosition = Input.mousePosition;
            downBearing = bearing;
            downElevation = elevation;
            oldFlags = main.clearFlags;
            if(oldFlags == CameraClearFlags.Nothing)
                main.clearFlags = CameraClearFlags.SolidColor;
	    }

        if(Input.GetMouseButton(1))
        {
            var deltaMouse = cameraSpeed*(Input.mousePosition - downPosition);
            bearing = downBearing + deltaMouse.x;
            elevation = downElevation + deltaMouse.y;
            OnValidate();
        }

        if(Input.GetMouseButtonUp(1))
        {
            main.clearFlags = oldFlags;
        }

        //Camera.main.fieldOfView = 180 * Input.GetAxis("wheel");

	}

    Color[] colors =
    {
        Color.red, Color.green, Color.blue, Color.yellow
    };


    public bool showBackPlane;
    public bool[] showPlane = new bool[4];

    public FrustumSphereMeshBuilder builder;

#if UNITY_EDITOR
    private FrustumSphereIntersection fsi;
    private void OnDrawGizmos()
    {
        Vector3 parentPosition = transform.parent.position;
        if(fsi==null)
        {
            fsi = new FrustumSphereIntersection(main, parentPosition, mesh.radius);
        }
        else
        {
            fsi.SetSphere(parentPosition, mesh.radius);
        }
        fsi.DrawGizmos();

        if(builder!=null)
        {
            builder.DrawGizmos();
        }
    }
#endif

    private void DrawArrow(Vector3 localDirection, Color color)
    {
        Vector3 worldDirection = parent.TransformDirection(localDirection);
        DebugUtils.DrawArrow(transform.position, worldDirection, color);
    }
    
    public Vector3 up,north,east;
    Transform parent;
    private CameraClearFlags oldFlags;
    

    public Vector3 bearingDirection 
    {
        private set ;get;
    }

    public static Vector3 CoordinatesToNormal(float longitude, float latitude)
    {
        latitude *= Mathf.Deg2Rad;
        longitude *= Mathf.Deg2Rad;
        float cosl = Mathf.Cos(latitude);
        float sinl = Mathf.Sin(latitude);
        float cosL = Mathf.Cos(longitude);
        float sinL = Mathf.Sin(longitude);
        return new Vector3(cosl * cosL, sinl, cosl * sinL);
    }
    private const float r2d=Mathf.Rad2Deg;

    public static Vector2 NormalToCoordinates(Vector3 n)
    {
        float latitude = Mathf.Atan2(n.y, Mathf.Sqrt(n.x*n.x+n.z*n.z)) * r2d;
        float longitude = Mathf.Atan2(n.z, n.x) * r2d;
        return new Vector2(longitude, latitude);
    }

    private void SetPos()
    {
        if(!showHandles)
        {
            return;
        }


        float mainNearClipPlane = main.farClipPlane/65536;
        main.nearClipPlane = mainNearClipPlane;
        parent = transform.parent;
        float d2r = Mathf.Deg2Rad;
        float l = d2r * latitude;
        float L = d2r * longitude;

        float cosl = Mathf.Cos(l);
        float sinl = Mathf.Sin(l);
        float cosL = Mathf.Cos(L);
        float sinL = Mathf.Sin(L);
        up = new Vector3(cosl * cosL, sinl, cosl * sinL);
        transform.localPosition = (mesh.radius + 2*mainNearClipPlane + altitude/1000f) * up;
        

        north = new Vector3(-cosL * sinl, cosl, -sinl * sinL);
        east = new Vector3(-sinL*cosl, 0,cosl*cosL);

        float bR = bearing * d2r;
        float cosB = Mathf.Cos(bR);
        float sinB = Mathf.Sin(bR);
        float eR = elevation * d2r;
        float cosE = Mathf.Cos(eR);
        float sinE = Mathf.Sin(eR);

        bearingDirection =  cosB * north + sinB * east;
        localLookAt = cosE * bearingDirection + sinE * up;
        lookAtPosition = transform.position + parent.TransformDirection(localLookAt);
        if(compass)
        {
            compass.transform.rotation = new Quaternion(){eulerAngles = new Vector3(0,0,bearing)};
        }
        if(bearingText)
        {
            bearingText.text = AngleToString(bearing);
        }
        if(positionText)
        {
            positionText.text = AngleToString(Mathf.Abs(longitude)) +  (longitude>=0 ? " E " : " W ") +
                                AngleToString(Mathf.Abs(latitude)) +  (latitude>=0 ? " N" : " S");
        }

        Quaternion yawRollQ = new Quaternion {eulerAngles = new Vector3(0, yaw, roll)};
        
        transform.LookAt(lookAtPosition, parent.TransformDirection(up));
        transform.rotation = transform.rotation * yawRollQ ;
        if(mesh)
            mesh.BuildMesh(this);
    }

    private string AngleToString(float a)
    {
        int aI = Mathf.FloorToInt(a);
        
        float m = (a-aI)*60;
        int mI = Mathf.FloorToInt(m);
        float s = (m-mI)*60;
        int sI = Mathf.FloorToInt(s);
        return aI+"°"+mI+"'" + sI + "\"";
    }

    public void SetLongitude(float f)
    {
        longitude = f;
        fromSlider = true;
        OnValidate();
    }

    public void SetCoordinates(Vector2 c)
    {
        latitude = c.y;
        longitude = c.x;
        fromSlider = false;
        OnValidate();
    }

    public void SetLatitude(float f)
    {
        latitude = f;
        fromSlider = true;
        OnValidate();
    }

    public void SetAltitude(float f)
    {
        altitude = f;
        fromSlider = true;
        OnValidate();
    }

    public Vector2 GetCoordinates()
    {
        return new Vector2(longitude, latitude);
    }

    public int triangleRatio = 10;
    public void BuildMesh()
    {
        builder = new FrustumSphereMeshBuilder(transform.parent.position, mesh.radius, main, triangleRatio);
#if UNITY_EDITOR
        if(!EditorApplication.isPlaying)
        {
            builder.Generate();
            return;
        }
#endif
        StopAllCoroutines();
        StartCoroutine(builder.GenerateCoroutine());
    }

 
}
