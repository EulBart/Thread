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
    [Header("UI")]
    [SerializeField] Image compass;
    [SerializeField] TextMeshProUGUI bearingText;
    [SerializeField] TextMeshProUGUI positionText;
    [SerializeField] Slider latitudeSlider;
    [SerializeField] Slider longitudeSlider;
    [Header("MeshControl")]
    [SerializeField] PlanetMesh mesh;

    Vector3 localLookAt;
    Vector3 lookAtPosition;
    bool fromSlider;

    void OnEnable()
    {
        main = GetComponent<Camera>();
        SetPos();
        main.nearClipPlane = main.farClipPlane/65536;
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
        elevation = Mathf.Clamp(elevation, -90, 90);

        if(!fromSlider)
        {
            if(latitudeSlider)
                latitudeSlider.value = latitude;
            if(longitudeSlider)
                longitudeSlider.value = longitude;
        }
        SetPos();

    }

    Vector3 downPosition;
    float downBearing;
    float downElevation;
    Camera main;
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

    private void OnDrawGizmosSelected()
    {
        Handles.color = Color.white;
        Handles.SphereHandleCap(0, lookAtPosition, Quaternion.identity, .025f, Event.current.type);
        DrawArrow(up, Color.yellow);
        DrawArrow(north, Color.yellow);
        DrawArrow(east, Color.yellow);
        DrawArrow(localLookAt, Color.red);
    }

    private void DrawArrow(Vector3 localDirection, Color color)
    {
        Vector3 worldDirection = parent.TransformDirection(localDirection);
        Quaternion q = new Quaternion();
        q.SetLookRotation(worldDirection);
        Handles.color = color;
        Handles.ArrowHandleCap(0,transform.position, q, 0.1f, Event.current.type);
    }


    Vector3 up,north,east;
    Transform parent;
    private CameraClearFlags oldFlags;
    

    private void SetPos()
    {
        parent = transform.parent;
        float d2r = Mathf.Deg2Rad;
        float l = d2r * latitude;
        float L = d2r * longitude;

        float cosl = Mathf.Cos(l);
        float sinl = Mathf.Sin(l);
        float cosL = Mathf.Cos(L);
        float sinL = Mathf.Sin(L);
        up = new Vector3(cosl * cosL, sinl, cosl * sinL);
        
        transform.localPosition = (0.5f+altitude/1000f) * up;
        
        north = new Vector3(-cosL * sinl, cosl, -sinl * sinL);
        east = new Vector3(-sinL*cosl, 0,cosl*cosL);

        float bR = bearing * d2r;
        float cosB = Mathf.Cos(bR);
        float sinB = Mathf.Sin(bR);
        float eR = elevation * d2r;
        float cosE = Mathf.Cos(eR);
        float sinE = Mathf.Sin(eR);

        Vector3 bearingDirection =  cosB * north + sinB * east;
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
        transform.LookAt(lookAtPosition, parent.TransformDirection(up));
        if(mesh)
            mesh.BuildMesh(longitude, latitude);
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


}
