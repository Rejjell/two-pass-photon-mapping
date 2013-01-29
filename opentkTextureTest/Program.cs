using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using System.Diagnostics;

namespace PhotonMapping
{
    class PhotonMappingClass : GameWindow
    {
        Camera camera = new Camera();

        Shader renderShader;
        Shader photonShader;

        FrameBuffer frameBuffer;

        private uint rectangleLightPointsTexture;
        private uint photonEmissionDirectionsTexture;
        private uint photonReflectionDirectionsTexture1;
        private uint photonReflectionDirectionsTexture2;
        private uint photonReflectionDirectionsTexture3;
        private uint randomProbabilityTexture;
        private uint rectangleLightPointsPhongTexture;

        private int photonMapSize;
        private int causticMapSize;
        
        static int w = 800;
        static int h = 800;


        int mapWidth = 80;
        private int mapHeight = 80;

        float PhotonIntensity = 100.0F;

        public PhotonMappingClass()
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

            rectangleLightPointsTexture = GenerateRandomTexture(-1,1,mapWidth,mapHeight);
            photonEmissionDirectionsTexture = GenerateRandomDirectionsTexture();
            photonReflectionDirectionsTexture1 = GenerateRandomDirectionsTexture();
            photonReflectionDirectionsTexture2 = GenerateRandomDirectionsTexture();
            photonReflectionDirectionsTexture3 = GenerateRandomDirectionsTexture();
            randomProbabilityTexture = GenerateRandomTexture(0, 1, mapWidth, mapHeight);
            rectangleLightPointsPhongTexture = GenerateRandomTexture(0, 1, 800, 800);

            PhotonMappingUniformSet();

            frameBuffer.Activate();
                Photon_Mapping();
            frameBuffer.Deactivate();

            PhotonMapSort();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            if (Keyboard[Key.Escape]) Exit();
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
        

        private uint GenerateRandomTexture(float a, float b,int w, int h)
        {
            float[] randomArray = new float[w * h * 3];
            Random r = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            for (int k = 0; k < w*h*3; k+=3)
            {
                randomArray[k] = (float) r.NextDouble()*(b - a) + a;
                randomArray[k + 1] = (float) r.NextDouble()*(b - a) + a;
                randomArray[k + 2] = (float) r.NextDouble()*(b - a) + a;
            }
            uint texture;
            GL.GenTextures(1, out texture);
            GL.BindTexture(TextureTarget.TextureRectangle, texture);
            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgb32f, w, h, 0, PixelFormat.Rgb,
                         PixelType.Float, randomArray);
            return texture;
        }

        private uint GenerateRandomDirectionsTexture()
        {
            float[] randomArray = new float[mapWidth * mapHeight * 3];
            Random r = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            for (int k = 0; k < mapWidth * mapHeight * 3; k += 3)
            {
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
            RayTracing();
            SwapBuffers();
        }

        private void PhotonMappingUniformSet()
        {
            photonShader.Activate();
                photonShader.SetUniformTextureRect(photonEmissionDirectionsTexture, TextureUnit.Texture0,  "PhotonEmissionDirectionsTexture");
                photonShader.SetUniformTextureRect(rectangleLightPointsTexture, TextureUnit.Texture1, "RectangleLightPointsTexture");
                photonShader.SetUniformTextureRect(photonReflectionDirectionsTexture1, TextureUnit.Texture2, "PhotonReflectionDirectionsTexture1");
                photonShader.SetUniformTextureRect(photonReflectionDirectionsTexture2, TextureUnit.Texture3, "PhotonReflectionDirectionsTexture2");
                photonShader.SetUniformTextureRect(photonReflectionDirectionsTexture3, TextureUnit.Texture4, "PhotonReflectionDirectionsTexture3");
                photonShader.SetUniformTextureRect(randomProbabilityTexture, TextureUnit.Texture5, "RandomProbabilityTexture");
                photonShader.SetUniform("BoxMinimum", new Vector3(-5.0F, -5.0F, -5.0F));
                photonShader.SetUniform("BoxMaximum", new Vector3(5.0F, 5.0F, 5.0F));
                photonShader.SetUniform("GlassSphere.Center", new Vector3(2.0F, -3.0F, -2.0F));
                photonShader.SetUniform("GlassSphere.Radius", 2.0F);
                photonShader.SetUniform("MatSphere.Center", new Vector3(-3.0F, -4.0F, -3.0F));
                photonShader.SetUniform("MatSphere.Radius", 1.0F);
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
                renderShader.SetUniform("Light.Position", new Vector3(0.0F, 4.999F, 0.0F));

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
                renderShader.SetUniform("RectangleLight.Length", 1.0F);
                renderShader.SetUniform("RectangleLight.Width", 1.0F);
            renderShader.Deactivate();
        }

        private void RayTracing()
        {
            GL.Viewport(0,0,w,h);
            renderShader.Activate();
            renderShader.SetUniform("PhotonTextureSize", photonMapSize);
            renderShader.SetUniform("CausticTextureSize", causticMapSize);
            renderShader.SetUniformTextureRect(frameBuffer.GetPhotonTexture(), TextureUnit.Texture6, "PhotonTexture");
            renderShader.SetUniformTextureRect(frameBuffer.GetCausticTexture(), TextureUnit.Texture7, "CausticTexture");
            renderShader.SetUniformTextureRect(rectangleLightPointsPhongTexture, TextureUnit.Texture8, "RectangleLightPointsPhongTexture");
                
            GL.Begin(BeginMode.Quads);
                GL.Vertex2(-w/2, -h/2);
                GL.Vertex2(w/2, -h/2);
                GL.Vertex2(w/2, h/2);
                GL.Vertex2(-w/2, h/2);
            GL.End();

            renderShader.Deactivate();
        }

        private void Photon_Mapping()
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
            GL.BindTexture(TextureTarget.TextureRectangle, frameBuffer.GetPhotonTexture());
            float[] pix = new float[mapWidth*mapHeight*3];
            GL.GetTexImage(TextureTarget.TextureRectangle, 0, PixelFormat.Rgb, PixelType.Float, pix);
            Vec3List list = new Vec3List(pix);

            list.Sort();
            list.Clean();
            photonMapSize = (int) Math.Ceiling(Math.Sqrt(list.list.Count));
            list.Fill(photonMapSize);

            float[] pixSorted = list.ToFloatArray();

            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgb32f, photonMapSize, photonMapSize, 0,
                          PixelFormat.Rgb,
                          PixelType.Float, pixSorted);


            GL.BindTexture(TextureTarget.TextureRectangle, frameBuffer.GetCausticTexture());

            float[] pix1 = new float[mapWidth*mapHeight*3];
            GL.GetTexImage(TextureTarget.TextureRectangle, 0, PixelFormat.Rgb, PixelType.Float, pix1);

            Vec3List list1 = new Vec3List(pix1);
            list1.Sort();
            list1.Clean();
            causticMapSize = (int) Math.Ceiling(Math.Sqrt(list1.list.Count));
            list1.Fill(causticMapSize);
            float[] pixSorted1 = list1.ToFloatArray();

            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgb32f, causticMapSize, causticMapSize, 0,
                          PixelFormat.Rgb,
                          PixelType.Float, pixSorted1);
        }


        [STAThread]
        static void Main()
        {
            using (PhotonMappingClass phtonMapping = new PhotonMappingClass())
            {
                phtonMapping.Run(30.0);
            }
        }
    }
}