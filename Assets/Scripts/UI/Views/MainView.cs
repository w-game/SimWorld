using TMPro;
using UI.Models;
using UnityEngine;

namespace UI.Views
{
    public class MainView : ViewBase<MainViewModel>
    {
        [SerializeField] private TextMeshProUGUI time;

        public override void OnShow()
        {
            
        }

        void Update()
        {
            var t = GameManager.I.GameTime.CurrentTime;
            time.text = 
                $"{(int)(t / 3600):00}:{(int)((t % 3600) / 60):00}:{(int)(t % 60):00}";
        }
    }
}