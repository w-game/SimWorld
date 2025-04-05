using System;
using Citizens;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class AgentStateElement : MonoBehaviour
    {
        [SerializeField] private Slider hp;
        [SerializeField] private Image hunger;
        [SerializeField] private Image toilet;
        [SerializeField] private Image social;
        [SerializeField] private Image mood;
        [SerializeField] private Image sleep;
        [SerializeField] private Image hygiene;

        private void OnEnable()
        {
            AgentState.OnAgentStateChangedEvent += OnAgentStateChanged;
        }

        private void OnDisable()
        {
            AgentState.OnAgentStateChangedEvent -= OnAgentStateChanged;
        }

        private void OnAgentStateChanged(AgentState state)
        {
            if (hp != null) hp.value = state.Health / 100f;
            if (hunger != null) hunger.fillAmount = state.Hunger / 100f;
            if (toilet != null) toilet.fillAmount = state.Toilet / 100f;
            if (social != null) social.fillAmount = state.Social / 100f;
            if (mood != null) mood.fillAmount = state.Mood / 100f;
            if (sleep != null) sleep.fillAmount = state.Sleep / 100f;
            if (hygiene != null) hygiene.fillAmount = state.Hygiene / 100f;
        }
    }
}