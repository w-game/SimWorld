
using System.Collections.Generic;
using AI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionMenu : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
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
        if (_isActionMenuVisible)
        {
            if (panel.rect.width == 0) return;

            panel.transform.position =
                UIManager.I.WorldPosToScreenPos(_menuPosition) +
                new Vector3(panel.rect.width / 2, -panel.rect.height / 2, 0);

            if (Vector3.Distance(Input.mousePosition, panel.transform.position) > panel.rect.width)
            {
                HideEventMenu();
            }
        }
    }

    private void OnDestroy()
    {
        GameManager.I.ActionSystem.OnMouseClick -= ShowEventMenu;
    }

    private void ShowEventMenu(List<IAction> actions, Vector3 position)
    {
        _menuPosition = UIManager.I.ScreenPosToWorldPos(position);
        UpdateActionButtons(actions);
        panel.gameObject.SetActive(true);
        _isActionMenuVisible = true;
    }

    private void UpdateActionButtons(List<IAction> actions)
    {
        foreach (var button in _actionButtons)
        {
            Destroy(button);
        }
        _actionButtons.Clear();

        foreach (var action in actions)
        {
            GameObject button = Instantiate(actionButtonPrefab, panel.transform);
            button.transform.GetComponentInChildren<TextMeshProUGUI>().text = action.ActionName;
            var btn = button.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                HideEventMenu();
                GameManager.I.ActionSystem.RegisterAction(action);
            });
            btn.interactable = action.Enable;
            _actionButtons.Add(button);
        }
    }

    public void HideEventMenu()
    {
        panel.gameObject.SetActive(false);
        _isActionMenuVisible = false;
    }
}