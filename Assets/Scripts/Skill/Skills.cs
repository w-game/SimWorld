using UnityEngine.Events;

namespace Skill
{
    public class SkillBase
    {
        public int Level { get; set; }
        public event UnityAction<int> OnLevelUp;

        public void LevelUp()
        {
            Level++;
            OnLevelUp?.Invoke(Level);
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