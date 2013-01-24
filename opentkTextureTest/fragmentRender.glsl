#extension GL_ARB_texture_rectangle : enable

#ifndef PHOTON_MAP
	struct SCamera
	{
		vec3 Position;
		vec3 Side;
		vec3 Up;
		vec3 View;
		vec2 Scale;
	};
#endif

struct SLight
{
	vec3 Position;
	#ifdef PHOTON_MAP
		vec2 Radius;
		float Distance;
	#endif
};

struct SRay
{
	vec3 Origin;
	vec3 Direction;
};

struct SIntersection
{
	float Time;
	vec3 Point;
	vec3 Normal;
	#ifndef PHOTON_MAP
		vec3 Color;
		vec4 Material;
	#endif
};

struct SSphere
{
	vec3 Center;
	float Radius;
};

struct RectangleLightStruct
{
	vec3 Color;
	vec2 Center;
	float Width;
	float Length;
};

#define TRACE_DEPTH_2					// Sets number of secondary rays ( from 1 to 3 )

const float GlassAirIndex = 4.5;							// Ratio of refraction indices of glass and air
const float AirGlassIndex = 1.0 / GlassAirIndex;			// Ratio of refraction indices of air and glass

const vec3 GlassColor = vec3 ( 1.0, 1.0, 1.0 );		
const vec3 MatColor = vec3 ( 0.0, 0.0, 1.0 );
const vec3 RightWallColor = vec3 (1.0,0.0,0.0);
const vec3 LeftWallColor = vec3 (0.0,1.0,0.0);
const vec3 DefaultWallsColor = vec3 (1.0,0.6,0.25);

const vec4 GlassMaterial = vec4 ( 0.1, 0.1, 0.6, 128.0 );
const vec4 MatMaterial = vec4 ( 0.1, 1.0, 0.05, 8.0 );	// Glass material ( ambient, diffuse and specular coeffs )
const vec4 WallMaterial = vec4 ( 0.1, 0.8, 0.1, 32.0 );		// Wall material ( ambient, diffuse and specular coeffs )
const vec4 FloorMaterial = vec4 ( 0.0, 0.5, 0.3, 32.0 );	// Floor material ( ambient, diffuse and specular coeffs )

const vec3 Zero = vec3 ( 0.0, 0.0, 0.0 );
const vec3 Unit = vec3 ( 1.0, 1.0, 1.0 );

const vec3 AxisX = vec3 ( 1.0, 0.0, 0.0 );
const vec3 AxisY = vec3 ( 0.0, 1.0, 0.0 );
const vec3 AxisZ = vec3 ( 0.0, 0.0, 1.0 );

const vec3 MirrorX = vec3 ( -1.0, 1.0, 1.0 );
const vec3 MirrorY = vec3 ( 1.0, -1.0, 1.0 );
const vec3 MirrorZ = vec3 ( 1.0, 1.0, -1.0 );

#define BIG 1000000.0
#define EPSILON 0.01
#define PI 3.14159265

uniform vec3 BoxMinimum;						// Minimum point of bounding box
uniform vec3 BoxMaximum;						// Maximum point of bounding box
uniform SLight Light;							// Ligth source parameters
uniform SSphere GlassSphere;
uniform SSphere MatSphere;	
uniform RectangleLightStruct RectangleLight;		


#ifndef PHOTON_MAP
	uniform SCamera Camera;							// Camera parameters
	uniform vec2 PhotonMapSize;						// Size of photon map
	uniform float PhotonIntensity;					// Intensity of single photon
	uniform float Delta;							// Radius of vicinity for gathering of photons
	uniform float InverseDelta;						// Inverse radius for fast calculations
	uniform sampler2DRect PhotonTexture;
#else
	uniform sampler2DRect AllocationTexture;
#endif

float IntersectBox ( SRay ray, vec3 minimum, vec3 maximum )
{
	vec3 OMAX = ( minimum - ray.Origin ) / ray.Direction;
	vec3 OMIN = ( maximum - ray.Origin ) / ray.Direction;
	vec3 MAX = max ( OMAX, OMIN );
	
	return min ( MAX.x, min ( MAX.y, MAX.z ) );
}

bool IntersectBox ( SRay ray, vec3 minimum, vec3 maximum, out float start, out float final )
{
	vec3 OMAX = ( minimum - ray.Origin ) / ray.Direction;
	vec3 OMIN = ( maximum - ray.Origin ) / ray.Direction;
	vec3 MAX = max ( OMAX, OMIN );
	vec3 MIN = min ( OMAX, OMIN );
	final = min ( MAX.x, min ( MAX.y, MAX.z ) );
	start = max ( max ( MIN.x, 0.0), max ( MIN.y, MIN.z ) );	

	return final > start;
}

bool IntersectPlane ( SRay ray, vec3 normal, float distance, float start, float final, out float time )
{
	time = ( distance - dot ( normal, ray.Origin ) ) / dot ( normal, ray.Direction );

	return ( time >= start ) && ( time <= final );
}

bool IntersectSphere ( SRay ray, float start, float final, out float time, SSphere Sphere )
{
	ray.Origin -= Sphere.Center;
	float A = dot ( ray.Direction, ray.Direction );
	float B = dot ( ray.Direction, ray.Origin );
	float C = dot ( ray.Origin, ray.Origin ) - Sphere.Radius * Sphere.Radius;
	float D = B * B - A * C;
	
	if ( D > 0.0 )
	{
		D = sqrt ( D );

		time = min ( max ( 0.0, ( -B - D ) / A ), ( -B + D ) / A );

		return ( time >= start ) && ( time <= final );
	}

	return false;
}

float RND_1d(vec2 x)
{
	uint n = floatBitsToUint(x.y * 214013.0 + x.x * 2531011.0);
	n = n * (n * n * 15731u + 789221u);
	n = (n >> 9u) | 0x3F800000u;

	return 2.0 - uintBitsToFloat(n);
}

SRay GenerateRay ( void )
{
#ifdef PHOTON_MAP
	/*float u=gl_TexCoord[0].x;
	float theta=-(gl_TexCoord[0].y+1.0)*PI/2;
	vec3 direction = vec3(sqrt(1-u*u)*cos(theta),sqrt(1-u*u)*sin(theta), u);*/

	vec3 direction = texture2DRect(AllocationTexture, vec2((gl_TexCoord[0].x+1)*40, (gl_TexCoord[0].y+1)*40));
	
	return SRay ( Light.Position, normalize ( direction ) );
#else
	vec2 coords = gl_TexCoord[0].xy * Camera.Scale;
	vec3 direction = Camera.View + Camera.Side * coords.x + Camera.Up * coords.y;

	return SRay ( Camera.Position, normalize ( direction ) );
#endif
}

vec3 Refract ( vec3 incident, vec3 normal, float index )
{
	float dot = dot ( incident, normal );
	float square = 1.0 - index * index * ( 1.0 - dot * dot );

	if ( square < 0.0 )
	{
		return reflect ( incident, normal );
	}
	else
	{
		return index * incident - ( sqrt ( square ) + index * dot ) * normal;
	}
}

#ifndef PHOTON_MAP
	vec3 Phong ( SIntersection intersect )
	{
		vec3 light = normalize ( Light.Position - intersect.Point );
		vec3 view = normalize ( Camera.Position - intersect.Point );
		float diffuse = max ( dot ( light, intersect.Normal ), 0.0 );
		vec3 reflect = reflect ( -view, intersect.Normal );
		float specular = pow ( max ( dot ( reflect, light ), 0.0 ), intersect.Material.w );

		return intersect.Material.x * Unit +
			   intersect.Material.y * diffuse * intersect.Color +
			   intersect.Material.z * specular * Unit;
	}

	bool Compare ( vec3 left, vec3 right )
	{
		bvec3 greater = greaterThan ( left, right );
		bvec3 equal = equal ( left, right );
	
		return greater.x || ( equal.x && greater.y ) || ( equal.x && equal.y && greater.z );

	}

	float BinSearch ( vec3 point )
	{
		float left = 0.0;
		float right = PhotonMapSize.x * PhotonMapSize.y;
		float center;
    
		while ( left < right )
		{
			center = 0.5 * ( left + right );
			vec3 position = texture2DRect ( PhotonTexture,
				vec2 ( mod ( center, PhotonMapSize.x ), floor ( center / PhotonMapSize.y ) ) ).xyz; 

			if ( Compare ( point, position ) )
			{
				left = center + 1.0;
			}        
			else
			{
				right = center - 1.0;
			}        
		 }
	
		return center;
	}

	void Caustic ( SIntersection intersect, inout vec3 color )
	{
		float left = BinSearch ( intersect.Point - vec3 ( Delta ) );
		float right = BinSearch ( intersect.Point + vec3 ( Delta ) );
	
		for ( float i = left; i <= right; i++ )
		{
			vec4 photon = texture2DRect (
				PhotonTexture, vec2 ( mod ( i, PhotonMapSize.x ), floor ( i / PhotonMapSize.y ) ) );
		
			color +=
				max ( 0.0, 1.0 - InverseDelta * length ( photon.xyz - intersect.Point ) ) * PhotonIntensity;
		}

	}
#endif

bool Raytrace ( SRay ray, float start, float final, inout SIntersection intersect, out bool refract )
{
	bool result = false;
	float test = BIG;

	if ( IntersectPlane ( ray, AxisY, BoxMinimum.y, start, final, test ) && test < intersect.Time )
	{
		intersect.Time = test;
		intersect.Point = ray.Origin + ray.Direction * test;

		#ifndef PHOTON_MAP
			intersect.Normal = AxisY;
			intersect.Color = DefaultWallsColor;//Bottom
			intersect.Material = FloorMaterial;
		#endif

		result = true;
	}

	if ( IntersectPlane ( ray, AxisY, BoxMaximum.y, start, final, test ) && test < intersect.Time )
	{
		intersect.Time = test;
		intersect.Point = ray.Origin + ray.Direction * test;

		#ifndef PHOTON_MAP
			intersect.Normal = -AxisY;
		
			if ((intersect.Point.x<(RectangleLight.Center.x + RectangleLight.Width/2))
			  &&(intersect.Point.x>(RectangleLight.Center.x - RectangleLight.Width/2))
			  &&(intersect.Point.z<(RectangleLight.Center.y + RectangleLight.Length/2))
			  &&(intersect.Point.z>(RectangleLight.Center.y - RectangleLight.Length/2)))
			{
				intersect.Color = RectangleLight.Color;//Ceiling
				intersect.Material = WallMaterial;
			}
			else
			{
				intersect.Color = DefaultWallsColor;//Ceiling
				intersect.Material = WallMaterial;
			}
		#endif

		result = true;
	}

	if ( IntersectPlane ( ray, AxisX, BoxMinimum.x, start, final, test ) && test < intersect.Time )
	{
		intersect.Time = test;
		intersect.Point = ray.Origin + ray.Direction * test;

		#ifndef PHOTON_MAP
			intersect.Normal = AxisX;
			intersect.Color = LeftWallColor;//Wall
			intersect.Material = WallMaterial;
		#endif

		result = true;
	}

	if ( IntersectPlane ( ray, AxisX, BoxMaximum.x, start, final, test ) && test < intersect.Time )
	{
		intersect.Time = test;
		intersect.Point = ray.Origin + ray.Direction * test;

		#ifndef PHOTON_MAP
			intersect.Normal = -AxisX;
			intersect.Color = RightWallColor;//Wall
			intersect.Material = WallMaterial;
		#endif

		result = true;
	}

	if ( IntersectPlane ( ray, AxisZ, BoxMinimum.z, start, final, test ) && test < intersect.Time )
	{
		intersect.Time = test;
		intersect.Point = ray.Origin + ray.Direction * test;

		#ifndef PHOTON_MAP
			intersect.Normal = AxisZ;
			intersect.Color = DefaultWallsColor;//Wall
			intersect.Material = WallMaterial;
		#endif

		result = true;
	}

	if ( IntersectPlane ( ray, AxisZ, BoxMaximum.z, start, final, test ) && test < intersect.Time )
	{
		intersect.Time = test;
		intersect.Point = ray.Origin + ray.Direction * test;

		#ifndef PHOTON_MAP
			intersect.Normal = -AxisZ;
			intersect.Color = DefaultWallsColor;//Wall
			intersect.Material = WallMaterial;
		#endif

		result = true;
	}

	refract = false;

	if ( IntersectSphere ( ray, start, final, test, GlassSphere ) && test < intersect.Time )
	{
		intersect.Time = test;
		intersect.Point = ray.Origin + ray.Direction * test;
		intersect.Normal = normalize ( intersect.Point - GlassSphere.Center );

		#ifndef PHOTON_MAP
			intersect.Color = GlassColor;
			intersect.Material = GlassMaterial;
		#endif

		refract = true;
		result = true;
	}

	if ( IntersectSphere ( ray, start, final, test, MatSphere ) && test < intersect.Time )
	{
		intersect.Time = test;
		intersect.Point = ray.Origin + ray.Direction * test;
		intersect.Normal = normalize ( intersect.Point - MatSphere.Center );

		#ifndef PHOTON_MAP
			intersect.Color = MatColor;
			intersect.Material = MatMaterial;
		#endif

		result = true;
	}



	return result;
}

void main ( void )
{
	SRay ray = GenerateRay ( );
	float start, final;

	if ( !IntersectBox ( ray, BoxMinimum, BoxMaximum, start, final ) )
	{
		discard;
	}
	
	ray.Origin += start * ray.Direction;
	SIntersection intersect;
	intersect.Time = BIG;

	#ifdef PHOTON_MAP
		intersect.Point = vec3 ( BIG );
	#else
		vec3 color = Zero;
	#endif

	bool trace = false;
	bool air = true;

	if ( Raytrace ( ray, EPSILON, final, intersect, trace ) )
	{
		#ifndef PHOTON_MAP
			color += Phong ( intersect );
		#endif

		if ( trace )
		{
			vec3 refract = Refract ( ray.Direction,
				                     mix ( -intersect.Normal, intersect.Normal, float ( air ) ),
									 mix ( GlassAirIndex, AirGlassIndex, float ( air ) ) );
			air = !air; 
			ray = SRay ( intersect.Point, refract );
			final = IntersectBox ( ray, BoxMinimum, BoxMaximum ); 
			intersect.Time = BIG;

			
			vec3 d=ray.Direction;
			vec3 pos= intersect.Point;
			vec3 normal=intersect.Normal;
			vec3 reflectDir = d- (2*dot(normal,d)) *normal;

			ray.Origin=pos;
			ray.Direction = reflectDir;


			if ( Raytrace ( ray, EPSILON, final, intersect, trace ) )
			{
				#ifndef PHOTON_MAP
					color += GlassColor * Phong ( intersect );
				#endif

				#ifndef TRACE_DEPTH_1

				if ( trace )
				{
					refract = Refract ( ray.Direction,
										mix ( -intersect.Normal, intersect.Normal, float ( air ) ),
										mix ( GlassAirIndex, AirGlassIndex, float ( air ) ) );

					air = !air; ray = SRay ( intersect.Point, refract );
					final = IntersectBox ( ray, BoxMinimum, BoxMaximum ); intersect.Time = BIG;

					if ( Raytrace ( ray, EPSILON, final, intersect, trace ) )
					{
						#ifndef PHOTON_MAP
							color += GlassColor * Phong ( intersect );
						#endif
						
						#ifndef TRACE_DEPTH_2

						if ( trace )
						{
							refract = Refract ( ray.Direction,
												mix ( -intersect.Normal, intersect.Normal, float ( air ) ),
												mix ( GlassAirIndex, AirGlassIndex, float ( air ) ) );
							air = !air; ray = SRay ( intersect.Point, refract );
							final = IntersectBox ( ray, BoxMinimum, BoxMaximum ); intersect.Time = BIG;

							if ( Raytrace ( ray, EPSILON, final, intersect, trace ) )
							{
								#ifndef PHOTON_MAP
									color += GlassColor * Phong ( intersect );

									if ( !trace )
									{
										Caustic ( intersect, color );
									}

								#endif
							}   // Tracing 3 secondary ray
						}   // If water
						else
						{
							#ifndef PHOTON_MAP
								Caustic ( intersect, color );
							#endif
						}

						#endif
					}   // Tracing 2 secondary ray
				}   // If water
				else
				{
					#ifndef PHOTON_MAP
						Caustic ( intersect, color );
					#endif
				}

				#endif
			}   // Tracing 1 secondary ray
		}   // If water
		else
		{
			#ifndef PHOTON_MAP
				Caustic ( intersect, color );
			#endif
		}
	}   // Tracing primary ray

	#ifdef PHOTON_MAP
		gl_FragColor = vec4 ( intersect.Point, 0.0 );
		//gl_FragColor = vec4 ( gl_TexCoord[0].x, gl_TexCoord[0].y,0.0,0.0 );
		//gl_FragColor = texture2DRect(AllocationTexture, vec2((gl_TexCoord[0].x+1)*40, (gl_TexCoord[0].y+1)*40));
	#else
		gl_FragColor = vec4 ( color, 1.0 );
	#endif
}