using UnityEngine;

public class StartMovie : MonoBehaviour {

    MovieTexture texture;

	void OnEnable() {
        texture = ((MovieTexture)GetComponent<Renderer>().material.mainTexture);
	    texture.loop = true;
        texture.Play();
	}
}
