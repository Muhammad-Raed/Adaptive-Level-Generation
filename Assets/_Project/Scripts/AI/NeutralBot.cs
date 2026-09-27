using UnityEngine;
public class NeutralBot : BotController
{
    [Header("Neutral Settings")]
    [Tooltip("Multiplier over minimumLootRequired before escaping. 1.5 = 50% more than minimum.")]
    public float lootBufferMultiplier = 1.5f;

    private int targetLoot = -1;

    protected override void BotDecision()
    {
        availableLoot.RemoveAll(l => l == null);

        if (targetLoot < 0 && GameManager.Instance != null)
            targetLoot = Mathf.RoundToInt(GameManager.Instance.minimumLootRequired * lootBufferMultiplier);

        switch (state)
        {
            case BotState.CollectingLoot:
                bool reachedTarget = player.totalLootValue >= targetLoot;
                bool noLootLeft = availableLoot.Count == 0;

                if ((reachedTarget || noLootLeft) && HasMinimumLoot())
                {
                    state = BotState.Escaping;
                    if (escapeDoor != null)
                        SetDestination(escapeDoor.transform.position);
                    return;
                }

                LootItem nearest = GetNearestLoot();
                if (nearest != null)
                    SetDestination(nearest.transform.position);
                break;

            case BotState.Escaping:
                if (ReachedDestination())
                    state = BotState.Done;
                break;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        hunterDetectRange = 4f;
        avoidanceStrength = 2f;
        lootBufferMultiplier = 1.5f;
    }

}