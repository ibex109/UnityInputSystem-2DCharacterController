using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class Player : MonoBehaviour
{
    [SerializeField] private float m_JumpForce = 400f;
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;
    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;
    [SerializeField] private bool m_AirControl = false;
    [SerializeField] private LayerMask m_WhatIsGround;
    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private Transform m_CeilingCheck;
    [SerializeField] private Collider2D m_CrouchDisableCollider;

    const float k_GroundedRadius = .2f;
    private bool m_Grounded;
    const float k_CeilingRadius = .2f
    private Rigidbody2D m_Rigidbody2D;
    private bool m_FacingRight = true;
    private Vector3 m_Velocity = Vector3.zero;

	public float runSpeed = 40f;
	float horizontalMove = 0f;
	bool jumpInput = false;
	bool crouchInput = false;

	public InputActionAsset actionAsset;
	InputActionMap playerActionMap;
	InputAction moveInputAction;
	InputAction crouchInputAction;
	InputAction jumpInputAction;

	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();

		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();

		if (OnCrouchEvent == null)
			OnCrouchEvent = new BoolEvent();

		playerActionMap = actionAsset.FindActionMap("Player");
		moveInputAction = playerActionMap.FindAction("Move");
		jumpInputAction = playerActionMap.FindAction("Jump");
		crouchInputAction = playerActionMap.FindAction("Crouch");

		moveInputAction.performed += GetMoveInput;
		moveInputAction.canceled += GetMoveInput;

		jumpInputAction.performed += GetJumpInput;
		
		crouchInputAction.performed += GetCrouchInput;
		crouchInputAction.canceled += CancelCrouchInput;
	}

	private void GetMoveInput(InputAction.CallbackContext context)
	{
		horizontalMove = context.ReadValue<float>() * runSpeed;
	}

	private void GetJumpInput(InputAction.CallbackContext context)
	{
		jumpInput = true;
	}

	private void GetCrouchInput(InputAction.CallbackContext context)
	{
		crouchInput = true;
	}

	private void CancelCrouchInput(InputAction.CallbackContext context)
	{
		crouchInput = false;
	}

	private void OnEnable()
	{
		moveInputAction.Enable();
		jumpInputAction.Enable();
		crouchInputAction.Enable();
	}

	private void OnDisable()
	{
		moveInputAction.Disable();
		jumpInputAction.Disable();
		crouchInputAction.Disable();
	}

	[Header("Events")]
	[Space]

	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	public BoolEvent OnCrouchEvent;
	private bool m_wasCrouching = false;

	private void FixedUpdate()
	{
		bool wasGrounded = m_Grounded;
		m_Grounded = false;

		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		// This can be done using layers instead but Sample Assets will not overwrite your project settings.
		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;
				if (!wasGrounded)
					OnLandEvent.Invoke();
			}
		}

		Move(horizontalMove * Time.fixedDeltaTime, crouchInput, jumpInput);
		jumpInput = false;
	}


	public void Move(float move, bool crouch, bool jump)
	{
		if (crouch == false)
		{
			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
			{
				crouch = true;
			}
		}

		if (m_Grounded || m_AirControl)
		{

			// If crouching
			if (crouch)
			{
				if (!m_wasCrouching)
				{
					m_wasCrouching = true;
					OnCrouchEvent.Invoke(true);
				}

				move *= m_CrouchSpeed;

				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = false;
			}
			else
			{
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = true;

				if (m_wasCrouching)
				{
					m_wasCrouching = false;
					OnCrouchEvent.Invoke(false);
				}
			}

			Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
			m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

			if (move > 0 && !m_FacingRight)
			{
				Flip();
			}
			else if (move < 0 && m_FacingRight)
			{
				Flip();
			}
		}
		if (m_Grounded && jump)
		{
			m_Grounded = false;
			m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
		}
	}


	private void Flip()
	{
		m_FacingRight = !m_FacingRight;

		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
