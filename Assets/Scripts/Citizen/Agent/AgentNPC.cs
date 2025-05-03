using AI;
using Citizens;
using UnityEngine;

namespace GameItem
{
    public class AgentNPC : Agent
    {
        public AgentNPC(ConfigBase config, Vector3 pos, AIController brain, FamilyMember citizen) : base(config, pos, brain, citizen)
        {
        }

        public override void ShowUI()
        {
            base.ShowUI();

            UI.Col.enabled = false;
        }
    }
}