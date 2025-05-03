using UnityEngine;
using UnityEngine.Events;

namespace Skill
{
    public class SkillBase
    {
        public int Level { get; protected set; } = 1;
        public float CurrentExp { get; private set; } = 0;
        public float ExpToNext => 500 * Mathf.Pow(1.5f, Level - 1);  // 例如指数增长

        public event UnityAction<int> OnLevelUp;

        public void AddExp(float amount)
        {
            CurrentExp += amount;
            Debug.Log($"[SkillSystem] Added {amount} exp, current exp: {CurrentExp}, level: {Level}");
            while (CurrentExp >= ExpToNext)
            {
                CurrentExp -= ExpToNext;
                Level++;
                OnLevelUp?.Invoke(Level);
            }
        }
    }

    public class PlantSkill : SkillBase
    {
        public PlantSkill()
        {
            OnLevelUp += OnLevelUpHandler;
        }

        private void OnLevelUpHandler(int level)
        {
            
        }
    }

    public class KnowledgeSkill : SkillBase
    {

    }

    public class CookSkill : SkillBase
    {
        
    }
}