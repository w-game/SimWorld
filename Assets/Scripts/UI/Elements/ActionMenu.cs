
using System;
using System.Collections.Generic;
using AI;
using GameItem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject actionButtonPrefab;
    private List<GameObject> _actionButtons = new List<GameObject>();

    private bool _isActionMenuVisible = false;

    private Vector3 _menuPosition;


    private void Start()
    {
        panel.SetActive(false);
        GameManager.I.ActionSystem.OnMouseClick += ShowEventMenu;
    }

    void Update()
    {
        if (_isActionMenuVisible)
        {
            panel.transform.position = UIManager.I.mainCamera.WorldToScreenPoint(_menuPosition);
        }
    }

    private void OnDestroy()
    {
        GameManager.I.ActionSystem.OnMouseClick -= ShowEventMenu;
    }

    private void ShowEventMenu(List<ActionBase> actions, Vector3 position)
    {
        _menuPosition = UIManager.I.mainCamera.ScreenToWorldPoint(position);
        UpdateActionButtons(actions);
        panel.SetActive(true);
        _isActionMenuVisible = true;
        Log.LogInfo("ActionMenu", "Event menu shown at position: " + position);
    }

    private void UpdateActionButtons(List<ActionBase> actions)
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
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                HideEventMenu();
                GameManager.I.ActionSystem.RegisterAction(action);
            });
            _actionButtons.Add(button);
        }
    }

    public void HideEventMenu()
    {
        panel.SetActive(false);
        _isActionMenuVisible = false;
    }
}