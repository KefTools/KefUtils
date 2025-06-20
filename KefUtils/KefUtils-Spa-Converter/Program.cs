using KefUtils.Images;
using System.Drawing;
using System.Drawing.Imaging;
class Program {
    public static void Main(string[] args) {
        SpaImage image = new SpaImage(File.ReadAllBytes("test.spa"));

        Console.WriteLine("Image size is {0}x{1}", image.FrameHeaders[0].Width, image.FrameHeaders[0].Height);
        Console.WriteLine("Image version is {0}", image.Header.Version);
        Console.WriteLine("Image contains {0} frames with an FPS of {1}", image.Header.NumFrames, image.Header.FramesPerSecond);

        for (int i = 0; i < image.Header.NumFrames; i++) {
            SpaConverter.SpaToBitmap(image, i).Save($"test_{i}.png", ImageFormat.Png);
        }
    }
}