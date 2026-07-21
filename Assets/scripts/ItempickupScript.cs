using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(NetworkObject))]
public class ItempickupScript : NetworkBehaviour
{
	public static ItempickupScript Instance { get; private set; }
	public enum itemTypes
	{
		None,
		Movement,
		MoreBombs,
		ExplosionRange,
		Health,
		RemoteBomb,
		PenetrateWalls,
		Shield,
		GodMode
	}

	public enum itemRarity
	{
		Common,
		Rare,
		Legendary
	}

	[Header("Items settings")]
	private float moveSpeedMultiplier = 1.03f;
	private float explosionRangeMultiplier = 1.2f;
	private int healthMultiplier = 1;
	private float godModeDuration = 10f;

	[Header("World event settings")]
	private static readonly string[] worldEvents = new string[] { "MixUp", "Switch", "Darkness", "Slippery" };
	private float worldEventChance = 0.1f; // 0.1f
	private float mixUpDuration = 10f;
	private float darknessDuration = 10f;
	private float slipperyDuration = 8f;

	public itemTypes item;
	public itemRarity rarity;

	void Awake()
	{
		Instance = this;
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (!collision.gameObject.CompareTag("Player")) return;

		NetworkObject playerNetObj = collision.gameObject.GetComponent<NetworkObject>();
		if (playerNetObj == null || !playerNetObj.IsOwner) return;

		PlaySound();
		PickupItemRpc(playerNetObj.NetworkObjectId);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	void PickupItemRpc(ulong playerNetworkObjectId)
	{
		if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerNetObj)) return;

		characterScript character = playerNetObj.GetComponent<characterScript>();
		bombScript bomb = playerNetObj.GetComponent<bombScript>();
		if (character == null) return;

		GeologistAbility geologist = character.GetAbility() as GeologistAbility;
		bool doubleActive = geologist != null && geologist.IsAbilityActive();

		switch (item)
		{
			case itemTypes.Movement:
				character.increaseMovementSpeed(doubleActive ? doubleMultiplier(moveSpeedMultiplier) : moveSpeedMultiplier);
				if (doubleActive) geologist.ConsumeDoubling();
				break;
			case itemTypes.MoreBombs:
				bomb.addBomb(doubleActive ? 2 : 1);
				if (doubleActive) geologist.ConsumeDoubling();
				break;
			case itemTypes.ExplosionRange:
				bomb.increaseExplosionRadius(doubleActive ? doubleMultiplier(explosionRangeMultiplier) : explosionRangeMultiplier);
				if (doubleActive) geologist.ConsumeDoubling();
				break;
			case itemTypes.Health:
				character.addHealth(doubleActive ? healthMultiplier * 2 : healthMultiplier);
				if (doubleActive) geologist.ConsumeDoubling();
				break;
			case itemTypes.RemoteBomb:
				bomb.setRemoteBomb();
				break;
			case itemTypes.PenetrateWalls:
				bomb.setPenetrateWalls();
				break;
			case itemTypes.Shield:
				character.activateShield();
				break;
			case itemTypes.GodMode:
				character.activateGodMode(godModeDuration);
				break;
		}

		float worldEventRoll = Random.value;
		if (worldEventRoll <= worldEventChance)
		{
			switch (worldEvents[Random.Range(0, worldEvents.Length)])
			{
				case "MixUp":
					WorldEventHandler.Instance.TriggerMixupRpc(mixUpDuration, "MixUp");
					break;
				case "Switch":
					WorldEventHandler.Instance.TriggerSwitchRpc(0f, "Switch");
					break;
				case "Darkness":
					WorldEventHandler.Instance.TriggerDarknessRpc(darknessDuration, "Darkness");
					break;
				case "Slippery":
					WorldEventHandler.Instance.TriggerSlipperyRpc(slipperyDuration, "Slippery");
					break;
			}
		}

		GetComponent<NetworkObject>().Despawn();
	}

	void PlaySound()
	{
		switch (rarity)
		{
			case itemRarity.Common:
				SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.powerUpSFX[0]);
				break;
			case itemRarity.Rare:
				SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.powerUpSFX[1]);
				break;
			case itemRarity.Legendary:
				SoundManager.Instance.PlaySFX(SoundManager.Instance.AudioRefs.powerUpSFX[2]);
				break;
		}
	}

	private float doubleMultiplier(float multiplier)
	{
		return multiplier * 2f;
	}

	public static string[] getWorldEvents() => worldEvents;
}
