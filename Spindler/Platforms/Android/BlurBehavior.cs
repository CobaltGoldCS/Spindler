﻿using Android.Graphics;
using Android.Widget;

namespace Spindler.Behaviors;

public partial class BlurBehavior : PlatformBehavior<Image, ImageView>
{
    ImageView? imageView;
    protected override void OnAttachedTo(Image bindable, ImageView platformView)
    {
        imageView = platformView;
        SetRendererEffect(platformView, Radius);
    }

    protected override void OnDetachedFrom(Image bindable, ImageView platformView)
    {
        SetRendererEffect(platformView, 0);
    }

    void SetRendererEffect(ImageView imageView, float radius)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var renderEffect = radius > 0 ? GetEffect(radius) : null;
            imageView.SetRenderEffect(renderEffect);
        }
    }

    static RenderEffect? GetEffect(float radius)
    {
        return OperatingSystem.IsAndroidVersionAtLeast(31) ?
            RenderEffect.CreateBlurEffect(radius, radius, Shader.TileMode.Decal!) :
            null;
    }
}
