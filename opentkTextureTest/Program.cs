using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using System.Diagnostics;

namespace StarterKit
{
    class Game : GameWindow
    {
        MyCamera camera = new MyCamera();

        Shader renderShader;
        Shader photonShader;

        FrameBuffer frameBuffer;

        private uint allocationTexture;
        
        static int w = 800;
        static int h = 800;

        private float angle;

        int mapWidth = 80;
        int mapHeight = 80;

        public Game()
            : base(w, h, OpenTK.Graphics.GraphicsMode.Default, "OpenTK Quick Start Sample")
        {
            VSync = VSyncMode.On;
        }
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            GL.ClearColor(0.0f, 0.4f, 0.0f, 0.0f);
            GL.Enable(EnableCap.Texture2D);
            
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-1, 1, -1, 1, -1, 1);
            
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Viewport(0, 0, w, h);

            frameBuffer = new FrameBuffer(mapWidth,mapHeight);

            angle = 0.0f;

            renderShader = new Shader("..\\..\\vertexRender.glsl", "..\\..\\fragmentRender.glsl", "");
            photonShader = new Shader("..\\..\\vertexRender.glsl", "..\\..\\fragmentRender.glsl", "#define PHOTON_MAP");

            Allocation();

            PhotonMappingUniformSet();
            RayTracingUniformSet();

            

        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            if (Keyboard[Key.Escape])
                Exit();
            if (Keyboard[Key.Down]) camera.MoveBack();
            if (Keyboard[Key.Up]) camera.MoveForward();
            if (Keyboard[Key.S]) camera.MoveDown();
            if (Keyboard[Key.W]) camera.MoveUp();
            if (Keyboard[Key.A] || Keyboard[Key.Left]) camera.MoveLeft();
            if (Keyboard[Key.D] || Keyboard[Key.Right]) camera.MoveRight();
            if (Keyboard[Key.Keypad6]) camera.RotateAroundUp(-0.05f);
            if (Keyboard[Key.Keypad4]) camera.RotateAroundUp(0.05f);
            if (Keyboard[Key.Keypad8]) camera.RotateAroundRight(0.05f);
            if (Keyboard[Key.Keypad2]) camera.RotateAroundRight(-0.05f);
            if (Keyboard[Key.Keypad9]) camera.RotateAroundView(0.05f);
            if (Keyboard[Key.Keypad7]) camera.RotateAroundView(-0.05f);
            if (Keyboard[Key.Plus]) camera.ScalePlus();
            if (Keyboard[Key.Minus]) camera.ScaleMinus();
            if (Keyboard[Key.Q]) Console.WriteLine(camera.GetCoords());
        }

        private void Allocation()
        {
                        float[] allocation = new float[mapWidth * mapHeight * 3];
            //GL.GetTexImage(TextureTarget.TextureRectangleArb, 0, PixelFormat.Rgb, PixelType.Float, pix);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var rnd = new Random();

            for (int i = 0; i < 80; i++)
            {
                for (int j = 0; j < 80; j++)
                {
                    
                    long t = sw.ElapsedMilliseconds;
                    //double t = DateTime.Now.Millisecond;

                    
                    
                    allocation[(i * 80 + j) * 3] =(float) rnd.NextDouble()*2-1;
                    allocation[(i * 80 + j) * 3 + 1] = (float)rnd.NextDouble()*2-1;
                    allocation[(i * 80 + j) * 3 + 2] = (float)rnd.NextDouble()*2-1;
                }
            }
            
            GL.GenTextures(1, out allocationTexture);
            GL.BindTexture(TextureTarget.TextureRectangleArb, allocationTexture);
            GL.TexImage2D(TextureTarget.TextureRectangleArb, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb,
                         PixelType.Float, allocation);
            
            float[] fpix = new float[80 * 80 * 3];
            GL.GetTexImage(TextureTarget.TextureRectangleArb, 0, PixelFormat.Rgb, PixelType.Float, fpix);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //angle += 0.1f;

            

            frameBuffer.Activate();
            PhotonMapping();
            frameBuffer.Deactivate();

            GL.BindTexture(TextureTarget.TextureRectangleArb, frameBuffer.GetTexture());

            float[] pix = new float[mapWidth * mapHeight * 3];
            GL.GetTexImage(TextureTarget.TextureRectangleArb, 0, PixelFormat.Rgb, PixelType.Float, pix);

            PhotonMapSort();

            renderShader.SetUniformTexture(frameBuffer.GetTexture(), TextureUnit.Texture2, "PhotonTexture");
            RayTracing();
            
            SwapBuffers();
        }

        private void PhotonMappingUniformSet()
        {
            photonShader.Activate();
                photonShader.SetUniform("BoxMinimum", new Vector3(-5.0F, -5.0F, -5.0F));
                photonShader.SetUniform("BoxMaximum", new Vector3(5.0F, 5.0F, 5.0F));
                photonShader.SetUniform("GlassSphere.Center", new Vector3(2.0F, -3.0F, -3.0F));
                photonShader.SetUniform("GlassSphere.Radius", 2.0F);
                photonShader.SetUniform("MatSphere.Center", new Vector3(-3.0F, -4.0F, 1.0F));
                photonShader.SetUniform("MatSphere.Radius", 1.0F);
                photonShader.SetUniform("Light.Position", new Vector3(0.0F/* + (float)Math.Sin(angle)*/, 4.0F, 0.0F/* + (float)Math.Cos(angle)*/));
                photonShader.SetUniform("Light.Radius", new Vector2(0.5F * 10, 0.5F * 10));
                photonShader.SetUniform("Light.Distance", 0.5F * 10);
            photonShader.Deactivate();
        }

        private void RayTracingUniformSet()
        {
            renderShader.Activate();
                renderShader.SetUniform("BoxMinimum", new Vector3(-5.0F, -5.0F, -5.0F));
                renderShader.SetUniform("BoxMaximum", new Vector3(5.0F, 5.0F, 5.0F));
                renderShader.SetUniform("GlassSphere.Center", new Vector3(2.0F, -3.0F, -3.0F));
                renderShader.SetUniform("GlassSphere.Radius", 2.0F);
                renderShader.SetUniform("MatSphere.Center", new Vector3(-3.0F, -4.0F, 1.0F));
                renderShader.SetUniform("MatSphere.Radius", 1.0F);
                renderShader.SetUniform("Light.Position", new Vector3(0.0F/* + (float)Math.Sin(angle)*/, 4.0F, 0.0F/* + (float)Math.Cos(angle)*/));

                renderShader.SetUniform("Delta", 0.8F);
                renderShader.SetUniform("InverseDelta", 1.0F / 0.8F);
                renderShader.SetUniform("PhotonMapSize", new Vector2(mapWidth, mapHeight));
                renderShader.SetUniform("PhotonIntensity", 100.0F / (mapWidth * mapHeight));

                renderShader.SetUniform("Camera.Position", camera.GetPosition());
                renderShader.SetUniform("Camera.View", camera.GetView());
                renderShader.SetUniform("Camera.Side", camera.GetRight());
                renderShader.SetUniform("Camera.Up", camera.GetUp());
                renderShader.SetUniform("Camera.Scale", camera.GetScale());

                renderShader.SetUniform("RectangleLight.Center", new Vector2(0.0F, 0.0F));
                renderShader.SetUniform("RectangleLight.Color", new Vector3(1.0F, 0.0F, 0.0F));
                renderShader.SetUniform("RectangleLight.Length", 4.0F);
                renderShader.SetUniform("RectangleLight.Width", 4.0F);
            renderShader.Deactivate();
        }

        private void RayTracing()
        {
            GL.Viewport(0,0,w,h);
            
            renderShader.Activate();
                GL.Color3(Color.Red);
                GL.Begin(BeginMode.Quads);
                GL.Vertex2(-400, -400);
                GL.Vertex2(400, -400);
                GL.Vertex2(400, 400);
                GL.Vertex2(-400, 400);
                GL.End();
            renderShader.Deactivate();
        }

        private void PhotonMapping()
        {
            GL.Viewport(0, 0, mapWidth, mapHeight);

            photonShader.Activate();
            photonShader.SetUniformTexture(allocationTexture, TextureUnit.Texture0, "AllocationTexture");
                GL.Begin(BeginMode.Quads);
                GL.Vertex2(-40, -40);
                GL.Vertex2(40, -40);
                GL.Vertex2(40, 40);
                GL.Vertex2(-40, 40);
                GL.End();
            photonShader.Deactivate();
        }

        private void PhotonMapSort()
        {
            GL.BindTexture(TextureTarget.TextureRectangleArb, frameBuffer.GetTexture());

            float[] pix = new float[mapWidth * mapHeight * 3];
            GL.GetTexImage(TextureTarget.TextureRectangleArb, 0, PixelFormat.Rgb, PixelType.Float, pix);

            Vec3List list = new Vec3List(pix);
            list.Sort();
            float[] pixSorted = list.ToFloatArray();

            GL.TexImage2D(TextureTarget.TextureRectangleArb, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb,
                          PixelType.Float, pixSorted);
        }
        

        [STAThread]
        static void Main()
        {
            using (Game game = new Game())
            {
                game.Run(30.0);
            }
        }
    }
}