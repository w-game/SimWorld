using System;
using System.Collections;
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

        public void OnGet()
        {

        }

        public void OnRelease()
        {
            canvasGroup.alpha = 1;
            text.text = string.Empty;
        }

        public void ShowTextByCharacter(string content, UnityAction callback, float charInterval = 0.05f)
        {
            StopAllCoroutines();
            text.text = content;
            text.ForceMeshUpdate();
            text.maxVisibleCharacters = 0;
            StartCoroutine(TypeText(charInterval, callback));
        }

        private IEnumerator TypeText(float charInterval, UnityAction callback)
        {
            int totalChars = text.textInfo.characterCount;
            for (int i = 1; i <= totalChars; i++)
            {
                text.maxVisibleCharacters = i;
                yield return new WaitForSeconds(charInterval);
            }

            yield return new WaitForSeconds(0.5f);
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