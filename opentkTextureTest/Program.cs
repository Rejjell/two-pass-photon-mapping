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

        float PhotonIntensity = 100.0F;

        public Game()
            : base(w, h, OpenTK.Graphics.GraphicsMode.Default, "OpenTK Quick Start Sample")
        {
            VSync = VSyncMode.On;
        }
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            GL.ClearColor(0.0f, 0.4f, 0.0f, 0.0f);
            
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-1, 1, -1, 1, -1, 1);
            
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Viewport(0, 0, w, h);

            

            angle = 0.0f;

            renderShader = new Shader("..\\..\\vertexRender.glsl", "..\\..\\fragmentRender.glsl", "");
            photonShader = new Shader("..\\..\\vertexRender.glsl", "..\\..\\fragmentRender.glsl", "#define PHOTON_MAP");

            frameBuffer = new FrameBuffer(mapWidth, mapHeight);
            Allocation(); 

                       

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
            if (Keyboard[Key.KeypadAdd]) PhotonIntensity+=5;
            if (Keyboard[Key.KeypadMinus]) PhotonIntensity-=5;
            if (Keyboard[Key.Plus]) camera.ScalePlus();
            if (Keyboard[Key.Minus]) camera.ScaleMinus();
            if (Keyboard[Key.Q]) Console.WriteLine(camera.GetCoords());

        }
        

        private void Allocation()
        {
            float[] allocation = new float[mapWidth * mapHeight * 3];
            float step = 2/(float)Math.Sqrt(mapWidth*mapHeight/6);
            int i = 0;

            for (float u = -1+step; u < 1; u += step)
                for (float v = -2+step; v < 0; v += step)
                {
                    allocation[i] = 1;
                    allocation[i + 1] = v;
                    allocation[i + 2] = u;
                    i += 3;
                }

            for (float u = -1 + step; u < 1.0; u += step)
                for (float v = -2 + step; v < 0; v += step)
                 {
                     allocation[i] = -1;
                     allocation[i + 1] = v;
                     allocation[i + 2] = u;
                     i += 3;
                 }

            for (float u = -1 + step; u < 1; u += step)
                 for (float v = -2 + step; v < 0; v += step)
                 {
                     allocation[i] = u;
                     allocation[i + 1] = v;
                     allocation[i + 2] = 1;
                     i += 3;
                 }

            for (float u = -1 + step; u < 1.0; u += step)
                 for (float v = -2 + step; v < 0; v += step)
                 {
                     allocation[i] = u;
                     allocation[i + 1] = v;
                     allocation[i + 2] = -1;
                     i += 3;
                 }

            for (float u = -1 + step; u < 1; u += step)
                 for (float v = -1 + step; v < 1; v += step)
                 {
                     allocation[i] = u;
                     allocation[i + 1] = -2;
                     allocation[i + 2] = v;
                     i += 3;
                 }


            /*for (float u = -1 + step; u < 1; u += step)
                for (float v = -1 + step; v < 1; v += step)
                {
                    allocation[i] = 0.1f*u;
                    allocation[i + 1] = 0.1f;
                    allocation[i + 2] = 0.1f*v;
                    i += 3;
                }*/

            Random rnd = new Random();

            while (i < allocation.Length)
            {
                allocation[i] = (float)rnd.NextDouble();
                allocation[i + 1] = (float)rnd.NextDouble();
                allocation[i + 2] = (float)rnd.NextDouble();
                i += 3;
            }
            
            GL.GenTextures(1, out allocationTexture);
            GL.BindTexture(TextureTarget.TextureRectangle, allocationTexture);
            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb,
                         PixelType.Float, allocation);
            

            float[] fpix = new float[80 * 80 * 3];
            GL.GetTexImage(TextureTarget.TextureRectangle, 0, PixelFormat.Rgb, PixelType.Float, fpix);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            PhotonMappingUniformSet();
            RayTracingUniformSet();

            //angle += 0.1f;
            frameBuffer.Activate();
            PhotonMapping();
            frameBuffer.Deactivate();
            
            //float[] pix = new float[mapWidth * mapHeight * 3];
            //GL.GetTexImage(TextureTarget.TextureRectangleArb, 0, PixelFormat.Rgb, PixelType.Float, pix);
            
            PhotonMapSort();
            
            renderShader.SetUniformTextureRect(frameBuffer.GetTexture(), "PhotonTexture");
            RayTracing();

            SwapBuffers();

            GL.BindTexture(TextureTarget.TextureRectangle, 0);

        }

        private void PhotonMappingUniformSet()
        {
            photonShader.Activate();
                photonShader.SetUniformTextureRect(allocationTexture, "AllocationTexture");
                photonShader.SetUniform("BoxMinimum", new Vector3(-5.0F, -5.0F, -5.0F));
                photonShader.SetUniform("BoxMaximum", new Vector3(5.0F, 5.0F, 5.0F));
                photonShader.SetUniform("GlassSphere.Center", new Vector3(2.0F, -3.0F, -3.0F));
                photonShader.SetUniform("GlassSphere.Radius", 2.0F);
                photonShader.SetUniform("MatSphere.Center", new Vector3(-3.0F, -4.0F, 1.0F));
                photonShader.SetUniform("MatSphere.Radius", 1.0F);
                photonShader.SetUniform("Light.Position", new Vector3(0.0F/* + (float)Math.Sin(angle)*/, 5.0F, 0.0F/* + (float)Math.Cos(angle)*/));
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
                renderShader.SetUniform("Light.Position", new Vector3(0.0F/* + (float)Math.Sin(angle)*/, 5.0F, 0.0F/* + (float)Math.Cos(angle)*/));

                renderShader.SetUniform("Delta", 0.8F);
                renderShader.SetUniform("InverseDelta", 1.0F / 0.8F);
                renderShader.SetUniform("PhotonMapSize", new Vector2(mapWidth, mapHeight));
                renderShader.SetUniform("PhotonIntensity",  PhotonIntensity / (mapWidth * mapHeight));

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
            GL.BindTexture(TextureTarget.TextureRectangle, frameBuffer.GetTexture());

            float[] pix = new float[mapWidth * mapHeight * 3];
            GL.GetTexImage(TextureTarget.TextureRectangle, 0, PixelFormat.Rgb, PixelType.Float, pix);

            Vec3List list = new Vec3List(pix);
            list.Sort();
            float[] pixSorted = list.ToFloatArray();

            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb,
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