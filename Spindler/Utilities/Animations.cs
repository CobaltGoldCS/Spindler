using CommunityToolkit.Maui.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Utilities;

class RotationAnimation : BaseAnimation
{
    public async override Task Animate(VisualElement view)
    {
        await view.RelRotateTo(360, easing: Easing.CubicIn);
    }
}