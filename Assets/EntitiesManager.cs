using System;
using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;
using UnityEditor;
using Debug = UnityEngine.Debug;


public class EntitiesManager : MonoBehaviour
{
    [SerializeField] private int maxCount = 10;
    [SerializeField] private int threadCount = 1;
    [SerializeField] private float maxSize = 10;
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
        StartCoroutine(WaitForEndOfJob(new RandomizeJob(maxSize, positions)));
    }

    private IEnumerator WaitForEndOfJob<T>(Job<T> job) where T : struct 
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();
        var result = job.Run(threadCount);
        yield return result;
        watch.Stop();
        Debug.Log(result.result.Length + " " + typeof(T).Name + " computed in " + watch.ElapsedMilliseconds + " ms.");

        EntityRenderer r = GetComponent<EntityRenderer>();
        if(r)
        {
            r.SetBuffer(this);
        }
    }


    public Bounds GetBounds()
    {
        return _bounds;    
    }
}
