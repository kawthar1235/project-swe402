using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class RoomAmbienceZone : MonoBehaviour
{
    public RoomAmbienceManager manager;

    public RoomAmbienceManager.Area area =
        RoomAmbienceManager.Area.FirstRoom;

    public int priority = 10;

    [Header("Optional player reference")]
    public Transform playerRoot;

    [Header("Start monster sound when player exits")]
    public bool startMonsterSoundOnExit;
    public MonsterChaseSound monsterSound;

    public bool IsPlayerInside => occupants.Count > 0;

    private readonly HashSet<Collider> occupants =
        new HashSet<Collider>();

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        if (manager == null)
            manager = FindFirstObjectByType<RoomAmbienceManager>();
    }

    private bool IsPlayer(Collider other)
    {
        if (playerRoot != null)
            return other.transform.IsChildOf(playerRoot);

        return other.GetComponentInParent<PlayerMovement>() != null;
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        RegisterPlayer(other);
    }

    private void RegisterPlayer(Collider other)
    {
        if (!IsPlayer(other))
            return;

        occupants.Add(other);

        if (manager != null)
            manager.RegisterZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!occupants.Remove(other))
            return;

        occupants.RemoveWhere(col =>
            col == null ||
            !col.enabled ||
            !col.gameObject.activeInHierarchy);

        if (occupants.Count > 0)
            return;

        if (manager != null)
            manager.UnregisterZone(this);

        if (startMonsterSoundOnExit && monsterSound != null)
            monsterSound.BeginChaseSound();
    }

    private void OnDisable()
    {
        occupants.Clear();

        if (manager != null)
            manager.UnregisterZone(this);
    }
}