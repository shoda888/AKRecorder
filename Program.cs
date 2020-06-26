using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using System;
using System.Data;
using System.Drawing;
using Image = Microsoft.Azure.Kinect.Sensor.Image;
using BitmapData = System.Drawing.Imaging.BitmapData;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using System.Threading.Tasks;

namespace Csharp_3d_viewer
{
    class Program
    {
        static async Task Main()
        {
            using (var visualizerData = new VisualizerData())
            {
                var renderer = new PosSaver(visualizerData);

                renderer.StartVisualizationThread();

                // Open device.
                using (Device device = Device.Open())
                {
                    device.StartCameras(new DeviceConfiguration()
                    {
                        ColorFormat = ImageFormat.ColorBGRA32,
                        ColorResolution = ColorResolution.R720p,
                        DepthMode = DepthMode.NFOV_Unbinned,
                        SynchronizedImagesOnly = true,
                        WiredSyncMode = WiredSyncMode.Standalone,
                        CameraFPS = FPS.FPS15
                    });

                    var deviceCalibration = device.GetCalibration();
                    var transformation = deviceCalibration.CreateTransformation();
                    PointCloud.ComputePointCloudCache(deviceCalibration);

                    using (Tracker tracker = Tracker.Create(deviceCalibration, new TrackerConfiguration() { ProcessingMode = TrackerProcessingMode.Gpu, SensorOrientation = SensorOrientation.Default }))
                    {
                        while (renderer.IsActive)
                        {
                            using (Capture sensorCapture = await Task.Run(() => device.GetCapture()).ConfigureAwait(true))
                            {
                                // Queue latest frame from the sensor.
                                tracker.EnqueueCapture(sensorCapture);
                                if (renderer.IsHuman)
                                {
                                    unsafe
                                    {
                                        //Depth画像の横幅(width)と縦幅(height)を取得
                                        int depth_width = device.GetCalibration().DepthCameraCalibration.ResolutionWidth;
                                        int depth_height = device.GetCalibration().DepthCameraCalibration.ResolutionHeight;
                                        // Bitmap depthBitmap = new Bitmap(depth_width, depth_height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                                        Bitmap colorBitmap = new Bitmap(depth_width, depth_height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                                        // Image depthImage = sensorCapture.Depth;
                                        Image colorImage = transformation.ColorImageToDepthCamera(sensorCapture);
                                        // ushort[] depthArray = depthImage.GetPixels<ushort>().ToArray();
                                        BGRA[] colorArray = colorImage.GetPixels<BGRA>().ToArray();
                                        // BitmapData bitmapData = depthBitmap.LockBits(new Rectangle(0, 0, depthBitmap.Width, depthBitmap.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                                        BitmapData bitmapData = colorBitmap.LockBits(new Rectangle(0, 0, colorBitmap.Width, colorBitmap.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                                        //各ピクセルの値へのポインタ
                                        byte* pixels = (byte*)bitmapData.Scan0;
                                        int index = 0;
                                        //一ピクセルずつ処理
                                        for (int i = 0; i < colorArray.Length; i++)
                                        {
                                            pixels[index++] = colorArray[i].B;
                                            pixels[index++] = colorArray[i].G;
                                            pixels[index++] = colorArray[i].R;
                                            pixels[index++] = 255;//Alpha値を固定して不透過に
                                        }
                                        //書き込み終了
                                        colorBitmap.UnlockBits(bitmapData);
                                        string string_now = renderer.now.ToString("HHmmssfff");
                                        colorBitmap.Save($@"C:\Users\gekka\temp\{renderer.day}\{renderer.scene}\depth\{string_now}.png", System.Drawing.Imaging.ImageFormat.Png);
                                    }
                                }
                            }

                            // Try getting latest tracker frame.
                            using (Frame frame = tracker.PopResult(TimeSpan.Zero, throwOnTimeout: false))
                            {
                                if (frame != null)
                                {
                                    // Save this frame for visualization in Renderer.

                                    // One can access frame data here and extract e.g. tracked bodies from it for the needed purpose.
                                    // Instead, for simplicity, we transfer the frame object to the rendering background thread.
                                    // This example shows that frame popped from tracker should be disposed. Since here it is used
                                    // in a different thread, we use Reference method to prolong the lifetime of the frame object.
                                    // For reference on how to read frame data, please take a look at Renderer.NativeWindow_Render().
                                    visualizerData.Frame = frame.Reference();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
