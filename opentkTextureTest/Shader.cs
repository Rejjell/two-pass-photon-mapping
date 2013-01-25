using System.IO;
using OpenTK;
//using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace StarterKit
{
    public class Shader
    {
        private int vertexShader;
        private int fragmentShader;

        private int program;

        public Shader(string vertexPath, string fragPath, string defineString)
        {
            program = GL.CreateProgram();
            int result;

            AttachVertexShader(vertexPath);
            AttachFragmentShader(fragPath, defineString);
            GL.LinkProgram(program);

            GL.GetProgram(program, ProgramParameter.LinkStatus, out result);
            if (result == 0)
            {
                System.Diagnostics.Debug.WriteLine("Failed to link shader program!");
                System.Diagnostics.Debug.WriteLine(GL.GetProgramInfoLog(program));
            }
        }

        private void AttachVertexShader(string path)
        {
            vertexShader = GL.CreateShader(ShaderType.VertexShader);

            var vertexReader = new StreamReader(path);
            string vertex = vertexReader.ReadToEnd();

            GL.ShaderSource(vertexShader, vertex);
            GL.CompileShader(vertexShader);

            int result;
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out result);
            if (result == 0)
            {
                System.Diagnostics.Debug.WriteLine("Failed to compile vertex shader!");
                System.Diagnostics.Debug.WriteLine(GL.GetShaderInfoLog(vertexShader));
            }

            GL.AttachShader(program, vertexShader);
        }

        private void AttachFragmentShader(string path, string defineString)
        {
            fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

            var fragReader = new StreamReader(path);
            string frag = fragReader.ReadToEnd();

            if (defineString == "")
                GL.ShaderSource(fragmentShader, frag);
            else
            {
                string[] sources = { defineString, frag };
                unsafe
                {
                    GL.ShaderSource(fragmentShader, 2, sources, null);
                }
            }

            GL.CompileShader(fragmentShader);

            int result;
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out result);
            if (result == 0)
            {
                System.Diagnostics.Debug.WriteLine("Failed to compile fragment shader!");
                System.Diagnostics.Debug.WriteLine(GL.GetShaderInfoLog(fragmentShader));
            }

            GL.AttachShader(program, fragmentShader);
        }

        public void SetUniform(string name, float value)
        {
            int loc = GL.GetUniformLocation(program, name);
            GL.Uniform1(loc, value);
        }

        public void SetUniform(string name, Vector2 value)
        {
            int loc = GL.GetUniformLocation(program, name);
            GL.Uniform2(loc, ref value);
        }

        public void SetUniform(string name, Vector3 value)
        {
            int loc = GL.GetUniformLocation(program, name);
            GL.Uniform3(loc, ref value);
        }

        public void Activate()
        {
            GL.UseProgram(program);
        }

        public void Deactivate()
        {
            GL.UseProgram(0);
        }

        public void SetUniformTextureRect(uint textureId, TextureUnit textureUnit, string UniformName)
        {
            GL.ActiveTexture(textureUnit);
            GL.BindTexture(TextureTarget.TextureRectangle, textureId);
            GL.Uniform1(GL.GetUniformLocation(program, UniformName), textureUnit-TextureUnit.Texture0);
            
        }

    }

}