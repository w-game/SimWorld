using Citizens;
using GameItem;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : GameItemUI
{
    [SerializeField] private Image actionProgress;
    private Agent _agent;

    public override void Init(GameItemBase gameItem)
    {
        _agent = gameItem as Agent;
        _agent.Brain.OnActionProgress += OnActionProgress;
    }

    private void OnActionProgress(float curProgress)
    {
        actionProgress.fillAmount = curProgress / 100f;
    }

    public override void OnGet()
    {
        actionProgress.fillAmount = 0f;
    }

    public override void OnRelease()
    {
        _agent.Brain.OnActionProgress -= OnActionProgress;
    }
}