using GameItem;
using UnityEngine;

public class PlayerController : GameItemUI
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform hand;

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
            if (ui is PropGameItemUI propGameItemUI && propGameItemUI.CanBeTake)
            {
                if (propGameItemUI.GameItem is PropGameItem propGameItem)
                {
                    propGameItem.CheckPickUp(_agent);
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

        bool xy = Mathf.Abs(dir.x) > Mathf.Abs(dir.y);
        if (xy)
        {
            dir.y = 0;
        }
        else
        {
            dir.x = 0;
        }

        animator.SetFloat("Horizontal", dir.x > 0 ? 1 : (dir.x < 0 ? -1 : 0));
        animator.SetFloat("Vertical", dir.y > 0 ? 1 : (dir.y < 0 ? -1 : 0));
        // animator.SetFloat("Speed", dir.magnitude);

        _lastPos = curPos;

        if (_agent.ItemInHand != null)
        {
            var item = _agent.ItemInHand;
            item.Pos = hand.position;
        }
    }
}