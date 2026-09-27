using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Interaction")]
    public float interactRange = 1.5f;
    public LayerMask interactableLayer;

    private InteractiveObject heldTarget;

    [Header("Stats")]
    public int totalLootValue = 0;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isCaught = false;
    private bool isStuck = false;
    private Collider2D col;
    private Vector2 botInput = Vector2.zero;
    private bool botMode = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        col.enabled = false;
        rb.simulated = false;
    }

    void Start()
    {
        
    }

    public void SetBotInput(Vector2 input)
    {
        botMode = true;
        botInput = input;
    }

    public void DisableBotMode()
    {
        botMode = false;
        botInput = Vector2.zero;
    }

    public void EnableCollision()
    {
        rb.simulated = true;
        col.enabled = true;
    }

    void Update()
    {
        if (isCaught || isStuck) return;
    
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;
    
        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    
        if (Input.GetKeyDown(KeyCode.F))
            TryStartUnblock();
    
        if (Input.GetKeyUp(KeyCode.F))
            TryCancelUnblock();
    }

    void TryStartUnblock()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, interactRange, interactableLayer);

        InteractiveObject closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            InteractiveObject obj = hit.GetComponent<InteractiveObject>();
            if (obj != null && obj.currentState == InteractiveObject.ObjectState.Blocked)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = obj;
                }
            }
        }

        if (closest != null)
        {
            heldTarget = closest;
            heldTarget.StartUnblock(transform.position);
        }
    }

    void TryCancelUnblock()
    {
        if (heldTarget != null)
        {
            heldTarget.CancelUnblock();
            heldTarget = null;
        }
    }

    void FixedUpdate()
    {
        if (isCaught || isStuck)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
    
        Vector2 activeInput = botMode ? botInput : moveInput;
        Vector2 newPosition = rb.position + activeInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    void TryInteract()
    {
        Collider2D[] debugHits = Physics2D.OverlapCircleAll(transform.position, interactRange);
        Debug.Log($"Hits ignoring layer: {debugHits.Length}");
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange, interactableLayer);
        Debug.Log($"Hits with layer mask: {hits.Length}");

        InteractiveObject closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            InteractiveObject obj = hit.GetComponent<InteractiveObject>();
            if (obj != null)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = obj;
                }
            }
        }

        if (closest != null)
            closest.UseObject(transform.position);
    }

    public void CollectLoot(int value)
    {
        totalLootValue += value;
        Debug.Log($"Loot collected! Total: {totalLootValue}");
        GameManager.Instance?.OnLootCollected(totalLootValue);
    }

    public void ResetForNewLevel()
    {
        isCaught = false;
        isStuck = false;
        totalLootValue = 0;
        rb.linearVelocity = Vector2.zero;
        EnableCollision();
    }

    public void GetCaught()
    {
        if (isCaught) return;
        isCaught = true;
        rb.linearVelocity = Vector2.zero;
        Debug.Log("Player caught! Game Over.");
        GameManager.Instance?.OnPlayerCaught();
    }

    public void GetTrapped(float duration)
    {
        if (!isStuck)
            StartCoroutine(StuckRoutine(duration));
    }

    private System.Collections.IEnumerator StuckRoutine(float duration)
    {
        isStuck = true;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(duration);
        isStuck = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Hunter"))
            GetCaught();
    }
}