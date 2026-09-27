using UnityEngine;

public class Door : InteractiveObject
{
    protected override void Start()
    {
        type = ObjectType.Door;
        base.Start();
    }
}