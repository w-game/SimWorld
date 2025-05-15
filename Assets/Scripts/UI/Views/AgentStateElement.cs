using System;
using Citizens;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class AgentStateElement : MonoBehaviour
    {
        [SerializeField] private Slider hp;
        [SerializeField] private Slider hunger;
        // [SerializeField] private Image toilet;
        // [SerializeField] private Image social;
        [SerializeField] private Slider mood;
        [SerializeField] private Slider sleep;
        [SerializeField] private Slider hygiene;

        private void OnEnable()
        {
            GameManager.I.CurrentAgent.State.OnAgentStateChangedEvent += OnAgentStateChanged;
        }

        private void OnDisable()
        {
            GameManager.I.CurrentAgent.State.OnAgentStateChangedEvent -= OnAgentStateChanged;
        }

        private void OnAgentStateChanged(AgentState state)
        {
            if (hp != null) hp.value = state.Health.Value / 100f;
            if (hunger != null) hunger.value = state.Hunger.Value / 100f;
            // if (toilet != null) toilet.fillAmount = state.Toilet.Value / 100f;
            // if (social != null) social.fillAmount = state.Social.Value / 100f;
            if (mood != null) mood.value = state.Mood.Value / 100f;
            if (sleep != null) sleep.value = state.Sleep.Value / 100f;
            if (hygiene != null) hygiene.value = state.Hygiene.Value / 100f;
        }
    }
}