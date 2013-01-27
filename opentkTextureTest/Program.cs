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
        FrameBuffer frameBuffer1;

        private uint rectangleLightPointsTexture;
        private uint photonEmissionDirectionsTexture;
        private uint photonRefletionDirectionsTexture1;
        private uint photonRefletionDirectionsTexture2;
        private uint photonRefletionDirectionsTexture3;
        private uint randomProbabilityTexture;
        
        static int w = 800;
        static int h = 800;

        int mapWidth = 20;
        int mapHeight = 20;

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

            /*rectangleLightPointsTexture = GenerateRandomTexture(-1,1);
            photonEmissionDirectionsTexture = GenerateRandomDirectionsTexture();
            photonRefletionDirectionsTexture1 = GenerateRandomDirectionsTexture();
            photonRefletionDirectionsTexture2 = GenerateRandomDirectionsTexture();
            photonRefletionDirectionsTexture3 = GenerateRandomDirectionsTexture();
            randomProbabilityTexture = GenerateRandomTexture(0,1);

            PhotonMappingUniformSet();

            frameBuffer.Activate();
            PhotonMapping();
            frameBuffer.Deactivate();

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.TextureRectangle, frameBuffer.GetTexture());
            float[] pix = new float[mapWidth * mapHeight * 3];
            GL.GetTexImage(TextureTarget.TextureRectangle, 0, PixelFormat.Rgb, PixelType.Float, pix);

            PhotonMapSort();*/

            
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
        

        private uint GenerateRandomTexture(float a, float b)
        {
            float[] randomArray = new float[mapWidth * mapHeight * 3];

            Random r = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

            for (int k = 0; k < mapWidth*mapHeight*3; k+=3)
            {
                randomArray[k] = (float) r.NextDouble()*(b - a) + a;
                randomArray[k + 1] = (float) r.NextDouble()*(b - a) + a;
                randomArray[k + 2] = (float) r.NextDouble()*(b - a) + a;
            }

            uint texture;
            GL.GenTextures(1, out texture);
            GL.BindTexture(TextureTarget.TextureRectangle, texture);
            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb,
                         PixelType.Float, randomArray);
            
            float[] fpix = new float[mapWidth * mapHeight * 3];
            GL.GetTexImage(TextureTarget.TextureRectangle, 0, PixelFormat.Rgb, PixelType.Float, fpix);

            return texture;
        }

        private uint GenerateRandomDirectionsTexture()
        {
            float[] randomArray = new float[mapWidth * mapHeight * 3];

            Random r = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

            for (int k = 0; k < mapWidth * mapHeight * 3; k += 3)
            {
                /*float x = (float)r.NextDouble()*2 - 1;
                float y = (float)((r.NextDouble() * 2 - 1)*Math.Sqrt(1-x*x));
                float z = (float) Math.Sqrt(1 - x*x - y*y);
                float p = (float)r.NextDouble();
                if (p > 0.5f) z = -z;*/
                randomArray[k] = (float)r.NextDouble() * 2 - 1;
                randomArray[k + 1] = (float)r.NextDouble() * 2 - 1;
                randomArray[k + 2] = (float)r.NextDouble() * 2 - 1;
            }

            uint texture;
            GL.GenTextures(1, out texture);
            GL.BindTexture(TextureTarget.TextureRectangle, texture);
            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb,
                         PixelType.Float, randomArray);

            float[] fpix = new float[mapWidth * mapHeight * 3];
            GL.GetTexImage(TextureTarget.TextureRectangle, 0, PixelFormat.Rgb, PixelType.Float, fpix);

            return texture;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            
            RayTracingUniformSet();

            //GL.BindTexture(TextureTarget.TextureRectangle, frameBuffer.GetTexture());
            //angle += 0.1f;
            

            //frameBuffer1 = new FrameBuffer(w, h);
            //frameBuffer1.Activate();
            renderShader.SetUniformTextureRect(frameBuffer.GetTexture(), TextureUnit.Texture0, "PhotonTexture");
            RayTracing();
            //frameBuffer1.Deactivate();

            /*GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.TextureRectangle, frameBuffer1.GetTexture());
            float[] pix1 = new float[w * h * 3];
            GL.GetTexImage(TextureTarget.TextureRectangle, 0, PixelFormat.Rgb, PixelType.Float, pix1);*/

            SwapBuffers();

            //GL.BindTexture(TextureTarget.TextureRectangle, 0);

        }

        private void PhotonMappingUniformSet()
        {
            photonShader.Activate();
                photonShader.SetUniformTextureRect(photonEmissionDirectionsTexture, TextureUnit.Texture0,  "PhotonEmissionDirectionsTexture");
                photonShader.SetUniformTextureRect(rectangleLightPointsTexture, TextureUnit.Texture1, "RectangleLightPointsTexture");
                photonShader.SetUniformTextureRect(photonRefletionDirectionsTexture1, TextureUnit.Texture2, "PhotonRefletionDirectionsTexture1");
                photonShader.SetUniformTextureRect(photonRefletionDirectionsTexture2, TextureUnit.Texture3, "PhotonRefletionDirectionsTexture2");
                photonShader.SetUniformTextureRect(photonRefletionDirectionsTexture3, TextureUnit.Texture4, "PhotonRefletionDirectionsTexture3");
                photonShader.SetUniformTextureRect(randomProbabilityTexture, TextureUnit.Texture5, "RandomProbabilityTexture");
                photonShader.SetUniform("BoxMinimum", new Vector3(-5.0F, -5.0F, -5.0F));
                photonShader.SetUniform("BoxMaximum", new Vector3(5.0F, 5.0F, 5.0F));
                photonShader.SetUniform("GlassSphere.Center", new Vector3(2.0F, -3.0F, -2.0F));
                photonShader.SetUniform("GlassSphere.Radius", 2.0F);
                photonShader.SetUniform("MatSphere.Center", new Vector3(-3.0F, -4.0F, -3.0F));
                photonShader.SetUniform("MatSphere.Radius", 1.0F);
                photonShader.SetUniform("Light.Position", new Vector3(0.0F, 5.0F, 0.0F));
                photonShader.SetUniform("Light.Radius", new Vector2(0.5F * 10, 0.5F * 10));
                photonShader.SetUniform("Light.Distance", 0.5F * 10);
                photonShader.SetUniform("PhotonMapSize", new Vector2(mapWidth, mapHeight));
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
                renderShader.SetUniform("Light.Position", new Vector3(0.0F, 5.0F, 0.0F));

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
                renderShader.SetUniform("RectangleLight.Color", new Vector3(1.0F, 1.0F, 1.0F));
                renderShader.SetUniform("RectangleLight.Length", 4.0F);
                renderShader.SetUniform("RectangleLight.Width", 4.0F);
            renderShader.Deactivate();
        }

        private void RayTracing()
        {
            GL.Viewport(0,0,w,h);
            
            renderShader.Activate();
                GL.Begin(BeginMode.Quads);
                GL.Vertex2(-w/2, -h/2);
                GL.Vertex2(w/2, -h/2);
                GL.Vertex2(w/2, h/2);
                GL.Vertex2(-w/2, h/2);
                GL.End();
            renderShader.Deactivate();
        }

        private void PhotonMapping()
        {
            photonShader.Activate();
                GL.Begin(BeginMode.Quads);
                GL.Vertex2(-mapWidth / 2, -mapHeight / 2);
                GL.Vertex2(mapWidth / 2, -mapHeight / 2);
                GL.Vertex2(mapWidth / 2, mapHeight / 2);
                GL.Vertex2(-mapWidth / 2, mapHeight / 2);
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