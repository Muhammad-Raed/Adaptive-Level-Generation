using UnityEngine;

public class RoomLootSpawner : MonoBehaviour
{
    [Header("Loot Settings")]
    public GameObject lootPrefab;
    public int maxLootItems = 3;

   public void SpawnLoot(bool isLeafRoom)
    {
        float spawnChance = isLeafRoom ? 0.7f : 0.3f;
        int itemsToSpawn = Mathf.Max(1, Mathf.RoundToInt(maxLootItems * spawnChance));

        for (int i = 0; i < itemsToSpawn; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 4f;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);

            GameObject lootObj = Instantiate(lootPrefab, spawnPos, Quaternion.identity);
            lootObj.transform.parent = this.transform;

            LootItem loot = lootObj.GetComponent<LootItem>();
            if (loot != null)
                loot.lootValue = Random.Range(30, 76);
        }
    }
}