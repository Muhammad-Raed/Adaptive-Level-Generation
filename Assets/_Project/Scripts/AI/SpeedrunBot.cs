using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class SpeedrunBot : BotController
{
    private Queue<LootItem> targetQueue = new Queue<LootItem>();
    private bool queueBuilt = false;

    protected override void BotDecision()
    {
        availableLoot.RemoveAll(l => l == null);

        switch (state)
        {
            case BotState.CollectingLoot:
                if (!queueBuilt)
                {
                    BuildTargetQueue();
                    queueBuilt = true;
                }
                while (targetQueue.Count > 0 && targetQueue.Peek() == null)
                    targetQueue.Dequeue();

                if (HasMinimumLoot())
                {
                    state = BotState.Escaping;
                    if (escapeDoor != null)
                        SetDestination(escapeDoor.transform.position);
                    return;
                }

                if (targetQueue.Count > 0)
                {
                    LootItem target = targetQueue.Peek();
                    if (target != null)
                        SetDestination(target.transform.position);
                    else
                        targetQueue.Dequeue();
                }
                else
                {
                    LootItem nearest = GetNearestLoot();
                    if (nearest != null)
                        SetDestination(nearest.transform.position);
                }
                break;

            case BotState.Escaping:
                if (ReachedDestination())
                    state = BotState.Done;
                break;
        }
    }

    private void BuildTargetQueue()
    {
        int required = GameManager.Instance != null ? GameManager.Instance.minimumLootRequired : 100;
        int accumulated = 0;

        var sorted = availableLoot
            .Where(l => l != null)
            .OrderByDescending(l => l.lootValue)
            .ToList();

        targetQueue.Clear();
        foreach (var loot in sorted)
        {
            if (accumulated >= required) break;
            targetQueue.Enqueue(loot);
            accumulated += loot.lootValue;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        hunterDetectRange = 5f;
        avoidanceStrength = 2f;
    }

}