using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Renderscripts;
using Android.Widget;
using System.Diagnostics;


using AndroidElement = Android.Renderscripts.Element;
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

    async void SetRendererEffect(ImageView imageView, float radius)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var renderEffect = radius > 0 ? GetEffect(radius) : null;
            imageView.SetRenderEffect(renderEffect);
        }
        else if (OperatingSystem.IsAndroidVersionAtLeast(21))
        {
            Radius = 20 - Radius;
            
            Drawable? drawable = null;
            Stopwatch timer = new();
            timer.Start();
            while (drawable is null && timer.Elapsed <= TimeSpan.FromSeconds(10))
            {
                await Task.Delay(50);
                drawable = imageView.Drawable;
            }
            timer.Stop();

            if (timer.Elapsed > TimeSpan.FromSeconds(10))
                return;

            Bitmap bitmap = Bitmap.CreateBitmap(drawable!.IntrinsicWidth / 5, drawable.IntrinsicHeight / 5, Bitmap.Config.Argb8888!)!;
            Canvas canvas = new(bitmap);
            drawable!.SetBounds(0, 0, canvas.Width, canvas.Height);
            drawable.Draw(canvas);

            imageView.SetImageBitmap(blurRenderScript(Platform.CurrentActivity!, bitmap, (int)Radius));
        }
    }

    static RenderEffect? GetEffect(float radius)
    {
        return OperatingSystem.IsAndroidVersionAtLeast(31) ?
            RenderEffect.CreateBlurEffect(radius, radius, Shader.TileMode.Decal!) :
            null;
    }

    // https://stackoverflow.com/questions/27831617/how-to-blur-imageview-in-android
    public static Bitmap blurRenderScript(Context context, Bitmap smallBitmap, int radius)
    {
        try
        {
            smallBitmap = RGB565toARGB888(smallBitmap);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        Bitmap bitmap = Bitmap.CreateBitmap(
                smallBitmap.Width, smallBitmap.Height,
                Bitmap.Config.Argb8888!)!;

#pragma warning disable CA1422 // API 21
        RenderScript renderScript = RenderScript.Create(context)!;

        Allocation blurInput = Allocation.CreateFromBitmap(renderScript, smallBitmap)!;
        Allocation blurOutput = Allocation.CreateFromBitmap(renderScript, bitmap)!;

        ScriptIntrinsicBlur blur = ScriptIntrinsicBlur.Create(renderScript,
                AndroidElement.U8_4(renderScript))!;
        blur.SetInput(blurInput);
        blur.SetRadius(radius); // radius must be 0 < r <= 25
        blur.ForEach(blurOutput);

        blurOutput.CopyTo(bitmap);
        renderScript.Destroy();

#pragma warning restore CA1422 // API 21
        return bitmap;
    }

    private static Bitmap RGB565toARGB888(Bitmap img)
    {
        int numPixels = img.Width * img.Height;
        int[] pixels = new int[numPixels];

        img.GetPixels(pixels, 0, img.Width, 0, 0, img.Width, img.Height);

        Bitmap result = Bitmap.CreateBitmap(img.Width, img.Height, Bitmap.Config.Argb8888!)!;

        result.SetPixels(pixels, 0, result.Width, 0, 0, result.Width, result.Height);
        return result;
    }
}