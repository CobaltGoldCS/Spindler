using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Internal;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;

namespace Spindler.Platforms.Android;

class CustomTabBarAppearanceTracker : ShellBottomNavViewAppearanceTracker
{
    private readonly IShellContext shellContext;

    public CustomTabBarAppearanceTracker(IShellContext shellContext, ShellItem shellItem) : base(shellContext, shellItem)
    {
        this.shellContext = shellContext;
    }

    public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
    {
        base.SetAppearance(bottomView, appearance);
        var backgroundDrawable = new GradientDrawable();
        backgroundDrawable.SetShape(ShapeType.Rectangle);
        backgroundDrawable.SetCornerRadius(30);
        backgroundDrawable.SetColor(appearance.EffectiveTabBarBackgroundColor.ToPlatform());
        bottomView.SetBackground(backgroundDrawable);

        var layoutParams = bottomView.LayoutParameters;
        if (layoutParams is ViewGroup.MarginLayoutParams marginLayoutParams)
        {
            var margin = 30;
            marginLayoutParams.BottomMargin = margin / 2;
            marginLayoutParams.LeftMargin = margin;
            marginLayoutParams.RightMargin = margin;
            bottomView.LayoutParameters = layoutParams;
        }

        // Update Tab Size
        bottomView.ItemIconSize = 80;
        bottomView.ItemPaddingTop = 28;
    }

    protected override void SetBackgroundColor(BottomNavigationView bottomView, Color color)
    {
        base.SetBackgroundColor(bottomView, color);
        bottomView.RootView?.SetBackgroundColor(color.ToPlatform());
    }
}
