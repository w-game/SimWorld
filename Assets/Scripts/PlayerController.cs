using System;
using Citizens;
using GameItem;
using UnityEngine;

public class PlayerController : GameItemUI
{
    [SerializeField] private Rigidbody2D rb;

    public Rigidbody2D Rb => rb;
    private Agent _agent;

    public override void Init(IGameItem gameItem)
    {
        _agent = gameItem as Agent;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("GameItem"))
        {
            var ui = other.GetComponent<GameItemUI>();
            if (ui != null)
            {
                if (ui.GameItem is PropGameItem propGameItem)
                {
                    propGameItem.BePickedUp(_agent);
                }
            }
        }
    }

    public void MoveTo(Vector2 target)
    {
        rb.MovePosition(target);
    }
}