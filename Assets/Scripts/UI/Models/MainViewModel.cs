using UI.Views;

namespace UI.Models
{
    public class MainViewModel : ModelBase<MainView>
    {
        public override string Path => "MainView";

        public override ViewType ViewType => ViewType.View;
    }
}