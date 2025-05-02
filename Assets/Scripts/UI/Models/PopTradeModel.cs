using AI;
using UI.Popups;

namespace UI.Models
{
    public class PopTradeModel : ModelBase<PopTrade>
    {
        public override string Path => "PopTrade";
        public override ViewType ViewType => ViewType.Popup;

        public TradeAction TradeAction => Data[0] as TradeAction;
    }
}