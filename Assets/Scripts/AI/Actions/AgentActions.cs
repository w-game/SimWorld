using System;
using System.Collections.Generic;
using System.Linq;
using Citizens;
using GameItem;
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
                    var dialogData = new DialogData
                    {
                        Content = "Sorry, I can't do that right now.",
                        Callback = () =>
                        {
                            _targetAgent.HideDialog();
                            Done = true;
                        }
                    };
                    _targetAgent.ShowConversation(dialogData);
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
            var dialogData = new DialogData
            {
                Content = $"{_state} says something.",
                Callback = () =>
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
                },
            };
            speaker.ShowConversation(dialogData);
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

            var model = IModel.GetModel<PopTradeModel>();
            model.ShowUI(this);
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
    
    public class RentPropertyAction : ConditionActionBase
    {
        private Agent _agentOne;
        private Agent _agentTwo;

        private bool _end = false;

        private int _step = 0;

        private DialogOption _endOption;

        private List<Property> _propertiesForRent = new List<Property>();

        private Property _selectedProperty;

        public override void OnGet(params object[] args)
        {
            ActionName = "RentProperty";
            _agentOne = args[0] as Agent;
            _agentTwo = args[1] as Agent;

            _agentOne.HideDialog();
            _agentTwo.HideDialog();

            // var model = IModel.GetModel<PopRentPropertyModel>();
            // model.ShowUI(this);
            _end = false;

            Condition = () => _end;

            _endOption = new DialogOption
            {
                Text = "我考虑考虑",
                OnClick = () =>
                {
                    EndRent();
                }
            };
        }

        private void EndRent()
        {
            _end = true;
            _agentOne.HideDialog();
            _agentTwo.HideDialog();
        }

        private void Rent()
        {
            _end = true;
            _agentOne.HideDialog();
            _agentTwo.HideDialog();
            _selectedProperty.LeaseTo(_agentOne.Citizen.Family);
        }

        public override void OnRegister(Agent agent)
        {
            var role = _agentTwo.Citizen.Job as Owner;
            if (role != null)
            {
                var properties = role.Properties.OfType<FarmProperty>();
                _propertiesForRent = properties.Where(x => x.ForRent).Cast<Property>().ToList();
            }
        }

        protected override void DoExecute(Agent agent)
        {
            switch (_step)
            {
                case 0:
                    DialogData dialogData = null;
                    if (_selectedProperty == null)
                    {
                        List<string> sizes = _propertiesForRent.Select(x => (x.House.Size.x * x.House.Size.y).ToString()).ToList();
                        dialogData = new DialogData
                        {
                            Content = $"我这有{_propertiesForRent.Count}块地，你问的是哪一块？\n" +
                                        "分别有" +
                                        string.Join(",", sizes) + "平方米大小的",
                        };

                        for (int i = 0; i < sizes.Count; i++)
                        {
                            var size = sizes[i];
                            var idx = i;
                            dialogData.Options.Add(new DialogOption
                            {
                                Text = $"{i + 1}号地（{size}平方米）",
                                OnClick = () =>
                                {
                                    _selectedProperty = _propertiesForRent[idx];
                                    _step = 0;
                                }
                            });
                        }
                    }
                    else
                    {
                        dialogData = new DialogData
                        {
                            Content = "这块地\n" +
                                    "一周只要<color=#ff0000>10文钱每平方米</color>，\n" +
                                    "怎么样？",
                            Options = new List<DialogOption>
                            {
                                new DialogOption{
                                    Text = "成交！",
                                    OnClick = () =>
                                    {
                                        _step = 1;
                                    }
                                },
                                new DialogOption{
                                    Text = "能便宜点吗？",
                                    OnClick = () => {
                                        _step = 2;
                                    }
                                },
                                new DialogOption{
                                    Text = "我想看看位置。",
                                    OnClick = () =>
                                    {
                                        _step = 3;
                                    }
                                },
                                _endOption
                            }
                        };
                    }
                    _agentTwo.ShowConversation(dialogData);
                    _step = -1;
                    break;
                case 1:
                    var dialogData1 = new DialogData
                    {
                        Content = "那先付这周的租金吧\n",
                        Options = new List<DialogOption>
                        {
                            new DialogOption{
                                Text = "给钱（10文钱）",
                                OnClick = () =>
                                {
                                    Rent();
                                }
                            },
                            _endOption
                        }
                    };
                    _agentTwo.ShowConversation(dialogData1);
                    _step = -1;
                    break;
                case 2:
                    var dialogData2 = new DialogData
                    {
                        Content = "我可以给你<color=#ff0000>8文钱</color>，\n" +
                                  "但是你要帮我做点事情。",
                        Options = new List<DialogOption>
                        {
                            new DialogOption{
                                Text = "好的，我愿意。",
                                OnClick = () =>
                                {
                                    Rent();
                                }
                            },
                            _endOption
                        }
                    };
                    _agentTwo.ShowConversation(dialogData2);
                    _step = -1;
                    break;
                case 3:
                    var dialogData3 = new DialogData
                    {
                        Content = "我带你过去吧，跟着我。"
                    };
                    _agentTwo.ShowConversation(dialogData3);
                    _step = -1;
                    break;
            }
        }
    }
}