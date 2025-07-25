using CoreGraphics;
using CoreImage;
using UIKit;

namespace Spindler.Behaviors;
public partial class BlurBehavior : PlatformBehavior<Image, UIImageView>
{
    private CGImage? originalImage;
    UIImageView? imageView;

    protected override void OnAttachedTo(Image bindable, UIImageView platformView)
    {
        imageView = platformView;

        originalImage = platformView.Image?.CGImage;
        SetRendererEffect(imageView, Radius);
    }

    protected override void OnDetachedFrom(Image bindable, UIImageView platformView)
    {
        SetImage(platformView, originalImage);
    }

    static void SetImage(UIImageView imageView, CGImage? image)
    {
        if (image is null)
        {
            return;
        }

        imageView.Image = new UIImage(image);
    }

    void SetRendererEffect(UIImageView imageView, float radius)
    {
        if (originalImage is null)
        {
            return;
        }

        var myContext = CIContext.Create();
        var inputImage = new CIImage(originalImage);
        var filter = new CIGaussianBlur
        {
            InputImage = inputImage,
            Radius = radius
        };
        var resultImage = myContext.CreateCGImage(filter.OutputImage!, inputImage.Extent);
        SetImage(imageView, resultImage);
    }
}