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

#define TRACE_DEPTH_2
// Sets number of secondary rays ( from 1 to 3 )

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

uniform vec2 PhotonMapSize;

#ifndef PHOTON_MAP
	uniform SCamera Camera;							// Camera parameters
							// Size of photon map
	uniform float PhotonIntensity;					// Intensity of single photon
	uniform float Delta;							// Radius of vicinity for gathering of photons
	uniform float InverseDelta;						// Inverse radius for fast calculations
	uniform sampler2DRect PhotonTexture;

#else
	uniform sampler2DRect PhotonEmissionDirectionsTexture;
	uniform sampler2DRect PhotonRefletionDirectionsTexture1;
	uniform sampler2DRect PhotonRefletionDirectionsTexture2;
	uniform sampler2DRect PhotonRefletionDirectionsTexture3;
	uniform sampler2DRect RectangleLightPointsTexture;
	uniform sampler2DRect RandomProbabilityTexture;
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

SRay GenerateRay ( void )
{
#ifdef PHOTON_MAP
	vec3 direction = vec3(0.0,-1.0,0.0) + texture2DRect(PhotonEmissionDirectionsTexture, vec2((gl_TexCoord[0].x+1)*(PhotonMapSize.x/2), (gl_TexCoord[0].y+1)*(PhotonMapSize.y/2)));
	vec3 position = texture2DRect(RectangleLightPointsTexture, vec2((gl_TexCoord[0].x+1)*(PhotonMapSize.x/2), (gl_TexCoord[0].y+1)*(PhotonMapSize.y/2)));
	position.x *= RectangleLight.Width/2.0;
	position.z *= RectangleLight.Length/2.0;
	//gl_FragColor = vec4 ( direction, 0.0 );
	position.y = Light.Position.y-0.01;
	return SRay ( position, normalize ( direction ) );
	
	
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

			vec3 position = texture2DRect ( PhotonTexture,	vec2 ( mod ( center, PhotonMapSize.x ), floor ( center / PhotonMapSize.y ))).xyz; 
		

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

void SetIntersection(SRay ray, inout SIntersection intersect, vec3 normal, vec3 color, vec4 material, float test)
{
	intersect.Time = test;
	intersect.Point = ray.Origin + ray.Direction * test;
	intersect.Normal = normal;

	#ifndef PHOTON_MAP
		intersect.Color = color;//Bottom
		intersect.Material = material;
	#endif
}



bool Raytrace ( SRay ray, float start, float final, inout SIntersection intersect, out bool refract )
{
	bool result = false;
	float test = BIG;

	refract = false;

	if ( IntersectPlane ( ray, AxisY, BoxMinimum.y, start, final, test ) && test < intersect.Time )
	{
		SetIntersection(ray, intersect, AxisY, DefaultWallsColor, WallMaterial, test);

		refract = false;
		result = true;
	}

	if ( IntersectPlane ( ray, AxisY, BoxMaximum.y, start, final, test ) && test < intersect.Time )
	{
		vec3 point = ray.Origin + ray.Direction * test;

			if ((point.x<(RectangleLight.Center.x + RectangleLight.Width/2))
			  &&(point.x>(RectangleLight.Center.x - RectangleLight.Width/2))
			  &&(point.z<(RectangleLight.Center.y + RectangleLight.Length/2))
			  &&(point.z>(RectangleLight.Center.y - RectangleLight.Length/2)))
				SetIntersection(ray, intersect, -AxisY, RectangleLight.Color, WallMaterial, test);
			else
				SetIntersection(ray, intersect, -AxisY, DefaultWallsColor, WallMaterial, test);

		refract = false;
		result = true;
	}

	
	if ( IntersectPlane ( ray, AxisX, BoxMinimum.x, start, final, test ) && test < intersect.Time )
	{
		SetIntersection(ray, intersect, AxisX, LeftWallColor, WallMaterial, test);

		refract = false;
		result = true;
	}

	if ( IntersectPlane ( ray, AxisX, BoxMaximum.x, start, final, test ) && test < intersect.Time )
	{
		SetIntersection(ray, intersect, -AxisX, RightWallColor, WallMaterial, test);
		
		result = true;
		refract = false;
	}

	if ( IntersectPlane ( ray, AxisZ, BoxMinimum.z, start, final, test ) && test < intersect.Time )
	{
		SetIntersection(ray, intersect, AxisZ, DefaultWallsColor, WallMaterial, test);

		refract = false;
		result = true;
	}
	
	if ( IntersectPlane ( ray, AxisZ, BoxMaximum.z, start, final, test ) && test < intersect.Time )
	{
		SetIntersection(ray, intersect, -AxisZ, DefaultWallsColor, WallMaterial, test);

		refract = false;
		result = true;
	}

	if ( IntersectSphere ( ray, start, final, test, GlassSphere ) && test < intersect.Time )
	{
		vec3 normal = normalize ( (ray.Origin + ray.Direction * test) - GlassSphere.Center ); //Intersect.Point - GlassSphere.Center
		SetIntersection(ray, intersect, normal, GlassColor, GlassMaterial, test);

		refract = true;
		result = true;
	}

	if ( IntersectSphere ( ray, start, final, test, MatSphere ) && test < intersect.Time )
	{
		vec3 normal = normalize ( ray.Origin + ray.Direction * test - MatSphere.Center );
		SetIntersection(ray, intersect, normal, MatColor,MatMaterial, test);
		
		refract = false;
		result = true;
	}

	return result;
}

#ifdef PHOTON_MAP
	float[3] PhotonProbabilitiesInitialization()
	{
		float[3] probabilities;
		vec3 p = texture2DRect(RandomProbabilityTexture, vec2((gl_TexCoord[0].x+1)*(PhotonMapSize.x/2), (gl_TexCoord[0].y+1)*(PhotonMapSize.y/2)));
		probabilities[0] = p.x;
		probabilities[1] = p.y;
		probabilities[2] = p.z;

		return probabilities;
	}

	vec3[3] PhotonReflectionDirectionsInitialization()
	{
		vec3[3] directions;
		directions[0] = texture2DRect(PhotonRefletionDirectionsTexture1, vec2((gl_TexCoord[0].x+1)*(PhotonMapSize.x/2), (gl_TexCoord[0].y+1)*(PhotonMapSize.y/2)));
		directions[1] = texture2DRect(PhotonRefletionDirectionsTexture2, vec2((gl_TexCoord[0].x+1)*(PhotonMapSize.x/2), (gl_TexCoord[0].y+1)*(PhotonMapSize.y/2)));
		directions[2] = texture2DRect(PhotonRefletionDirectionsTexture3, vec2((gl_TexCoord[0].x+1)*(PhotonMapSize.x/2), (gl_TexCoord[0].y+1)*(PhotonMapSize.y/2)));

		return directions;
	}
#endif

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

	bool trace = true;
	bool air = true;

	int depth = 2;
	int rayCount = 0;



	while ((rayCount<=depth)&&trace)
		if ( Raytrace ( ray, EPSILON, final, intersect, trace ) )
		{
			#ifndef PHOTON_MAP
				color += Phong ( intersect );
			#else
				
			#endif

			

			if (trace&&(rayCount<depth))
			{
				vec3 refractDirection = reflect ( ray.Direction, intersect.Normal);

				air = !air; 
				ray = SRay ( intersect.Point, refractDirection );
				final = IntersectBox ( ray, BoxMinimum, BoxMaximum ); 
				intersect.Time = BIG;
			}

			rayCount++;
		}

	#ifndef PHOTON_MAP
		if (rayCount > 0)
			Caustic ( intersect, color );
	#endif

	#ifdef PHOTON_MAP
		gl_FragColor = vec4 ( intersect.Point, 0.0 );
	#else
		//gl_FragColor = texture2DRect(PhotonTexture, vec2((gl_TexCoord[0].x+1)*400, (gl_TexCoord[0].y+1)*400));
		gl_FragColor = vec4 ( color, 1.0 );
	#endif
}