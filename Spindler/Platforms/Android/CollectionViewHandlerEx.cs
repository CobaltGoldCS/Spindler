using Microsoft.Maui.Controls.Handlers.Items;

namespace Spindler.Platforms.Android;


public class CollectionViewHandlerEx : CollectionViewHandler
{
    protected override ReorderableItemsViewAdapter<ReorderableItemsView, IGroupableItemsViewSource> CreateAdapter()
    {
        return new CollectionViewAdapter(VirtualView);
    }
}
