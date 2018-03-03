//#define SHOWMOUSE


using UnityEngine;

namespace Fractal
{
    public class MandelbrotController : MonoBehaviour
    {
        private int  matrixKey ;
#if SHOWMOUSE
        int mouseKey;
#endif

        [SerializeField] private Material material;
        [SerializeField] private Vector2 position;
        [SerializeField] private float zoom;
        [SerializeField] private float angle;

        Vector2[] downMousePos = new Vector2[3];
        Vector2[] downWorldPos = new Vector2[3];

        Matrix4x4 rasterToWorld;
        Vector3 zoomFactor;

        void OnValidate()
        {
            OnEnable();
            material.SetMatrix(matrixKey, rasterToWorld);
        }

        void OnEnable()
        {
#if SHOWMOUSE
            mouseKey = Shader.PropertyToID("_Mouse"); 
#endif
            matrixKey = Shader.PropertyToID("_rTW"); 
            SetupMatrix();
        }

        private void SetupMatrix()
        {
            float ratio = (float)Screen.width/Screen.height;
            zoomFactor = (ratio>1) ? new Vector3(zoom * ratio, zoom,1) 
                                   : new Vector3(zoom, zoom / ratio,1);
            rasterToWorld.SetTRS(position, Quaternion.Euler(0,0,angle), zoomFactor);
        }

        Vector2 ScreenToWorld(Vector2 screenPos)
        {
            screenPos.x /= Screen.width;
            screenPos.y /= Screen.height;
            return rasterToWorld * screenPos;
        }

        void Update()
        {
            var wheel = Input.GetAxis("Mouse ScrollWheel");

            Vector2 mousePosition = Input.mousePosition;
            Vector2? preZoomWorldPos;

            if(wheel<0)
            {
                preZoomWorldPos = ScreenToWorld(mousePosition);
                zoom *= 1.05f;
                SetupMatrix();
            }
            else if(wheel>0)
            {
                preZoomWorldPos = ScreenToWorld(mousePosition);
                zoom *= 0.95f;
                SetupMatrix();
            }
            else
            {
                preZoomWorldPos = null;
            }


            if(preZoomWorldPos.HasValue)
            {
                Vector2 newWorldPos = ScreenToWorld(mousePosition);
                position -= newWorldPos - preZoomWorldPos.Value;
                SetupMatrix();
            }

            for(int i = 0; i < 2; ++i)
            {
                if(Input.GetMouseButtonDown(i))
                {
                    downMousePos[i] = mousePosition;
                    switch(i)
                    {
                        case 0 : 
                            downWorldPos[i] = position;
                            break;
                        case 1 :
                            downWorldPos[i].x = angle;
                            break;
                    }
                }
            }

            if(Input.GetMouseButton(0))
            {
                Vector2 delta = mousePosition - downMousePos[0];
                position = downWorldPos[0] - ScreenToWorld(delta);
                SetupMatrix();
            }

            if(Input.GetMouseButton(1))
            {
                Vector2 center = 0.5f*new Vector2(Screen.width, Screen.height);
                Vector2 d1 = (downMousePos[1] - center).normalized;
                Vector2 d2 = (mousePosition - center).normalized;

                float cos = Vector2.Dot(d1,d2);
                Vector3 cross = Vector3.Cross(d1,d2);
                float sin = cross.magnitude * Mathf.Sign(cross.z);
                angle = downWorldPos[1].x - Mathf.Atan2(sin,cos) * Mathf.Rad2Deg ;
                Vector2 delta = ScreenToWorld(center);
                SetupMatrix();
                delta -= ScreenToWorld(center);;
                position += delta;
                SetupMatrix();
            }



#if SHOWMOUSE
            material.SetVector(mouseKey, new Vector4(mousePosition.x/Screen.width, mousePosition.y/Screen.height));
#endif
            material.SetMatrix(matrixKey, rasterToWorld);
        }
    }
}