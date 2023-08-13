using CSTGames.CommonEnums;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class QuickPullingOrb : Interactable
{
	[Header("Pull Speed")]
	[Space]
	[SerializeField, Min(0f), Tooltip("How fast would this orb pull the player towards it?")]
	private float pullSpeed;
	
	[SerializeField, Min(0f), Tooltip("How fast would the line animation be?")]
	private float animationSpeed;
	
	[SerializeField, Min(0f), Tooltip("The maximum distance required before releasing the player.")]
	private float releaseRadius;
	
	[SerializeField, Min(0f), Tooltip("The orb will not response to interaction if the player is within this range.")]
	private float deadZoneRadius;

	[Header("UI")]
	[Space]
	[SerializeField] private Canvas worldCanvas;
	[SerializeField] private GameObject uiTextPrefab;

	// Colors.
	private readonly Color _normalColor = new Color(.87f, .74f, .24f);
	private readonly Color _readyColor = new Color(.43f, .79f, .22f);
	private readonly Color _pullingColor = new Color(.82f, .16f, .15f);
	
	// Private fields.
	private Material _mat;
	private LineRenderer _lineRenderer;
	private SphereCollider _collider;
	private Transform _playerSphere;
	private Transform _mainCam;
	private GameObject _uiTextClone;

	private static bool _isCoroutineRunning;

	protected override void Awake()
	{
		base.Awake();

		worldCanvas = GameObjectExtensions.GetComponentWithTag<Canvas>("WorldCanvas");

		_mat = GetComponent<MeshRenderer>().material;
		_lineRenderer = GetComponent<LineRenderer>();
		_collider = GetComponent<SphereCollider>();

		_playerSphere = player.Find("Placeholder Sphere");
		_mainCam = Camera.main.transform;
	}

	private void Start()
	{
		_lineRenderer.SetPosition(0, transform.position);
		_lineRenderer.SetPosition(1, transform.position);
	}

	protected override void Update()
	{
		if (_isCoroutineRunning)
			return;

		float distance = Vector3.Distance(transform.position, _playerSphere.position);

		if (distance <= interactRadius && distance > deadZoneRadius)
		{
			if (!_collider.enabled)
			{
				_collider.enabled = true;
				_uiTextClone = Instantiate(uiTextPrefab, worldCanvas.transform);
				_uiTextClone.transform.position = transform.position + Vector3.up * .7f;
				_uiTextClone.SetActive(false);
			}

			CheckRaycast();
		}
		else
		{
			if (_collider.enabled)
			{
				_collider.enabled = false;
				Destroy(_uiTextClone);
			}
			
			_mat.SetColor("_BaseColor", _normalColor);
		}
	}

	public override void Interact()
	{
		base.Interact();

		StartCoroutine(PullPlayer());
	}

	private void CheckRaycast()
	{
		Ray ray = new Ray(_mainCam.position, _mainCam.forward);
		
		int layerToCheck = 1 << this.gameObject.layer;
		bool hitSomething = Physics.Raycast(ray, out RaycastHit hitInfo, interactRadius, layerToCheck);

		if (hitSomething && hitInfo.transform.TryGetComponent<QuickPullingOrb>(out QuickPullingOrb Instance))
		{
			Instance._mat.SetColor("_BaseColor", _readyColor);
			Instance._uiTextClone.SetActive(true);

			if (InputManager.Instance.GetKeyDown(KeybindingActions.Interact) && !_isCoroutineRunning)
				Instance.Interact();
		}
		else
		{
			_mat.SetColor("_BaseColor", _normalColor);
			_uiTextClone.SetActive(false);
		}
	}

	private IEnumerator PullPlayer()
	{
		_isCoroutineRunning = true;

		// Disable the player and the UI text, change the material's color.
		player.gameObject.DisablePlayer();
		_mat.SetColor("_BaseColor", _pullingColor);
		_uiTextClone.SetActive(false);

		// Animate the line towards the player.
		Vector3 lineEnd = _lineRenderer.GetPosition(1);
		float lineDistance = Vector3.Distance(lineEnd, _playerSphere.position);

		while (lineDistance > .05f)
		{
			lineEnd = Vector3.Lerp(lineEnd, _playerSphere.position, Time.deltaTime * animationSpeed);
			_lineRenderer.SetPosition(1, lineEnd);
			lineDistance = Vector3.Distance(lineEnd, _playerSphere.position);

			yield return null;
		}

		// Pull the player towards this orb.
		float distanceToPlayer = Vector3.Distance(transform.position, player.position);

		while (distanceToPlayer > releaseRadius)
		{
			player.position = Vector3.Lerp(player.position, transform.position, Time.deltaTime * pullSpeed);
			_lineRenderer.SetPosition(1, _playerSphere.position);
			
			distanceToPlayer = Vector3.Distance(transform.position, player.position);

			yield return null;
		}

		// Reset the line renderer and enable the player.
		_lineRenderer.SetPosition(1, transform.position);
		player.gameObject.EnablePlayer();

		_isCoroutineRunning = false;
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();

		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(transform.position, releaseRadius);

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, deadZoneRadius);
	}
}
