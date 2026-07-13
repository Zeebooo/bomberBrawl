using UnityEngine;

[CreateAssetMenu()]
public class AudioRefsSO : ScriptableObject
{
	public AudioClip[] backgroundMusic;
	public AudioClip[] explosionsSFX;
	public AudioClip[] positiveClickSFX;
	public AudioClip[] negativeClickSFX;
	public AudioClip[] victorySoundSFX;
	public AudioClip[] deathSoundSFX;
	public AudioClip[] powerUpSFX;
	public AudioClip[] swooshSFX;
	[Header("Character SFX")]
	public AudioClip[] ninjaAbilitySFX;
	public AudioClip[] builderAbilitySFX;
}
