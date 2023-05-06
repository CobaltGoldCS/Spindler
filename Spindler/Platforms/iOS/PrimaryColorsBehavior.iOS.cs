using CarPlay;
using CoreGraphics;
using Intents;
using Microsoft.Maui.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace Spindler.Platforms.iOS;

partial class PrimaryColorsBehavior : PlatformBehavior<Image, UIImageView>
{
    UIImageView? imageView;
    readonly byte DOMINANT_COLOR_NUMBER = 3;
    protected override void OnAttachedTo(Image bindable, UIImageView platformView)
    {
        imageView = platformView;
        CGDataProvider? dataProvider = imageView!.Image?.CGImage?.DataProvider;
        Task.Run(async () =>
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (dataProvider is null && timer.Elapsed <= TimeSpan.FromSeconds(10))
            {
                await Task.Delay(50);
                dataProvider = imageView!.Image?.CGImage?.DataProvider;
            }
            timer.Stop();
            if (timer.Elapsed > TimeSpan.FromSeconds(10))
                return;

            // Get colors in dictionary with the color as a key and the number of instances as a value
            var colorDictionary = GetColorData();
            if (colorDictionary is null)
                return;

            // Cluster colors in order to get dominant colors
            var clusters = Utilities.ColorUtilities.KMeansCluster(colorDictionary, DOMINANT_COLOR_NUMBER);

            if (clusters is null)
                return;

            var dominantColors = new Color[DOMINANT_COLOR_NUMBER];
            for (int i = 0; i < DOMINANT_COLOR_NUMBER; i++)
            {
                dominantColors[i] = clusters[i].OrderByDescending(pair => pair.Value).Select(pair => pair.Key).First();
            }
            var values = dominantColors.Where(color => color is not null).OrderByDescending(color => color.GetLuminosity() + color.GetSaturation() * 2);

        if (values.Count() < 2)
                return;

            // Darken primary color 
            Color primaryColor = Color.FromHsv(
                values.ElementAt(0).GetHue(),
                (float)Math.Clamp(values.ElementAt(0).GetSaturation() + .15, .2, 1.0d),
                (float)(values.ElementAt(0).GetLuminosity() * .5));

            Color secondaryColor = values.ElementAt(1);
            GradientStopCollection gradientStops = new()
            {
                new GradientStop(primaryColor, 0.0f),
                new GradientStop(secondaryColor, 1.0f)
            };

            Dispatcher.Dispatch(() =>
            {
                bindable.Background = new LinearGradientBrush(gradientStops, new(0.0, 0.0), new(1.0, 1.0));
                bindable.Source = null;
            });
        });
    }

    protected override void OnDetachedFrom(Image bindable, UIImageView platformView)
    {
        imageView = null;
        base.OnDetachedFrom(bindable, platformView);
    }
    

    private Dictionary<Color, int>? GetColorData()
    {
        IEnumerable<Color> values;
        byte[]? data = imageView!.Image?.CGImage?.DataProvider.CopyData()?.ToArray();
        if (data is null)
        {
            return null;
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

        return colors
            .Select(pair => new KeyValuePair<Color, int>(Color.FromInt(pair.Key), pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
