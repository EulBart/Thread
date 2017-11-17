using UnityEngine;
using System.Collections;
using System.Diagnostics;
using TMPro;
using Debug = UnityEngine.Debug;


public class EntitiesManager : MonoBehaviour
{
    [SerializeField] private int maxCount = 10;
    [SerializeField] private int threadCount = 1;
    [SerializeField] private float maxSize = 10;
    [SerializeField] private TextMeshProUGUI text;
    private Container<Vector4> positions;
    private Container<Matrix4x4> matrices;
    private Bounds _bounds;

    public Vector4[] Positions
    {
        get
        {
            return positions.Array;
        }
    }

    public Matrix4x4[] Matrices
    {
        get
        {
            return matrices.Array;
        }
    }

    void OnEnable()
    {
        _bounds = new Bounds(Vector3.zero, maxSize * Vector3.one );
        positions = new Container<Vector4>(maxCount);
        positions.Add(maxCount);
        matrices = new Container<Matrix4x4>(maxCount);
        matrices.Add(maxCount);
        StartCoroutine(Run());
    }
    [Range(1,250)] public float wantedFPS = 30;
    private IEnumerator Run()
    {
        yield return WaitForEndOfJob(new RandomizeJob(maxSize, positions));
        EntityRenderer r = GetComponent<EntityRenderer>();
        float timer = 0;
        for(;;)
        {
            if(Input.GetMouseButton(1))
                yield return WaitForEndOfJob(new SphereJob(maxSize, 0.01f, positions));
            else
            {
                yield return null;
            }
            if(Input.GetMouseButton(0))
                r.SetBuffer(this);
        }
    }
 
    private IEnumerator WaitForEndOfJob<T>(Job<T> job, bool showTime=true) where T : struct 
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();
        var result = job.Run(threadCount);
        yield return result;
        watch.Stop();
        if(showTime)
        {
            if(text)
                text.SetText(watch.ElapsedMilliseconds.ToString("0.0"));
            else
            {
                Debug.Log(result.result.Length + " " + typeof(T).Name + " computed in " + watch.ElapsedMilliseconds + " ms.");    
            }
        }
    }


    public Bounds GetBounds()
    {
        return _bounds;    
    }
}
