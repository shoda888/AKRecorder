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
        // GUI描画する場合
        // private SphereRenderer SphereRenderer;
        // private CylinderRenderer CylinderRenderer;
        // private PointCloudRenderer PointCloudRenderer;
        // private List<Vertex> pointCloud = null;

        private readonly VisualizerData visualizerData;
        public PosSaver(VisualizerData visualizerData)
        {
            this.visualizerData = visualizerData;
        }

        public bool IsActive { get; private set; }

        public bool IsHuman { get; private set; } = false;

        public string day;
        public string scene;

        public static string path = @"C:\Users\gekka\temp"; // YOU NEED CHANGING SAVING PATH

        public DateTime now = DateTime.Now;

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

        private void NativeWindow_ContextCreated(object sender, NativeWindowEventArgs e)
        {
            Gl.ReadBuffer(ReadBufferMode.Back);

            Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Gl.LineWidth(2.5f);

            CreateResources();
        }

        private static float ToRadians(float degrees)
        {
            return degrees / 180.0f * (float)Math.PI;
        }

        public void NativeWindow_Render(object sender, NativeWindowEventArgs e)
        {
            using (var lastFrame = visualizerData.TakeFrameWithOwnership())
            {
                if (lastFrame == null)
                {
                    return;
                }
                // NativeWindow nativeWindow = (NativeWindow)sender;

                // GUI描画する場合
                // Gl.Viewport(0, 0, (int)nativeWindow.Width, (int)nativeWindow.Height);
                // Gl.Clear(ClearBufferMask.ColorBufferBit);

                // var proj = Matrix4x4.CreatePerspectiveFieldOfView(ToRadians(65.0f), (float)nativeWindow.Width / nativeWindow.Height, 0.1f, 150.0f);
                // var view = Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitZ, -Vector3.UnitY);

                if (lastFrame.NumberOfBodies > 0)
                {
                    // GUI描画する場合
                    // SphereRenderer.View = view;
                    // SphereRenderer.Projection = proj;

                    // CylinderRenderer.View = view;
                    // CylinderRenderer.Projection = proj;

                    // PointCloudRenderer.View = view;
                    // PointCloudRenderer.Projection = proj;

                    // PointCloud.ComputePointCloud(lastFrame.Capture.Depth, ref pointCloud);
                    // PointCloudRenderer.Render(pointCloud, new Vector4(1, 1, 1, 1));
                    
                    if (!IsHuman)
                    {
                        DateTime tmp = DateTime.Now;
                        TimeSpan baseInterval = new TimeSpan(0, 0, 10);
                        // Console.WriteLine(TimeSpan.Compare(baseInterval, tmp - now));
                        if(TimeSpan.Compare(baseInterval, tmp - now) == -1) //別シーンの動作と認識した場合のみ違うディレクトリを生成する
                        {
                            now = DateTime.Now;
                            day = now.ToString("yyyyMMdd");
                            scene = now.ToString("HHmmssfff");
                            string depth_path = $@"{path}\{day}\{scene}\depth";
                            Directory.CreateDirectory(depth_path);
                        }
                        IsHuman = true;
                    }
                    for (uint i = 0; i < lastFrame.NumberOfBodies; ++i)
                    {
                        DirectoryUtils.SafeCreateDirectory($@"{path}\{day}\{scene}\{i}");
                        string filename = $@"{path}\{day}\{scene}\{i}\pos.csv";
                        var append = true;
                        var skeleton = lastFrame.GetBodySkeleton(i);
                        var bodyId = lastFrame.GetBodyId(i);
                        var bodyColor = BodyColors.GetColorAsVector(bodyId);

                        using (var sw = new System.IO.StreamWriter(filename, append))
                        {
                            now = DateTime.Now;
                            string string_now = now.ToString("HHmmssfff");
                            sw.Write("{0}, ", string_now);
                            for (int jointId = 0; jointId < (int)JointId.Count; ++jointId)
                            {
                                var joint = skeleton.GetJoint(jointId);
                                sw.Write("{0}, {1}, {2},", joint.Position.X, joint.Position.Y, joint.Position.Z);
                                
                                // GUI描画する場合
                                // const float radius = 0.024f;
                                // SphereRenderer.Render(joint.Position / 1000, radius, bodyColor);

                                // if (JointConnections.JointParent.TryGetValue((JointId)jointId, out JointId parentId))
                                // {
                                //      // Render a bone connecting this joint and its parent as a cylinder.
                                //      CylinderRenderer.Render(joint.Position / 1000, skeleton.GetJoint((int)parentId).Position / 1000, bodyColor);
                                // }
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

        private void CreateResources()
        {
            // GUI描画する場合
            // SphereRenderer = new SphereRenderer();
            // CylinderRenderer = new CylinderRenderer();
            // PointCloudRenderer = new PointCloudRenderer();
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
