
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class PointAt : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float size;
    void Update()
    {
        transform.LookAt(target.position);
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {

        Handles.ArrowHandleCap(0, transform.position, transform.rotation, size, Event.current.type);
    }
#endif

}
