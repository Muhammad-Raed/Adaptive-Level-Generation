using UnityEngine;

public class LootItem : MonoBehaviour
{
    public int lootValue = 0;
    private bool collected = false;

    void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
    void OnTriggerStay2D(Collider2D other)  => TryCollect(other);

    private void TryCollect(Collider2D other)
    {
        if (collected) return;
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                collected = true;
                player.CollectLoot(lootValue);
                Destroy(gameObject);
            }
        }
    }
}