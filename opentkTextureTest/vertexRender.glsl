#version 330 compatibility

void main ( void )
{
   gl_TexCoord[0] = gl_Vertex;
   gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
}

