
using System.Collections.Generic;
using AI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ActionMenu : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private GameObject actionTitlePrefab;
    [SerializeField] private GameObject actionButtonPrefab;
    private List<GameObject> _actionButtons = new List<GameObject>();

    private bool _isActionMenuVisible = false;

    private Vector3 _menuPosition;


    private void Start()
    {
        panel.gameObject.SetActive(false);
        GameManager.I.ActionSystem.OnMouseClick += ShowEventMenu;
    }

    void Update()
    {
        if (!_isActionMenuVisible || panel.rect.width == 0)
            return;

        Vector2 screenPoint = UIManager.I.WorldPosToScreenPos(_menuPosition);
        Vector2 localPoint;
        RectTransform canvasRect = panel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        // 将屏幕坐标转换为 Canvas 内部的局部坐标（更稳定）
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);

        // 添加偏移（例如右移 20）
        localPoint += new Vector2(panel.rect.width / 2, -panel.rect.height / 2);

        panel.anchoredPosition = localPoint;

        float maxDistance = Mathf.Max(panel.rect.width, panel.rect.height) + 200f;
        if (Vector3.Distance(Input.mousePosition, panel.position) > maxDistance)
        {
            HideEventMenu();
        }
    }

    private void OnDestroy()
    {
        GameManager.I.ActionSystem.OnMouseClick -= ShowEventMenu;
    }

    private void ShowEventMenu(Dictionary<string, List<IAction>> actions, Vector3 position)
    {
        _menuPosition = UIManager.I.ScreenPosToWorldPos(position);
        UpdateActionButtons(actions);
        panel.gameObject.SetActive(true);
        _isActionMenuVisible = true;
    }

    private void UpdateActionButtons(Dictionary<string, List<IAction>> actions)
    {
        foreach (var button in _actionButtons)
        {
            Destroy(button);
        }
        _actionButtons.Clear();

        foreach (var title in actions.Keys)
        {
            GameObject titleObj = Instantiate(actionTitlePrefab, panel.transform);
            titleObj.transform.GetComponentInChildren<TextMeshProUGUI>().text = title;
            _actionButtons.Add(titleObj);

            var slotGO = new GameObject("Slot", typeof(RectTransform));
            var slot = slotGO.transform as RectTransform;
            slot.SetParent(panel.transform);
            slot.localScale = Vector3.one;
            var slotGroup = slot.AddComponent<VerticalLayoutGroup>();
            slotGroup.childControlWidth = true;
            slotGroup.childForceExpandWidth = true;
            slotGroup.childControlHeight = true;
            slotGroup.childForceExpandHeight = false;
            slotGroup.spacing = 10;
            slotGroup.padding = new RectOffset(20, 0, 0, 0);
            _actionButtons.Add(slot.gameObject);

            foreach (var action in actions[title])
            {
                CreateActionButton(action, slot);
            }
        }
    }

    private void CreateActionButton(IAction action, Transform slot)
    {
        GameObject button = Instantiate(actionButtonPrefab, slot);
        button.transform.GetComponentInChildren<TextMeshProUGUI>().text = action.ActionName;
        var btn = button.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            HideEventMenu();
            GameManager.I.ActionSystem.RegisterAction(action);
        });
        btn.interactable = action.Enable;
    }

    public void HideEventMenu()
    {
        panel.gameObject.SetActive(false);
        _isActionMenuVisible = false;
    }
}