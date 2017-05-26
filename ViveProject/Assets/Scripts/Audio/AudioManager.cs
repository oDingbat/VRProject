using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {

	AudioSource[]		jukeboxes = new AudioSource[0];
	public float				jukeboxCount;
	int							jukeboxIndex = 0;

	public GameObject			jukeboxPrefab;

	void Start () {
		InitializeJukeboxes();

		PlayClipAtPoint(null, new Vector3(0, 10, 0), 1);
		PlayClipAtPoint(null, new Vector3(0, 15, 0));
		PlayClipAtPoint(null, new Vector3(0, 15, 0));
		PlayClipAtPoint(null, new Vector3(0, 15, 0));
		PlayClipAtPoint(null, new Vector3(0, 15, 0));
		PlayClipAtPoint(null, new Vector3(0, 20, 0));
	}

	void InitializeJukeboxes() {
		List<AudioSource> newJukeboxes = new List<AudioSource>();

		for (int i = 0; i < jukeboxCount; i++) {
			AudioSource newJukebox = (AudioSource)Instantiate(jukeboxPrefab, Vector3.zero, Quaternion.identity).GetComponent<AudioSource>();
			newJukebox.transform.parent = transform;
			newJukebox.gameObject.name = "Jukebox (" + (i + 1) + ")";
			newJukeboxes.Add(newJukebox);
		}

		jukeboxes = newJukeboxes.ToArray();
	}

	public void PlayClipAtPoint (AudioClip clip, Vector3 pos, float volume) {
		jukeboxIndex = (jukeboxIndex == jukeboxes.Length - 1 ? 0 : jukeboxIndex + 1);
		jukeboxes[jukeboxIndex].Stop();
		jukeboxes[jukeboxIndex].transform.position = pos;
		jukeboxes[jukeboxIndex].clip = clip;
		jukeboxes[jukeboxIndex].volume = volume;
		jukeboxes[jukeboxIndex].Play();
	}

	public void PlayClipAtPoint(AudioClip clip, Vector3 pos) {
		jukeboxIndex = (jukeboxIndex == jukeboxes.Length - 1 ? 0 : jukeboxIndex + 1);
		jukeboxes[jukeboxIndex].Stop();
		jukeboxes[jukeboxIndex].transform.position = pos;
		jukeboxes[jukeboxIndex].clip = clip;
		jukeboxes[jukeboxIndex].Play();
	}


}
