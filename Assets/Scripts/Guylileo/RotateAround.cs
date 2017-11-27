using UnityEngine;

public class RotateAround : MonoBehaviour {
    [SerializeField] float yearDuration = 86400 * 365.25f;

    float distanceToStar;
    float frequency;
	void OnEnable()
	{
	    Init();
	}
    void Init()
    {
		distanceToStar = transform.localPosition.magnitude;
        frequency = 2*Mathf.PI/yearDuration;
	}
    void OnValidate()
    {
        Init();
    }
	
	void Update () {
        float angle = frequency * Time.time;
		transform.localPosition = distanceToStar * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
	}
}
