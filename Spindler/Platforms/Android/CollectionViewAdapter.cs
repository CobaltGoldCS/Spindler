using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls.Handlers.Items;

namespace Spindler.Platforms.Android;

public class CollectionViewAdapter : ReorderableItemsViewAdapter<ReorderableItemsView, IGroupableItemsViewSource>
{
    private readonly List<DataTemplate> _dataTemplates = new();
    private const int _textItemViewType = 41; // Microsoft.Maui.Controls.Handlers.Items.ItemViewType.TextItem
    private const int _templatedItemViewType = 42; // Microsoft.Maui.Controls.Handlers.Items.ItemViewType.TemplatedItem

    public CollectionViewAdapter(ReorderableItemsView reorderableItemsView, Func<Microsoft.Maui.Controls.View, Context, ItemContentView>? createView = null) : base(reorderableItemsView, createView)
    {
    }

    public override int GetItemViewType(int position)
    {
        int type = base.GetItemViewType(position);

        if (type == _templatedItemViewType && ItemsView.ItemTemplate is DataTemplateSelector dataTemplateSelector)
        {
            DataTemplate dataTemplate = dataTemplateSelector.SelectTemplate(ItemsSource.GetItem(position), ItemsView);
            int index = _dataTemplates.IndexOf(dataTemplate);

            if (index == -1)
            {
                index = _dataTemplates.Count;
                _dataTemplates.Add(dataTemplate);
            }

            return index;
        }

        return type;
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        if (viewType >= _textItemViewType)
            return base.OnCreateViewHolder(parent, viewType);

        return new TemplatedItemViewHolder(
            new ItemContentView(parent.Context),
            _dataTemplates[viewType],
            ItemsView.SelectionMode is not SelectionMode.None
        );
    }

    public override void OnDetachedFromRecyclerView(RecyclerView recyclerView)
    {
        base.OnDetachedFromRecyclerView(recyclerView);
        _dataTemplates.Clear();
    }
}
