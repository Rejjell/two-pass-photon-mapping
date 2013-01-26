using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace StarterKit
{
    class FrameBuffer
    {
        private uint FboHandle;
        private uint ColorTexture;
        private int mapWidth;
        private int mapHeight;

        public FrameBuffer(int mapWidth,int mapHeight)
        {
            GL.GenTextures(1, out ColorTexture);
            GL.BindTexture(TextureTarget.TextureRectangleArb, ColorTexture);
            //GL.TexParameter(TextureTarget.TextureRectangleArb, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            //GL.TexParameter(TextureTarget.TextureRectangleArb, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.TextureRectangleArb, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureRectangleArb, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexImage2D(TextureTarget.TextureRectangleArb, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);

            GL.BindTexture(TextureTarget.TextureRectangleArb, 0);

            GL.Ext.GenFramebuffers(1, out FboHandle);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, FboHandle);
            GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, TextureTarget.TextureRectangleArb, ColorTexture, 0);

            

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

        public uint GetTexture()
        {
            return ColorTexture;
        }
    }
}
