using UnityEngine;

public class CorridorWalls : MonoBehaviour
{
    public float wallThickness = 0.3f;

    void Awake()
    {
        float worldLength = transform.localScale.x;
        float halfWidth   = transform.localScale.y / 2f;

        CreateWall("Wall_Top",    halfWidth,  worldLength);
        CreateWall("Wall_Bottom", -halfWidth, worldLength);
    }

    private void CreateWall(string wallName, float localYOffset, float worldLength)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(this.transform, false);

        wall.transform.localPosition = new Vector3(
            0f,
            localYOffset / transform.localScale.y,
            0f
        );

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();

        col.size = new Vector2(
            worldLength / transform.localScale.x,
            wallThickness / transform.localScale.y
        );
    }
}