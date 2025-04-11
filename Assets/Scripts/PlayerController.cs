using AI;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : GameItemUI
{
    [SerializeField] private Image actionProgress;
    
    private Rigidbody2D rb;

    void Start()
    {
        // 获取 Rigidbody2D 组件
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        ActionBase.OnProgress += OnActionProgress;
    }

    private void OnDisable()
    {
        ActionBase.OnProgress -= OnActionProgress;
    }

    void FixedUpdate()
    {
        // float moveX = Input.GetAxis("Horizontal");
        // float moveY = Input.GetAxis("Vertical");
        // Vector2 movement = new Vector2(moveX, moveY) * _agent.MoveSpeed * Time.deltaTime;
        // rb.MovePosition(rb.position + movement);
    }

    private void OnActionProgress(float curProgress)
    {
        actionProgress.fillAmount = curProgress / 100f;
    }
}