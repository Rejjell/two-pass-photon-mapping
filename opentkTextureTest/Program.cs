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
        Camera camera = new Camera();

        Shader renderShader;
        Shader photonShader;

        FrameBuffer frameBuffer;

        private uint allocationTexture;
        private uint randomTexture;
        
        static int w = 800;
        static int h = 800;

        int mapWidth = 100;
        int mapHeight = 100;

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

            renderShader = new Shader("..\\..\\vertexRender.glsl", "..\\..\\fragmentRender.glsl", "");
            photonShader = new Shader("..\\..\\vertexRender.glsl", "..\\..\\fragmentRender.glsl", "#define PHOTON_MAP");

            frameBuffer = new FrameBuffer(mapWidth, mapHeight);
            LightDirectionAllocation();
            SquareLightPoints();
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
        

        private void LightDirectionAllocation()
        {
            float[] allocation = new float[mapWidth * mapHeight * 3];

            int n = mapWidth*mapHeight;
            float inc = (float)Math.PI * (3 - (float)Math.Sqrt(5));
            float off = 2.0f/n;

        /*
            for (int k = 0; k < n; k++)
            {
                float y = k * off - 1 + (off / 2);
                float r = (float)Math.Sqrt(1 - y * y);
                float phi = k * inc;
                allocation[3 * k] = (float)Math.Cos(phi) * r;
                allocation[3 * k + 1] = y;
                allocation[3 * k + 2] = (float)Math.Sin(phi) * r;
            }
            */
             var r = new Random();
             for (int k = 0; k < n; k++)
             {
                 allocation[3 * k] = (float)r.NextDouble() * 2 - 1;
                 allocation[3 * k + 1] = (float)r.NextDouble() * 2 -1;
                 allocation[3 * k + 2] = (float)r.NextDouble() * 2 - 1;
             }

            GL.GenTextures(1, out allocationTexture);
            GL.BindTexture(TextureTarget.TextureRectangle, allocationTexture);
            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb,
                         PixelType.Float, allocation);
            
           // float[] fpix = new float[mapWidth * mapHeight * 3];
          //  GL.GetTexImage(TextureTarget.TextureRectangle, 0, PixelFormat.Rgb, PixelType.Float, fpix);
        }

        uint squareLightPointsTexture;

        private void SquareLightPoints()
        {
            Random r = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            float[] rnd = new float[mapWidth * mapHeight * 3];
            int n = mapWidth * mapHeight;
            /*for (int k = 0; k < n; k++)
            {
                rnd[3 * k] = (float)r.NextDouble() * 4 - 2;
                rnd[3 * k + 1] = 4.9f;
                rnd[3 * k + 2] = (float)r.NextDouble() * 4 - 2;

            }*/

            for (var i = 0; i < 80; i++)
                for (var j = 0; j < 80; j++)
                {
                    rnd[(i * 80 + j) * 3] = -2 + 4 / (float)Math.Sqrt(6400) * i;
                    rnd[(i * 80 + j) * 3 + 1] = 4.9f;
                    rnd[(i * 80 + j) * 3 + 2] = -2 + 4 / (float)Math.Sqrt(6400) * j;
                }
            


            GL.GenTextures(1, out squareLightPointsTexture);
            GL.BindTexture(TextureTarget.TextureRectangle, squareLightPointsTexture);
            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb,
                         PixelType.Float, rnd);
        }


        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            PhotonMappingUniformSet();
            RayTracingUniformSet();

            //GL.BindTexture(TextureTarget.TextureRectangle, frameBuffer.GetTexture());
            //angle += 0.1f;
            frameBuffer.Activate();
            PhotonMapping();
            frameBuffer.Deactivate();
            
            /*GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.TextureRectangle, frameBuffer.GetTexture());
            float[] pix = new float[mapWidth * mapHeight * 3];
            GL.GetTexImage(TextureTarget.TextureRectangle, 0, PixelFormat.Rgb, PixelType.Float, pix);*/
            
            PhotonMapSort();
            
            renderShader.SetUniformTextureRect(frameBuffer.GetTexture(), TextureUnit.Texture0, "PhotonTexture");
            RayTracing();

            SwapBuffers();

            //GL.BindTexture(TextureTarget.TextureRectangle, 0);

        }

        private void PhotonMappingUniformSet()
        {
            photonShader.Activate();
                photonShader.SetUniformTextureRect(allocationTexture, TextureUnit.Texture0,  "AllocationTexture");
                photonShader.SetUniformTextureRect(squareLightPointsTexture, TextureUnit.Texture1, "SquareLightTexture");
                photonShader.SetUniform("BoxMinimum", new Vector3(-5.0F, -5.0F, -5.0F));
                photonShader.SetUniform("BoxMaximum", new Vector3(5.0F, 5.0F, 5.0F));
                photonShader.SetUniform("GlassSphere.Center", new Vector3(2.0F, -3.0F, -2.0F));
                photonShader.SetUniform("GlassSphere.Radius", 2.0F);
                photonShader.SetUniform("MatSphere.Center", new Vector3(-3.0F, -4.0F, -3.0F));
                photonShader.SetUniform("MatSphere.Radius", 1.0F);
                photonShader.SetUniform("Light.Position", new Vector3(0.0F, 4.9F, 0.0F));
                photonShader.SetUniform("Light.Radius", new Vector2(0.5F * 10, 0.5F * 10));
                photonShader.SetUniform("Light.Distance", 0.5F * 10);
            photonShader.Deactivate();
        }

        private void RayTracingUniformSet()
        {
            renderShader.Activate();

    
                renderShader.SetUniform("BoxMinimum", new Vector3(-5.0F, -5.0F, -5.0F));
                renderShader.SetUniform("BoxMaximum", new Vector3(5.0F, 5.0F, 5.0F));
                renderShader.SetUniform("GlassSphere.Center", new Vector3(2.0F, -3.0F, -2.0F));
                renderShader.SetUniform("GlassSphere.Radius", 2.0F);
                renderShader.SetUniform("MatSphere.Center", new Vector3(-3.0F, -4.0F, -3.0F));
                renderShader.SetUniform("MatSphere.Radius", 1.0F);
                renderShader.SetUniform("Light.Position", new Vector3(0.0F, 4.9F, 0.0F));

                renderShader.SetUniform("Delta", 1.0F);
                renderShader.SetUniform("InverseDelta", 1.0F / 1.0F);
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
            GL.ActiveTexture(TextureUnit.Texture0);
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