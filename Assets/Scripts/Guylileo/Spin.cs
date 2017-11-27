using UnityEngine;

public class Spin : MonoBehaviour {
    [SerializeField] float dayDuration = 86400;

    float frequency;

	void OnEnable(){Init();}
    void OnValidate(){Init();}



    void Init()
    {
        frequency = 360f/dayDuration;
	}
	
	void Update () {
        float angle = frequency * Time.time;
        transform.localRotation = new Quaternion(){ eulerAngles = new Vector3(0,angle,0)};
	}
}
