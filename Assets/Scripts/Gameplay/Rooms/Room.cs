﻿using UnityEngine;

/// <summary>
/// A room that the player can enter.
/// </summary>
public abstract class Room : MonoBehaviour, Area {
    private BoxCollider2D _myCollider;

    void Start() {
        _myCollider = GetComponent<BoxCollider2D>();
    }

    public abstract void OnPlayerEnter(PlayerController pc);
    public abstract void OnPlayerExit(PlayerController pc);
    public abstract bool CanPlaceHere(Machine m);
}