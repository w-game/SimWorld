using System;
using Citizens;
using GameItem;
using UnityEngine;

public class PlayerController : GameItemUI
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    public Rigidbody2D Rb => rb;
    private Agent _agent;

    private Vector3 _lastPos;

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

    public void FixedUpdate()
    {
        var curPos = transform.position;
        var dir = curPos - _lastPos;
        var moved = dir.magnitude > 0.01f;

        animator.SetBool("MoveRight", moved && dir.x > 0f);
        animator.SetBool("MoveLeft", moved && dir.x < 0f);
        animator.SetBool("MoveUp", moved && dir.y > 0f);
        animator.SetBool("MoveDown", moved && dir.y < 0f);

        _lastPos = curPos;
    }
}