using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

public class AmbientSoundMixer : MonoBehaviour
{
    public AudioSource jungleAudioSource;
    public AudioSource beachAudioSource;
    public AudioSource mountainAudioSource;

    public BiomeDefinition jungleBiome;
    public BiomeDefinition beachBiome;
    public BiomeDefinition mountainBiome;

    // Voxelplay
    VoxelPlayEnvironment env;

    Transform player;

    public float transitionSpeed = 0.25f;
    Vector3 oldPosition;
    BiomeDefinition currentBiome;

    // Start is called before the first frame update
    private void Start()
    {
        env = VoxelPlayEnvironment.instance;
    }

    // Get local player so we can keep track of them
    public void PlayAmbience(Transform incPlayer)
    {
        player = incPlayer;

        jungleAudioSource.Play();
        beachAudioSource.Play();
        mountainAudioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (!player) return;

        Vector3 currentPosition = player.position;
        float distTravelled = FastVector.SqrDistanceByValue(oldPosition, currentPosition);
        if (distTravelled > 1f)
        {
            oldPosition = currentPosition;
            currentBiome = env.GetBiome(currentPosition);
        }

        if (currentBiome == jungleBiome) 
        {
            if (jungleAudioSource.volume < 1)
            {
                jungleAudioSource.volume += Time.deltaTime * transitionSpeed;
                if (beachAudioSource.volume > 0)
                    beachAudioSource.volume -= Time.deltaTime * transitionSpeed;
                if (mountainAudioSource.volume > 0.1f)
                    mountainAudioSource.volume -= Time.deltaTime * transitionSpeed;
            }
        }
        else if (currentBiome == beachBiome)
        {
            if (beachAudioSource.volume < 1)
            {
                beachAudioSource.volume += Time.deltaTime * transitionSpeed;
                if (jungleAudioSource.volume > 0.1f)         
                    jungleAudioSource.volume -= Time.deltaTime * transitionSpeed;
                if (mountainAudioSource.volume > 0)
                    mountainAudioSource.volume -= Time.deltaTime * transitionSpeed;
            }
        }
        else if(currentBiome == mountainBiome)
        {
            if (mountainAudioSource.volume < 1)
            {
                mountainAudioSource.volume += Time.deltaTime * transitionSpeed;
                if (jungleAudioSource.volume > 0.1f)
                    jungleAudioSource.volume -= Time.deltaTime * transitionSpeed;
                if (beachAudioSource.volume > 0)
                    beachAudioSource.volume -= Time.deltaTime * transitionSpeed;
            }
        }

    }

    public void ToggleMusic( bool state )
    {
        if (state)
        {
            jungleAudioSource.Play();
            beachAudioSource.Play();
            mountainAudioSource.Play();
        }
        else
        {
            jungleAudioSource.Stop();
            beachAudioSource.Stop();
            mountainAudioSource.Stop();
        }
            
    }
}
