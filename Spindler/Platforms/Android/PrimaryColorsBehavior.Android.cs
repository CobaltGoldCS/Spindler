using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Widget;
using System.Diagnostics;
using Color = Microsoft.Maui.Graphics.Color;

namespace Spindler.Platforms.Android;

public partial class PrimaryColorsBehavior : PlatformBehavior<Image, ImageView>
{
    ImageView? imageView;

    readonly double THRESHOLD = 0.7d;
    protected override void OnAttachedTo(Image bindable, ImageView platformView)
    {
        imageView = platformView;
        Drawable drawable = imageView!.Drawable!;
        
        Task.Run(async () =>
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (drawable is null && timer.Elapsed <= TimeSpan.FromSeconds(10))
            {
                await Task.Delay(50);
                drawable = imageView!.Drawable!;
            }
            timer.Stop();
            if (timer.Elapsed > TimeSpan.FromSeconds(10))
                return;

            Bitmap bitmap = Bitmap.CreateBitmap(75, 75, Bitmap.Config.Argb8888!)!;
            Canvas canvas = new(bitmap);
            drawable!.SetBounds(0, 0, canvas.Width, canvas.Height);
            drawable.Draw(canvas);


            // TODO: Use K value clustering to pick out dominant colors
            // Exclude colors under a certain saturation level
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
            IEnumerable<Color> values = colors.Select((pair) => Color.FromInt(pair.Key)).OrderByDescending(c => c.GetSaturation() * 2 + c.GetLuminosity());
                                         
            
            if (colors.Count < 2)
                return;

            Color primaryColor = Color.FromHsv(values.ElementAt(0).GetHue(), (float)Math.Clamp(values.ElementAt(0).GetSaturation() + .15, .2, 1.0d), (float)(values.ElementAt(0).GetLuminosity() * .5));
            Color secondaryColor = values.ElementAt(1);

            foreach (Color color in values.Skip(1).Where(c => c.GetSaturation() > 0.6 && c.GetLuminosity() > 0.4))
            {
                secondaryColor = color;

                // Euclidian distance determines how close the colors are mathematically
                var euclidianDist = Math.Sqrt(
                    Math.Pow((secondaryColor.Red - primaryColor.Red), 2) +
                    Math.Pow((secondaryColor.Blue - primaryColor.Blue), 2) +
                    Math.Pow((secondaryColor.Green - primaryColor.Green), 2)
                    );
                // Determine if two colors are sufficiently different
                if (euclidianDist > THRESHOLD)
                    break;
            }

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
  
}
