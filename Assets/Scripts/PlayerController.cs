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

        animator.SetFloat("Horizontal", dir.x > 0 ? 1 : (dir.x < 0 ? -1 : 0));
        animator.SetFloat("Vertical", dir.y > 0 ? 1 : (dir.y < 0 ? -1 : 0));
        // animator.SetFloat("Speed", dir.magnitude);

        _lastPos = curPos;
    }
}