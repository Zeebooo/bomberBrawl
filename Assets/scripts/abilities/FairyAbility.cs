using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FairyAbility : AbilityBase
{
	private ParticleSystem particleSystemComponent;
	private float abilityDuration = 8f;
	public override float EffectDuration => abilityDuration;

	private void Awake()
	{
		particleSystemComponent = Camera.main.GetComponent<ParticleSystem>();
	}

	protected override void ExecuteAbility()
	{
		GlitterServerRpc();
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void GlitterServerRpc()
	{
		List<ulong> targetIds = new();
		foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			if (clientId != OwnerClientId) // hoppa över castaren själv
				targetIds.Add(clientId);
		}

		ClientRpcParams targetParams = new()
		{
			Send = new ClientRpcSendParams { TargetClientIds = targetIds.ToArray() }
		};

		ShowGlitterClientRpc(targetParams);
		PlayAbilitySFXClientRpc();
	}

	[ClientRpc]
	private void ShowGlitterClientRpc(ClientRpcParams rpcParams = default)
	{
		StartCoroutine(GlitterRoutine());
	}

	private IEnumerator GlitterRoutine()
	{
		particleSystemComponent.Play();
		yield return new WaitForSeconds(abilityDuration);
		particleSystemComponent.Stop();
	}

	[ClientRpc]
	private void PlayAbilitySFXClientRpc()
	{
		SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.fairyAbilitySFX[0]);
	}
}
