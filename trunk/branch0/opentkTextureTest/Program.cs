using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;

namespace StarterKit
{
    class Game : GameWindow
    {
        MyCamera camera = new MyCamera();

        Shader renderShader;
        Shader photonShader;

        FrameBuffer frameBuffer;
        
        static int w = 800;
        static int h = 800;

        private float angle;

        int mapWidth = 80;
        int mapHeight = 80;

        private uint allocationTexture;

        private uint photonTexture;

        static int LoadTexture(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException(filename);

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            Bitmap bmp = new Bitmap(filename);
            System.Drawing.Imaging.BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);

            
            bmp.UnlockBits(bmp_data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return id;
        }

        static int LoadPhotonTexture(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException(filename);

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            Bitmap bmp = new Bitmap(filename);
            System.Drawing.Imaging.BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);
            
            bmp.UnlockBits(bmp_data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return id;
        }

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
            Allocation();
            angle = 0.0f;
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

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            
            //angle += 0.1f;

            PhotonMapping();

            SceneRender();
            //CreatePhotonMap();
            SwapBuffers();
        }


        private void SceneRender()
        {
            GL.Viewport(0,0,w,h);
            renderShader = new Shader("..\\..\\vertexRender.glsl", "..\\..\\fragmentRender.glsl", "");

            renderShader.Activate();
            renderShader.SetUniform("BoxMinimum", new Vector3(-5.0F, -5.0F, -5.0F));
            renderShader.SetUniform("BoxMaximum", new Vector3(5.0F, 5.0F, 5.0F));
            renderShader.SetUniform("GlassSphere.Center", new Vector3(2.0F, -3.0F, -3.0F));
            renderShader.SetUniform("GlassSphere.Radius", 2.0F);
            renderShader.SetUniform("MatSphere.Center", new Vector3(-3.0F, -4.0F, 1.0F));
            renderShader.SetUniform("MatSphere.Radius", 1.0F);
            renderShader.SetUniform("Light.Position", new Vector3(0.0F/* + (float)Math.Sin(angle)*/, 0.0F, 0.0F/* + (float)Math.Cos(angle)*/));
            renderShader.SetUniformTexture(frameBuffer.GetTexture(),TextureUnit.Texture0,"PhotonTexture");
            
            renderShader.SetUniform("Delta", 0.6F);
            renderShader.SetUniform("InverseDelta", 1.0F / 0.6F);
            renderShader.SetUniform("PhotonMapSize", new Vector2(mapWidth, mapHeight));
            renderShader.SetUniform("PhotonIntensity", 50.0F / (mapWidth * mapHeight));

            renderShader.SetUniform("Camera.Position", camera.GetPosition());
            renderShader.SetUniform("Camera.View", camera.GetView());
            renderShader.SetUniform("Camera.Side", camera.GetRight());
            renderShader.SetUniform("Camera.Up", camera.GetUp());
            renderShader.SetUniform("Camera.Scale", camera.GetScale());

            GL.Color3(Color.Red);
            GL.Begin(BeginMode.Quads);
            GL.Vertex2(-400, -400);
            GL.Vertex2(400, -400);
            GL.Vertex2(400, 400);
            GL.Vertex2(-400, 400);
            GL.End();

            renderShader.Deactivate();

            
        }

        private void CreatePhotonMap()
        {
            photonShader.Activate();
                photonShader.SetUniform("BoxMinimum", new Vector3(-5.0F, -5.0F, -5.0F));
                photonShader.SetUniform("BoxMaximum", new Vector3(5.0F, 5.0F, 5.0F));
                photonShader.SetUniform("GlassSphere.Center", new Vector3(2.0F, -3.0F, -3.0F));
                photonShader.SetUniform("GlassSphere.Radius", 2.0F);
                photonShader.SetUniform("MatSphere.Center", new Vector3(-3.0F, -4.0F, 1.0F));
                photonShader.SetUniform("MatSphere.Radius", 1.0F);
                photonShader.SetUniform("Light.Position", new Vector3(0.0F/* + (float)Math.Sin(angle)*/, 0.0F, 0.0F/* + (float)Math.Cos(angle)*/));
                photonShader.SetUniform("Light.Radius", new Vector2(0.5F * 10, 0.5F * 10));
                photonShader.SetUniform("Light.Distance", 0.5F * 10);
                photonShader.SetUniformTexture(allocationTexture, TextureUnit.Texture0, "AllocationTexture");
                
                GL.Begin(BeginMode.Quads);
                GL.Vertex2(-40, -40);
                GL.Vertex2(40, -40);
                GL.Vertex2(40, 40);
                GL.Vertex2(-40, 40);
                GL.End();
            photonShader.Deactivate();
        }

        private void Allocation()
        {
                        float[] allocation = new float[mapWidth * mapHeight * 3];
            //GL.GetTexImage(TextureTarget.TextureRectangleArb, 0, PixelFormat.Rgb, PixelType.Float, pix);

            float anglei = 0.0f;
            float anglej = 0.0f;

            var rnd = new Random();

            rnd.NextDouble();
            /*
            for (int i = 0; i < 80; i++)
            {
                anglei += (float)Math.PI/80;

                for (int j = 0; j < 80; j++)
                {
                    anglej += (float)Math.PI/80;

                    allocation[(i*80 + j)*3] = (float) Math.Cos(anglei);
                    allocation[(i * 80 + j) * 3 + 1] = (float)Math.Sin(anglei) + (float)Math.Cos(anglej);
                    allocation[(i * 80 + j) * 3 + 2] = (float)Math.Sin(anglej);
                }
            }*/


            for (int i = 0; i < 80; i++)
            {
                for (int j = 0; j < 80; j++)
                {
                    allocation[(i * 80 + j) * 3] =(float) rnd.NextDouble()*2-1;
                    allocation[(i * 80 + j) * 3 + 1] = (float)rnd.NextDouble();
                    allocation[(i * 80 + j) * 3 + 2] = (float)rnd.NextDouble()*2-1;
                }
            }
            
            GL.GenTextures(1, out allocationTexture);
            GL.BindTexture(TextureTarget.TextureRectangleArb, allocationTexture);
            GL.TexImage2D(TextureTarget.TextureRectangleArb, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb,
                         PixelType.Float, allocation);
            
          /*  float[] fpix = new float[80 * 80 * 3];
            GL.GetTexImage(TextureTarget.TextureRectangleArb, 0, PixelFormat.Rgb, PixelType.Float, fpix);*/
        }


        private void PhotonMapping()
        {
            photonShader = new Shader("..\\..\\vertexRender.glsl", "..\\..\\fragmentRender.glsl", "#define PHOTON_MAP");

            frameBuffer.Activate();
            GL.Viewport(0,0,mapWidth,mapHeight);
            CreatePhotonMap();
            frameBuffer.Deactivate();

            GL.BindTexture(TextureTarget.TextureRectangleArb, frameBuffer.GetTexture());

            float[] pix = new float[mapWidth*mapHeight*3];
            GL.GetTexImage(TextureTarget.TextureRectangleArb, 0, PixelFormat.Rgb, PixelType.Float, pix);
            
            Vec3List list = new Vec3List(pix);
            list.Sort();
            float[] pixSorted = list.ToFloatArray();

            GL.TexImage2D(TextureTarget.TextureRectangleArb, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb,
                          PixelType.Float, pixSorted);

            //float[] fpix = new float[80 * 80 * 3];
            //GL.GetTexImage(TextureTarget.TextureRectangleArb, 0, PixelFormat.Rgb, PixelType.Float, fpix);

           
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