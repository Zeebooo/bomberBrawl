using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class JesterAbility : AbilityBase
{
	[SerializeField] private Sprite[] abilityIcons; // samma ordning som pool i Awake(): Builder, Ninja, Haldo, Monk

	private AbilityBase[] pool;

	private readonly NetworkVariable<int> currentAbilityIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public Sprite CurrentIcon => abilityIcons != null && currentAbilityIndex.Value < abilityIcons.Length
		? abilityIcons[currentAbilityIndex.Value]
		: null;

	private void Awake()
	{
		pool = new AbilityBase[]
		{
			GetComponent<BuilderAbility>(),
			GetComponent<NinjaAbility>(),
			GetComponent<HaldoAbility>(),
			GetComponent<MonkAbility>(),
			GetComponent<FairyAbility>(),
			GetComponent<GeologistAbility>(),
		};
	}

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
			currentAbilityIndex.Value = Random.Range(0, pool.Length);
	}

	protected override void ExecuteAbility()
	{
		AbilityBase chosen = pool[currentAbilityIndex.Value];
		chosen.TriggerEffect();
		StartCoroutine(ResolveAfterDelay(chosen));
	}

	private IEnumerator ResolveAfterDelay(AbilityBase chosen)
	{
		if (chosen.RunningCoroutine != null)
			yield return chosen.RunningCoroutine;
		else if (chosen.EffectDuration > 0f)
			yield return new WaitForSeconds(chosen.EffectDuration);

		if (chosen.LastEffectFailed)
		{
			// Misslyckat/avbrutet försök — ge tillbaka Jesters egen cooldown och behåll samma ability till nästa försök.
			ResetCooldown();
		}
		else
		{
			currentAbilityIndex.Value = Random.Range(0, pool.Length);
		}
	}
}
