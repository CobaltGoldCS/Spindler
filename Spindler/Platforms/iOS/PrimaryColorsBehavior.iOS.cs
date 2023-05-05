using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace Spindler.Platforms.iOS;

partial class PrimaryColorsBehavior : PlatformBehavior<Image, UIImageView>
{
    UIImageView? imageView;
    protected override void OnAttachedTo(Image bindable, UIImageView platformView)
    {
        imageView = platformView;
        bindable.PropertyChanged += ImageSourceChanged;
    }

    protected override void OnDetachedFrom(Image bindable, UIImageView platformView)
    {
        imageView = null;
        bindable.PropertyChanged -= ImageSourceChanged;
        base.OnDetachedFrom(bindable, platformView);
    }

    private void ImageSourceChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        IEnumerable<Color> values;
        byte[]? data = imageView!.Image?.CGImage?.DataProvider.CopyData()?.ToArray();
        if (data is null)
        {
            return;
        }

        Dictionary<int, int> colors = new();
        var size = data!.Length / sizeof(int);
        for (var index = 0; index < size; index++)
        {
            int color = BitConverter.ToInt32(data!.ToArray(), index * sizeof(int));
            // Default value is 0, so we don't need a check
            colors.TryGetValue(color, out int value);
            colors[color] = value + 1;
        }

        values = colors
                 .OrderByDescending((pair) => pair.Value)
                 .Select((pair) => Color.FromInt(pair.Key));
    }
}
