using UnityEngine;

public class Window : InteractiveObject
{
    protected override void Start()
    {
        type = ObjectType.Window;
        base.Start();
    }
}