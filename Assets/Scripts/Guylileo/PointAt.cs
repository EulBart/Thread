using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class PointAt : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float size;
    void Update()
    {
        transform.LookAt(target.position);
    }

    void OnDrawGizmos()
    {
        Handles.ArrowHandleCap(0, transform.position, transform.rotation, size, Event.current.type);
    }

}
