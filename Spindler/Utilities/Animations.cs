using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Utilities;

class RotationAnimation : BaseAnimation
{
    public async override Task Animate(VisualElement view, CancellationToken token = default)
    {
        await view.RelRotateTo(360, 500, easing: Easing.CubicIn);
    }
}