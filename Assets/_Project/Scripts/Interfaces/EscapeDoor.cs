using UnityEngine;

public class EscapeDoor : MonoBehaviour
{
    private bool isUnlocked = false;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        SetVisual();
    }

    public void Unlock()
    {
        isUnlocked = true;
        SetVisual();
        Debug.Log("Escape door unlocked.");
    }

    private void SetVisual()
    {
        if (sr != null)
            sr.color = isUnlocked ? Color.green : Color.red;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isUnlocked) return;
        if (other.CompareTag("Player"))
            GameManager.Instance?.OnPlayerEscaped();
    }
}