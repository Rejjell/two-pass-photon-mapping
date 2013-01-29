using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace PhotonMapping
{
    class FrameBuffer
    {
        private uint FboHandle;
        private uint photonTexture;
        private uint causticTexture;
        private int mapWidth;
        private int mapHeight;

        public FrameBuffer(int mapWidth,int mapHeight)
        {
            GL.GenTextures(1, out photonTexture);
            GL.BindTexture(TextureTarget.TextureRectangleArb, photonTexture);
            GL.TexParameter(TextureTarget.TextureRectangleArb, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureRectangleArb, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexImage2D(TextureTarget.TextureRectangleArb, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);

            GL.GenTextures(1, out causticTexture);
            GL.BindTexture(TextureTarget.TextureRectangleArb, causticTexture);
            GL.TexParameter(TextureTarget.TextureRectangleArb, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureRectangleArb, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexImage2D(TextureTarget.TextureRectangleArb, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
            GL.BindTexture(TextureTarget.TextureRectangleArb, 0);

            GL.Ext.GenFramebuffers(1, out FboHandle);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, FboHandle);
            GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0, TextureTarget.TextureRectangleArb, photonTexture, 0);
            GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment1, TextureTarget.TextureRectangleArb, causticTexture, 0);

            DrawBuffersEnum[] drawBuffers = { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 };
            GL.DrawBuffers(2, drawBuffers);

            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);

            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
        }

        public void Activate()
        {
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, FboHandle);
            GL.Viewport(0, 0, mapWidth, mapHeight);
        }

        public void Deactivate()
        {
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
        }

        public uint GetPhotonTexture()
        {
            return photonTexture;
        }

        public uint GetCausticTexture()
        {
            return causticTexture;
        }
    }
}
