/*************************************************************************************************/
/*                        Vertex Shader for Implicity Surface Ray Tracing                        */	
/*************************************************************************************************/

void main ( void )
{
   gl_TexCoord [0] = gl_Vertex;

   gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
}

