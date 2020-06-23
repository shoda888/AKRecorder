using Microsoft.Azure.Kinect.BodyTracking;
using OpenGL;
using OpenGL.CoreUI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Csharp_3d_viewer
{
    public class PosSaver
    {
        private readonly VisualizerData visualizerData;
        public PosSaver(VisualizerData visualizerData)
        {
            this.visualizerData = visualizerData;
        }

        public bool IsActive { get; private set; }

        public bool IsHuman { get; private set; } = false;

        public string day;
        public string scene;
        public string now;

        public void StartVisualizationThread()
        {
            Task.Run(() =>
            {
                using (NativeWindow nativeWindow = NativeWindow.Create())
                {
                    IsActive = true;
                    // nativeWindow.ContextCreated += NativeWindow_ContextCreated;
                    nativeWindow.Render += NativeWindow_Render;
                    nativeWindow.KeyDown += (object obj, NativeWindowKeyEventArgs e) =>
                    {
                        switch (e.Key)
                        {
                            case KeyCode.Escape:
                                nativeWindow.Stop();
                                IsActive = false;
                                break;

                            case KeyCode.F:
                                nativeWindow.Fullscreen = !nativeWindow.Fullscreen;
                                break;
                        }
                    };
                    nativeWindow.Animation = true;

                    nativeWindow.Create(0, 0, 640, 480, NativeWindowStyle.Overlapped);

                    nativeWindow.Show();
                    nativeWindow.Run();
                }
            });
        }


        public void NativeWindow_Render(object sender, NativeWindowEventArgs e)
        {
            using (var lastFrame = visualizerData.TakeFrameWithOwnership())
            {
                if (lastFrame == null)
                {
                    return;
                }

                if (lastFrame.NumberOfBodies > 0)
                {
                    if (!IsHuman)
                    {
                        this.day = DateTime.Now.ToString("yyyyMMdd");
                        this.scene = DateTime.Now.ToString("HHmmssfff");
                        string path = $@"C:\Users\gekka\temp\{this.day}\{this.scene}\depth";
                        Directory.CreateDirectory(path);
                        IsHuman = true;
                    }
                    for (uint i = 0; i < lastFrame.NumberOfBodies; ++i)
                    {
                        // System.Diagnostics.Debug.WriteLine(i);
                        DirectoryUtils.SafeCreateDirectory($@"C:\Users\gekka\temp\{this.day}\{this.scene}\{i}");
                        string filename = $@"C:\Users\gekka\temp\{this.day}\{this.scene}\{i}\pos.csv";
                        var append = true;
                        var skeleton = lastFrame.GetBodySkeleton(i);
                        var bodyId = lastFrame.GetBodyId(i);
                        var bodyColor = BodyColors.GetColorAsVector(bodyId);

                        using (var sw = new System.IO.StreamWriter(filename, append))
                        {
                            this.now = DateTime.Now.ToString("HHmmssfff");
                            sw.Write("{0}, ", this.now);
                            for (int jointId = 0; jointId < (int)JointId.Count; ++jointId)
                            {
                                var joint = skeleton.GetJoint(jointId);
                                sw.Write("{0}, {1}, {2},", joint.Position.X, joint.Position.Y, joint.Position.Z);
                            }
                            sw.Write("\r\n");
                        }

                    }
                }
                else
                {
                    IsHuman = false;
                }

            }
        }

        public static class DirectoryUtils
        {
            /// <summary>
            /// 指定したパスにディレクトリが存在しない場合
            /// すべてのディレクトリとサブディレクトリを作成します
            /// </summary>
            public static DirectoryInfo SafeCreateDirectory(string path)
            {
                if (Directory.Exists(path))
                {
                    return null;
                }
                return Directory.CreateDirectory(path);
            }
        }
    }
}
