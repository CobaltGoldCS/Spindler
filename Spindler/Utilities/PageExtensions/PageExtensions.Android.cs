using Google.Android.Material.BottomSheet;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Utilities;

// https://vladislavantonyuk.github.io/articles/Creating-a-bottom-sheet-using-.NET-MAUI/
public static partial class PageExtensions
{
    public static void ShowBottomSheet(this Page page, IView bottomSheetContent, bool dimDismiss)
    {
#nullable disable
        var bottomSheetDialog = new BottomSheetDialog(Platform.CurrentActivity?.Window?.DecorView.FindViewById(Android.Resource.Id.Content)?.RootView?.Context);
#nullable enable
        bottomSheetDialog.SetContentView(bottomSheetContent.ToPlatform(page.Handler?.MauiContext ?? throw new Exception("MauiContext is null")));
        bottomSheetDialog.Behavior.Hideable = dimDismiss;
        bottomSheetDialog.Behavior.FitToContents = true;
        bottomSheetDialog.Show();
    }
}
