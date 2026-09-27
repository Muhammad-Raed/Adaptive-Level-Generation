using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using System.Collections;
using NavMeshPlus.Components;

public abstract class InteractiveObject : MonoBehaviour
{
    public enum ObjectType { Door, Window }
    public enum ObjectState { Open, Blocked }

    [Header("Settings")]
    public string uniqueID;
    public ObjectType type;
    public ObjectState currentState = ObjectState.Open;

    [Header("Unblocking")]
    public float unblockDuration = 3f;
    private Coroutine unblockCoroutine;

    [Header("Side Detection")]
    public Vector3 startRoomDirection; 

    protected Collider2D col;

    protected virtual void Start()
    {
        col = GetComponent<Collider2D>();
        
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();

        if (DirectorManager.Instance != null)
            DirectorManager.Instance.RegisterObject(uniqueID, type.ToString(), transform.position, this);
        
        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle != null) obstacle.carving = false;
        
        UpdateCollider();
    }

    public virtual void UseObject(Vector3 userPosition)
    {
        if (currentState == ObjectState.Blocked)
        {
            Vector3 dirToUser = (userPosition - transform.position).normalized;
            if (Vector3.Dot(startRoomDirection, dirToUser) > 0)
            {
                currentState = ObjectState.Open;
                UpdateCollider();
                DirectorManager.Instance?.UpdateHeatMap(uniqueID, type.ToString(), true);
                Debug.Log("Unblocked strategically!");
            }
            else
            {
                Debug.Log("Blocked from this side!");
            }
            return;
        }

        currentState = ObjectState.Blocked;
        UpdateCollider();
        DirectorManager.Instance?.UpdateHeatMap(uniqueID, type.ToString(), false);
    }

    public void UpdateCollider()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (var c in cols)
            c.enabled = (currentState != ObjectState.Open);

        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle != null)
            obstacle.carving = (currentState != ObjectState.Open);
    }

    public void ForceOpen()
    {
        currentState = ObjectState.Open;
        UpdateCollider();
        DirectorManager.Instance?.UpdateHeatMap(uniqueID, type.ToString(), true);
    }

    public void StartUnblock(Vector3 userPosition)
    {
        Vector3 dirToUser = (userPosition - transform.position).normalized;
        if (Vector3.Dot(startRoomDirection, dirToUser) <= 0)
        {
            Debug.Log("Blocked from this side — cannot unblock.");
            return;
        }

        if (unblockCoroutine == null)
            unblockCoroutine = StartCoroutine(UnblockRoutine());
    }

    public void CancelUnblock()
    {
        if (unblockCoroutine != null)
        {
            StopCoroutine(unblockCoroutine);
            unblockCoroutine = null;
            Debug.Log("Unblock cancelled.");
        }
    }

    private IEnumerator UnblockRoutine()
    {
        Debug.Log($"Unblocking in {unblockDuration}s...");
        yield return new WaitForSeconds(unblockDuration);
        currentState = ObjectState.Open;
        UpdateCollider();
        DirectorManager.Instance?.UpdateHeatMap(uniqueID, type.ToString(), true);
        Debug.Log("Unblocked!");
        unblockCoroutine = null;
    }

}
