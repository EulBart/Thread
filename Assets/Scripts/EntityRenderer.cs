using System.Collections;
using UnityEngine;


public class EntityRenderer : MonoBehaviour
{
    public Mesh instanceMesh;
    public Material instanceMaterial;

    private ComputeBuffer positionBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private Bounds bounds;

    void Start()
    {
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        bounds.center = Vector3.zero;
        bounds.extents = 10000*Vector3.one;
    }

    WaitForEndOfFrame endOfFrame;

    IEnumerator Render()
    {

        for(;;)
        { 
            
            Graphics.DrawMeshInstancedIndirect(instanceMesh, 
                0, instanceMaterial, 
                bounds, argsBuffer);
            yield return null;
        }
    }

    public void SetBuffer(EntitiesManager manager) {

        // positions
        if (positionBuffer != null)
            positionBuffer.Release();

        int instanceCount = manager.Positions.Length;


        positionBuffer = new ComputeBuffer(instanceCount, 16);
        positionBuffer.SetData(manager.Positions);
        instanceMaterial.SetBuffer("positionBuffer", positionBuffer);

        // indirect args
        uint numIndices = (instanceMesh != null) ? (uint)instanceMesh.GetIndexCount(0) : 0;
        args[0] = numIndices;
        args[1] = (uint)instanceCount;
        argsBuffer.SetData(args);

        StopAllCoroutines();
        StartCoroutine(Render());
    }

    void OnDisable() {

        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }
}