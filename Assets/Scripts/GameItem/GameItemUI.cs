using GameItem;
using TMPro;
using UnityEngine;

public class GameItemUI : MonoBehaviour, IPoolable
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;
    [SerializeField] private TextMeshProUGUI itemNameText;

    public Collider2D Col => col;
    public IGameItem GameItem { get; private set; }

    public virtual void Init(IGameItem gameItem)
    {
        GameItem = gameItem;
    }

    internal void SetRenderer(string spritePath, float alpha = 1)
    {
        sr.sprite = Resources.Load<Sprite>(spritePath);
        sr.color = new Color(1, 1, 1, alpha);
    }

    public void SetName(string name)
    {
        if (itemNameText == null)
            return;
        itemNameText.text = name;
    }
    
    public virtual void OnGet()
    {
        if (col != null)
            col.enabled = true;
    }

    public virtual void OnRelease()
    {
        
    }
}