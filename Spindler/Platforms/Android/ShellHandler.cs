using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace Spindler.Platforms.Android
{
    public class ShellHandler : ShellRenderer
    {
        protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
        {
            return new CustomTabBarAppearanceTracker(this, shellItem.CurrentItem);
        }
    }
}
