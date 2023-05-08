using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Widget;
using Spindler.Utilities;
using System.Diagnostics;
using Color = Microsoft.Maui.Graphics.Color;

namespace Spindler.Platforms.Android;

public partial class PrimaryColorsBehavior : PlatformBehavior<Image, ImageView>
{
    ImageView? imageView;
    readonly byte DOMINANT_COLOR_NUMBER = 7;
    protected override void OnAttachedTo(Image bindable, ImageView platformView)
    {
        imageView = platformView;
        Drawable? drawable = imageView?.Drawable;
        
        Task.Run(async () =>
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (drawable is null && timer.Elapsed <= TimeSpan.FromSeconds(10))
            {
                await Task.Delay(50);
                drawable = imageView?.Drawable;
            }
            timer.Stop();
            if (timer.Elapsed > TimeSpan.FromSeconds(10))
                return;
            var colorData = GetColorData(drawable);
            var colors = ColorUtilities.KMeansCluster(colorData, DOMINANT_COLOR_NUMBER);

            var dominantColors = new Color[DOMINANT_COLOR_NUMBER];

            for (int i = 0; i < DOMINANT_COLOR_NUMBER; i++)
            {
                dominantColors[i] = colors[i].OrderByDescending(pair => pair.Value).Select(pair => pair.Key).FirstOrDefault(Colors.Black);
            }
            var values = dominantColors.Where(color => color is not null && color.GetLuminosity() > .1).OrderByDescending(color => color.GetSaturation()).ToList();

            if (values.Count == 0)
                return;
            Color primaryColor = values.First().WithLuminosity((float)ColorUtilities.AverageLuminosity(colorData));
            Color secondaryColor = values.Take(2).Last().WithLuminosity((float)ColorUtilities.AverageLuminosity(colorData) * .75f);

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

    protected override void OnDetachedFrom(Image bindable, ImageView platformView)
    {
        imageView = null;
        base.OnDetachedFrom(bindable, platformView);
    }

    private Dictionary<Color, int> GetColorData(Drawable? drawable)
    {
        Bitmap bitmap = Bitmap.CreateBitmap(75, 75, Bitmap.Config.Argb8888!)!;
        Canvas canvas = new(bitmap);
        drawable!.SetBounds(0, 0, canvas.Width, canvas.Height);
        drawable.Draw(canvas);


        // TODO: Use K value clustering to pick out dominant colors
        // Exclude colors under a certain saturation level and brightness if possible
        Dictionary<int, int> colors = new();
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                // ARGB Color
                int color = bitmap.GetPixel(x, y);
                // Default value is 0, so we don't need a check
                colors.TryGetValue(color, out int value);
                colors[color] = value + 1;
            }

        }

        Dictionary<Color, int> values = new();
        foreach ((var key, var value) in colors)
        {
            Color colorKey = Color.FromInt(key);
            int instances = values.GetValueOrDefault(colorKey, 0);
            values[colorKey] = instances + value;
        }
        return values;
    }
  
}
