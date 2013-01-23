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
        uint FboHandle;
        uint ColorTexture;

        public FrameBuffer(int mapWidth,int mapHeight)
        {
            GL.GenTextures(1, out ColorTexture);
            GL.BindTexture(TextureTarget.TextureRectangle, ColorTexture);
            //GL.TexParameter(TextureTarget.TextureRectangleArb, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            //GL.TexParameter(TextureTarget.TextureRectangleArb, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.TextureRectangle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureRectangle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgb32f, mapWidth, mapHeight, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);

            GL.Ext.GenFramebuffers(1, out FboHandle);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, FboHandle);
            GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, TextureTarget.TextureRectangle, ColorTexture, 0);

            GL.DrawBuffer((DrawBufferMode)FramebufferAttachment.ColorAttachment0Ext);

            //GL.PushAttrib(AttribMask.ViewportBit);
            GL.Viewport(0, 0, mapWidth, mapHeight);

            //GL.PopAttrib();
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
            //GL.DrawBuffer(DrawBufferMode.Back);
        }

        public void Activate()
        {
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, FboHandle);
        }

        public void Deactivate()
        {
            //GL.PopAttrib();
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
            //GL.DrawBuffer(DrawBufferMode.Back);
        }

        public uint GetTexture()
        {
            return ColorTexture;
        }



    }
}
