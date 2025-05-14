using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Elements
{
    public class DialogElement : MonoBehaviour, IPoolable
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform optionParent;

        private List<DialogOptionElement> _dialogOptionElements = new List<DialogOptionElement>();

        public void OnGet()
        {

        }

        public void OnRelease()
        {
            canvasGroup.alpha = 1;
            text.text = string.Empty;
        }

        public void ShowTextByCharacter(DialogData dialogData, float charInterval = 0.05f)
        {
            StopAllCoroutines();
            text.text = dialogData.Content;
            text.ForceMeshUpdate();
            text.maxVisibleCharacters = 0;
            StartCoroutine(TypeText(charInterval, () =>
            {
                dialogData.Callback?.Invoke();
                StartCoroutine(ShowOptions(dialogData));
            }));
        }

        private IEnumerator ShowOptions(DialogData dialogData)
        {
            foreach (var optionElement in _dialogOptionElements)
            {
                UIManager.I.ReleaseElement(optionElement, "Prefabs/UI/Elements/DialogOptionElement");
            }
            _dialogOptionElements.Clear();

            foreach (var option in dialogData.Options)
            {
                var optionElement = UIManager.I.GetElement<DialogOptionElement>("Prefabs/UI/Elements/DialogOptionElement", Vector3.zero, optionParent);
                optionElement.ShowText(_dialogOptionElements.Count + 1, option.Text);
                optionElement.OnClick.AddListener(() =>
                {
                    option.OnClick?.Invoke();
                    Hide();
                });
                _dialogOptionElements.Add(optionElement);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator TypeText(float charInterval, Action callback)
        {
            int totalChars = text.textInfo.characterCount;
            for (int i = 1; i <= totalChars; i++)
            {
                text.maxVisibleCharacters = i;
                yield return new WaitForSeconds(charInterval);
            }

            callback?.Invoke();
        }

        public void Hide()
        {
            DOTween.Sequence()
                .Append(canvasGroup.DOFade(0, 0.2f))
                .OnComplete(() =>
                {
                    GameManager.I.GameItemManager.ItemUIPool.Release(this, "Prefabs/UI/Elements/DialogElement");
                });
        }
    }
}