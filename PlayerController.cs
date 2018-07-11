using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour {

    public Rigidbody2D rb;
    public Animator animator;
    public ParticleSystem jetpackParticles;
    public float speed, jumpVelocity;
    public int gameState; // 0 - running; 1 - flapping; 2 - flying

    public Transform groundedPoint;
    public LayerMask groundMask;
        
    private Vector2 moveVel;
    private bool jumped;
    private float fallMultiplier = 3f;
    private bool isDead;

    private Collider2D playerCollider;    

    private bool startedFlying;
    private bool firstFly;

    private void Start()
    {
        playerCollider = gameObject.GetComponent<BoxCollider2D>();        
    }

    private void Update()
    {
        if (gameState == 0)
        {
            if (jumped && IsGrounded())
            {
                jumped = false;
                animator.SetTrigger("running");
            }

#if UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                Jump();
            }
#elif UNITY_ANDROID
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                // Check if finger is over a UI element
                if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                {
                    Jump();
                }
            }
#endif
        }
        else if (gameState == 1)
        {
#if UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                Flap();
            }
#elif UNITY_ANDROID
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                // Check if finger is over a UI element
                if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                {
                    Flap();
                }
            }
#endif
        }
        else if (gameState == 2)
        {
#if UNITY_STANDALONE
            if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                startedFlying = true;
            }
            else if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                StopFlying();
            }
#elif UNITY_ANDROID
            if (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Began 
                || Input.GetTouch(0).phase == TouchPhase.Moved 
                || Input.GetTouch(0).phase == TouchPhase.Stationary))
            {
                // Check if finger is over an UI element
                if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                {
                    startedFlying = true;
                }
            }
            else if(Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Canceled 
                || Input.GetTouch(0).phase == TouchPhase.Ended))
            {
                StopFlying();
            }
#endif
        }
    }

    private void FixedUpdate()
    {
        if (isDead || !InGameController.isGameRunning)
            return;

        moveVel = rb.velocity;
        moveVel.x = speed * Time.deltaTime;

        rb.velocity = moveVel;

        if(gameState == 0 && rb.velocity.y < 0) //Running and falling  ->  Start falling faster
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * fallMultiplier * Time.deltaTime;
        }

        if (gameState == 2) //Flying
        {
            if (moveVel.y < -5) //Starts falling too fast
            {
                rb.velocity = new Vector2(moveVel.x, -5);
            }
            
            if (startedFlying)
            {
                Fly();
            }
        }
    }

    private void Jump()
    {
        if (InGameController.isGameRunning)
        {
            if (rb.velocity.y == 0 || IsGrounded())
            {
                AudioManager.instance.Play("jump");
                jumped = true;
                animator.SetTrigger("jump");
                rb.velocity = jumpVelocity * Vector2.up;
            }
        }
    }

    private void Flap()
    {
        AudioManager.instance.Play("jump");
        animator.SetTrigger("flap");
        rb.velocity = (jumpVelocity * 0.8f) * Vector2.up;
    }

    private void Fly()
    {
        if (!firstFly)
        {
            AudioManager.instance.Play("jetpack");

            firstFly = true;
            jetpackParticles.Play();
        }

        if (moveVel.y > 5)
        {
            rb.velocity = new Vector2(moveVel.x, 5);
        }

        rb.velocity += Vector2.up * 0.9f;
    }

    private void StopFlying()
    {
        AudioManager.instance.Stop("jetpack");
        jetpackParticles.Stop();
        startedFlying = false;
        firstFly = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Collider2D collider = collision.collider;

        if (collider.CompareTag("Ground"))
        {
            if(!IsCollisionValid(collision.collider))
            {
                Die();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Obstacle"))
        {
            Die();
        }
    }

    private bool IsCollisionValid(Collider2D collider) //Check if the top side of the ground/crate is hit
    {
        Vector3 contactPoint = playerCollider.bounds.min;
        Vector3 topColliderPoint = collider.bounds.max;
        
        bool top = contactPoint.y > topColliderPoint.y;

        return top;
    }

    private void Die()
    {
        if (!isDead)
        {
            if (gameState == 2)
            {
                AudioManager.instance.Stop("jetpack");
            }

            rb.bodyType = RigidbodyType2D.Static;
            AudioManager.instance.Play("fart");
            rb.velocity = new Vector2(0, 0);
            animator.enabled = false;
            isDead = true;

            InGameController.instance.HandlePlayerDeath();
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapBox(groundedPoint.position, new Vector2(0.1f, 0.3f), 0, groundMask); //0.5f, 0.3f       
    }
}
