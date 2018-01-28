using UnityEngine;

namespace Fractal
{
    public class MandelbrotController : MonoBehaviour
    {
        private int positionKey, rotationKey ;

        [SerializeField] private Material material;
        [SerializeField] private Vector2 position;
        [SerializeField] private float zoom;
        [SerializeField] private float angle;

        Vector2 downMousePos;
        Vector2 downWorldPos;

        private Vector2 zoomFactor;

        void OnEnable()
        {
            positionKey = Shader.PropertyToID("_Position"); 
            rotationKey = Shader.PropertyToID("_Rotation"); 
        }

        Vector2 ScreenToWorld(Vector2 screenPos)
        {
            return new Vector2(zoomFactor.x*screenPos.x/Screen.width, zoomFactor.y*screenPos.y/Screen.height);
        }

        void Update()
        {
            var wheel = Input.GetAxis("Mouse ScrollWheel");
            if(wheel<0)
            {
                zoom *= 1.05f;
            }
            else if(wheel>0)
            {
                zoom *= 0.95f;
            }

            float ratio = (float)Screen.width/Screen.height;
            zoomFactor = (ratio>1) ? new Vector2(zoom * ratio, zoom) 
                       : new Vector2(zoom, ratio * zoom);

            if(Input.GetMouseButtonDown(0))
            {
                downMousePos = Input.mousePosition;
                downWorldPos = position;
            }

            if(Input.GetMouseButton(0))
            {
                Vector2 delta = (Vector2)Input.mousePosition - downMousePos;
                position = downWorldPos - ScreenToWorld(delta);
            }
     
            material.SetVector(rotationKey, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
            material.SetVector(positionKey, new Vector4(position.x, position.y, zoomFactor.x, zoomFactor.y));
        }
    }
}