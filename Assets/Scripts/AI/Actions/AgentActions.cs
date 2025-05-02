using System;
using System.Collections.Generic;
using Citizens;
using UI.Models;
using UnityEngine;

namespace AI
{
    public class CheckInteractionAction : SingleActionBase
    {
        private Agent _targetAgent;
        private Type _action;
        public override void OnGet(params object[] args)
        {
            _targetAgent = args[0] as Agent;
            _action = args[1] as Type;
            ActionName = args[2] as string;
        }

        public override void OnRegister(Agent agent)
        {
            CheckMoveToArroundPos(agent, _targetAgent, () =>
            {
                if (_targetAgent.CheckInteraction(agent, _action))
                {
                    var action = ActionPool.Get(_action, agent, _targetAgent);
                    agent.Brain.StartInteraction(action);
                    _targetAgent.Brain.StartInteraction(action);
                }
                else
                {
                    _targetAgent.ShowConvarsation("Sorry, I'm busy right now.", () =>
                    {
                        _targetAgent.HideDialog();
                        Done = true;
                    });
                }
            });
        }

        protected override void DoExecute(Agent agent)
        {

        }
    }

    public class ChatAction : ConditionActionBase
    {
        private enum ChatState { AgentOneTurn, AgentTwoTurn, Speaking, Finished }

        private Agent _agentOne;
        private Agent _agentTwo;
        private ChatState _state;
        private float _timer;
        private float _delay;
        private int _maxTurns;
        private int _turnCount;

        public override void OnGet(params object[] args)
        {
            ActionName = "Chat";
            _agentOne = args[0] as Agent;
            _agentTwo = args[1] as Agent;

            _maxTurns = UnityEngine.Random.Range(3, 6);
            _turnCount = 0;
            _state = ChatState.AgentOneTurn;
            _timer = 0f;
            _delay = UnityEngine.Random.Range(1f, 3f);

            // Hide any existing dialogs
            _agentOne.HideDialog();
            _agentTwo.HideDialog();

            // Continue until state is Finished
            Condition = () => _state == ChatState.Finished;
        }

        public override void OnRegister(Agent agent)
        {
        }

        protected override void DoExecute(Agent agent)
        {
            if (_state == ChatState.Finished || _state == ChatState.Speaking)
                return;

            // If either agent stops this action, finish early
            if (_agentOne.Brain.CurAction != this || _agentTwo.Brain.CurAction != this)
            {
                EndChat();
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < _delay)
                return;

            // Determine whose turn it is
            Agent speaker = _state == ChatState.AgentOneTurn ? _agentOne : _agentTwo;
            Agent listener = speaker == _agentOne ? _agentTwo : _agentOne;

            var curState = _state;
            _state = ChatState.Speaking;
            // Show conversation line
            listener.HideDialog();
            speaker.ShowConvarsation($"{_state}'s turn to speak.", () =>
            {
                _turnCount++;
                if (_turnCount >= _maxTurns)
                {
                    EndChat();
                }
                else
                {
                    // Switch state for next turn
                    _state = (curState == ChatState.AgentOneTurn) ? ChatState.AgentTwoTurn : ChatState.AgentOneTurn;
                    _timer = 0f;
                    _delay = UnityEngine.Random.Range(1f, 3f);
                }
            });
        }

        private void EndChat()
        {
            _state = ChatState.Finished;
            _agentOne.HideDialog();
            _agentTwo.HideDialog();
        }
    }

    public class TradeAction : ConditionActionBase
    {
        private Agent _agentOne;
        private Agent _agentTwo;

        private List<PropItem> _soldItems = new List<PropItem>();

        private bool _end = false;

        public override void OnGet(params object[] args)
        {
            ActionName = "Trade";
            _agentOne = args[0] as Agent;
            _agentTwo = args[1] as Agent;

            _agentOne.HideDialog();
            _agentTwo.HideDialog();

            var model = IModel.GetModel<PopTradeModel>(this);
            model.ShowUI();
            _end = false;

            Condition = () => _end;
        }

        public void SellItem(PropItem item, int amount)
        {
            _agentOne.Bag.RemoveItem(item.Config, amount);
            _agentTwo.Bag.AddItem(item.Config, amount);
            var city = MapManager.I.GetCityByPos(_agentOne.Pos);
            var price = GameManager.I.PriceSystem.GetPrice(city, item.Config.id);
            _agentTwo.Money.Subtract(price);
            _agentOne.Money.Add(price);
            _soldItems.Add(item);
        }

        public void EndTrade()
        {
            _end = true;
            _agentOne.HideDialog();
            _agentTwo.HideDialog();

            var shopProperty = _agentTwo.Citizen.Job.Property as ShopProperty;
            foreach (var item in _soldItems)
            {
                var containerItem = shopProperty.ContainerItems.Find(x => x.Inventory.Items.Count < x.Inventory.MaxSize);
                if (containerItem == null)
                {
                    return;
                }
                _agentTwo.Brain.RegisterAction(ActionPool.Get<PutIntoContainer>(item, containerItem), false);
            }
        }

        public override void OnRegister(Agent agent)
        {
            
        }

        protected override void DoExecute(Agent agent)
        {

        }
    }
}