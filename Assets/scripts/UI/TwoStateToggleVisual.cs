using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class TwoStateToggleVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private Image targetGraphic;
	[SerializeField] private Sprite offNormalSprite;
	[SerializeField] private Sprite offHoverSprite;
	[SerializeField] private Sprite onNormalSprite;
	[SerializeField] private Sprite onHoverSprite;

	private Toggle toggle;
	private bool isHovering;

	private void Awake()
	{
		toggle = GetComponent<Toggle>();
		toggle.onValueChanged.AddListener(_ => UpdateVisual());
	}

	private void Start()
	{
		UpdateVisual();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		isHovering = true;
		UpdateVisual();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		isHovering = false;
		UpdateVisual();
	}

	private void UpdateVisual()
	{
		targetGraphic.sprite = toggle.isOn
			? (isHovering ? onHoverSprite : onNormalSprite)
			: (isHovering ? offHoverSprite : offNormalSprite);
	}
}
