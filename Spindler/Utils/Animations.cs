using CommunityToolkit.Maui.Animations;

namespace Spindler.Utilities;

class RotationAnimation : BaseAnimation
{
    public async override Task Animate(VisualElement view, CancellationToken token = default)
    {
        await view.RelRotateTo(360, 100, easing: Easing.CubicIn);
    }
}