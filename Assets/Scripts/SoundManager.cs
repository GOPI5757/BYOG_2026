using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private GameObject soundObjectPrefab;

    public AudioClip hoverSound;
    public AudioClip clickSound;

    public AudioClip coinSwitchSound;
    public AudioClip KingCaptureSound;
    public AudioClip PawnMovementSound;
    public AudioClip PawnTakeDownSound;
    public AudioClip SmashSound;

    private List<GameObject> Spawnedsoundobjects = new List<GameObject>();

    private int soundObjectIndex;

    public static SoundManager instance;

    private void Awake()
    {
        if(instance == null) instance = this;
        
    }

    void Start()
    {
        for (int i = 0; i < 10; i++) 
        {
            GameObject spawn = Instantiate(soundObjectPrefab, Vector3.zero, Quaternion.identity);
            Spawnedsoundobjects.Add(spawn);

            DontDestroyOnLoad(spawn);
        }

        DontDestroyOnLoad(this);
    }

    public void PlaySound(AudioClip clip)
    {
        Spawnedsoundobjects[soundObjectIndex].GetComponent<AudioSource>().clip = clip;
        Spawnedsoundobjects[soundObjectIndex].GetComponent<AudioSource>().Play();

        soundObjectIndex++;
        if(soundObjectIndex >= Spawnedsoundobjects.Count)
        {
            soundObjectIndex = 0;
        }
    }
}
