﻿using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls.Handlers.Items;

namespace Spindler.Platforms.Android;

public class CustomCollectionViewHandler : CollectionViewHandler
{
    private IItemsLayout? ItemsLayout { get; set; }

    protected override IItemsLayout GetItemsLayout()
    {
        //return base.GetItemsLayout(); //Throws exception
        // This is a workaround for 'virtualview is not defined' errors

        return ItemsLayout!;
    }

    protected override void ConnectHandler(RecyclerView platformView)
    {
        base.ConnectHandler(platformView);

        ItemsLayout = VirtualView.ItemsLayout;
    }
}
