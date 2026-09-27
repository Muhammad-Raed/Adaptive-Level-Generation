using UnityEngine;

public class LootGreedyBot : BotController
{
    protected override void BotDecision()
    {
        availableLoot.RemoveAll(l => l == null);

        switch (state)
        {
            case BotState.CollectingLoot:
                if (availableLoot.Count == 0)
                {
                    if (HasMinimumLoot() && escapeDoor != null)
                    {
                        state = BotState.Escaping;
                        SetDestination(escapeDoor.transform.position);
                    }
                    else if (escapeDoor != null)
                    {
                        state = BotState.Escaping;
                        SetDestination(escapeDoor.transform.position);
                    }
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
        hunterDetectRange = 3f;
        avoidanceStrength = 1.5f;
    }
}