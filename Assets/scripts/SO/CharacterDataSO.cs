using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu()]
public class CharacterDataSO : ScriptableObject
{
	public string characterName;
	public GameObject prefab;
	public GameObject portrait;
	public Sprite[] GUI;

	[Header("Base stats")]
	public int maxHealth;
	public float moveSpeed;
	public float explosionRadius;
	public int maxBombs;

	[Header("Ability")]
	public string abilityName;
	public string abilityDescription;
	public int abilityCooldown;
	public Sprite abilityIcon;
}
