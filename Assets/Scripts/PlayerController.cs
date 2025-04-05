using System;
using AI;
using Citizens;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Image actionProgress;
    
    private Rigidbody2D rb;
    private Agent _agent;

    void Start()
    {
        // 获取 Rigidbody2D 组件
        rb = GetComponent<Rigidbody2D>();
        _agent = GetComponent<Agent>();
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
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        Vector2 movement = new Vector2(moveX, moveY) * _agent.MoveSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);

        if (Input.GetMouseButtonDown(0))
        {
            var blockType = MapManager.I.CheckClickOnMap(out var mouseWorldPos);
            switch (blockType)
            {
                case BlockType.Ocean:
                    break;
                case BlockType.Plain:
                    _agent.MoveToTarget(mouseWorldPos);
                    break;
            }
        }
    }

    private void OnActionProgress(float curProgress)
    {
        actionProgress.fillAmount = curProgress / 100f;
    }
}