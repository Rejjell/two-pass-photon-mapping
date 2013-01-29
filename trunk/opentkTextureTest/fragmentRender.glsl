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
	vec4 Material;
	#ifndef PHOTON_MAP
		vec3 Color;
		
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

const float GlassAirIndex = 4.5;							
const float AirGlassIndex = 1.0 / GlassAirIndex;			

const vec3 GlassColor = vec3 ( 0.0, 1.0, 0.0 );		
const vec3 MatColor = vec3 ( 0.0, 0.0, 1.0 );
const vec3 RightWallColor = vec3 (1.0,0.0,0.0);
const vec3 LeftWallColor = vec3 (0.0,1.0,0.0);
const vec3 DefaultWallsColor = vec3 (1.0,0.6,0.25);

const vec4 GlassMaterial = vec4 ( 0.1, 0.1, 0.6, 128.0 );
const vec4 MatMaterial = vec4 ( 0.1, 1.0, 0.05, 8.0 );	
const vec4 WallMaterial = vec4 ( 0.1, 0.8, 0.1, 32.0 );	
const vec4 ReflectiveWallMaterial = vec4 ( 0.1, 0.5, 0.5, 128.0 );	
const vec3 Zero = vec3 ( 0.0, 0.0, 0.0 );
const vec3 Unit = vec3 ( 1.0, 1.0, 1.0 );

const vec3 AxisX = vec3 ( 1.0, 0.0, 0.0 );
const vec3 AxisY = vec3 ( 0.0, 1.0, 0.0 );
const vec3 AxisZ = vec3 ( 0.0, 0.0, 1.0 );

#define BIG 1000000.0
#define EPSILON 0.01
#define PI 3.14159265

uniform vec3 BoxMinimum;					
uniform vec3 BoxMaximum;					
						
uniform SSphere GlassSphere;
uniform SSphere MatSphere;	
uniform RectangleLightStruct RectangleLight;		

uniform vec2 PhotonMapSize;

#ifndef PHOTON_MAP
	uniform SCamera Camera;			
	uniform float PhotonIntensity;	
	uniform float Delta;			
	uniform float InverseDelta;	
				
	uniform sampler2DRect PhotonTexture;
	uniform sampler2DRect CausticTexture;
	uniform sampler2DRect RectangleLightPointsPhongTexture;

	uniform float PhotonTextureSize;
	uniform float CausticTextureSize;


	const float ReflectRefractCoef = 0.5;

#else
	uniform sampler2DRect PhotonEmissionDirectionsTexture;
	uniform sampler2DRect PhotonReflectionDirectionsTexture1;
	uniform sampler2DRect PhotonReflectionDirectionsTexture2;
	uniform sampler2DRect PhotonReflectionDirectionsTexture3;
	uniform sampler2DRect RectangleLightPointsTexture;
	uniform sampler2DRect RandomProbabilityTexture;

	out vec3 Photon;
	out vec3 CausticPhoton;
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

bool IntersectSphere ( SRay ray, float start, float final, out float time, SSphere Sphere, bool outside)
{
	ray.Origin -= Sphere.Center;
	float A = dot ( ray.Direction, ray.Direction );
	float B = dot ( ray.Direction, ray.Origin );
	float C = dot ( ray.Origin, ray.Origin ) - Sphere.Radius * Sphere.Radius;
	float D = B * B - A * C;
	
	if ( D > 0.0 )
	{
		D = sqrt ( D );
		float x1 = (-B - D)/A;
		float x2 = (-B + D)/A;
		if (outside)
			time = min ( max ( 0.0, ( -B - D ) / A ), ( -B + D ) / A );
		else
			time = max ( 0.0, ( -B + D ) / A );
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
	position.y = 5.0-0.001;
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
	vec3 PhongPointLight ( SIntersection intersect/*, vec3 pointLightPosition */)
	{
		vec3 pointLightPosition = texture2DRect(RectangleLightPointsPhongTexture, vec2((gl_TexCoord[0].x+1)*400, (gl_TexCoord[0].y+1)*400)).xyz;
		pointLightPosition.x *= 0.5;
		pointLightPosition.y = 5.0;
		pointLightPosition.z *= 0.5;
		vec3 light = normalize ( pointLightPosition - intersect.Point );
		vec3 view = normalize ( Camera.Position - intersect.Point );
		float diffuse = max ( dot ( light, intersect.Normal ), 0.1 );
		vec3 reflection = reflect ( -view, intersect.Normal );
		float specular = pow ( max ( dot ( reflection, light ), 0.1 ), intersect.Material.w );

		vec3 point=intersect.Point;

		if ((point.y<=5.01)&&(point.y>=4.99))  diffuse = 0.5 ;
		if ((point.x<(RectangleLight.Center.x + RectangleLight.Width/2))
			  &&(point.x>(RectangleLight.Center.x - RectangleLight.Width/2))
			  &&(point.z<(RectangleLight.Center.y + RectangleLight.Length/2))
			  &&(point.z>(RectangleLight.Center.y - RectangleLight.Length/2)))
				diffuse = 1;


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

	float BinSearch ( vec3 point, sampler2DRect tex, float texSize )
	{
		float left = 0.0;
		float right = texSize * texSize;
		float center;

		while ( left < right )
		{
			center = 0.5 * ( left + right );
			vec3 position = texture2DRect ( tex,	vec2 ( mod ( center, texSize ), floor ( center / texSize ))).xyz; 
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

	void PhotonGathering ( SIntersection intersect, inout vec3 color, float reflectionInfluence )
	{
		float left = BinSearch ( intersect.Point - vec3 ( Delta ), PhotonTexture, PhotonTextureSize );
		float right = BinSearch ( intersect.Point + vec3 ( Delta ), PhotonTexture, PhotonTextureSize );
	
		for ( float i = left; i <= right; i++ )
		{
			vec4 photon = texture2DRect (
				PhotonTexture, vec2 ( mod ( i, PhotonTextureSize ), floor ( i / PhotonTextureSize ) ) );
		
			if (photon.xyz != vec3(100.0, 100.0, 100.0))
			color +=
				max ( 0.0, 1.0 - InverseDelta * length ( photon.xyz - intersect.Point ) ) * PhotonIntensity * reflectionInfluence;
		}

	}

	void CausticGathering ( SIntersection intersect, inout vec3 color, float reflectionInfluence )
	{
		float left = BinSearch ( intersect.Point - vec3 ( Delta ), CausticTexture, CausticTextureSize );
		float right = BinSearch ( intersect.Point + vec3 ( Delta ), CausticTexture, CausticTextureSize );
	
		for ( float i = left; i <= right; i++ )
		{
			vec4 photon = texture2DRect (
				CausticTexture, vec2 ( mod ( i, CausticTextureSize ), floor ( i / CausticTextureSize ) ) );
			
			if (photon.xyz != vec3(100.0, 100.0, 100.0))
			color +=
				max ( 0.0, 1.0 - InverseDelta * length ( photon.xyz - intersect.Point ) ) * PhotonIntensity * reflectionInfluence;
		}

	}

#endif

void SetIntersection(SRay ray, inout SIntersection intersect, vec3 normal, vec3 color, vec4 material, float test)
{
	intersect.Time = test;
	intersect.Point = ray.Origin + ray.Direction * test;
	intersect.Normal = normal;
	intersect.Material = material;

	#ifndef PHOTON_MAP
		intersect.Color = color;//Bottom
		
	#endif
}



bool Raytrace ( SRay ray, float start, float final, inout SIntersection intersect, out bool reflection, out bool refraction)
{
	bool result = false;
	float test = BIG;

	reflection = false;
	refraction = false;

	if ( IntersectPlane ( ray, AxisY, BoxMinimum.y, start, final, test ) && test < intersect.Time )
	{
		SetIntersection(ray, intersect, AxisY, DefaultWallsColor, WallMaterial, test);

		refraction = false;
		reflection = false;
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

		refraction = false;
		reflection = false;
		result = true;
	}

	
	if ( IntersectPlane ( ray, AxisX, BoxMinimum.x, start, final, test ) && test < intersect.Time )
	{
		SetIntersection(ray, intersect, AxisX, LeftWallColor, ReflectiveWallMaterial, test);

		refraction = false;
		reflection = true;
		result = true;
	}

	if ( IntersectPlane ( ray, AxisX, BoxMaximum.x, start, final, test ) && test < intersect.Time )
	{
		SetIntersection(ray, intersect, -AxisX, RightWallColor, ReflectiveWallMaterial, test);
		
		result = true;
		reflection = true;
		refraction = false;
	}

	if ( IntersectPlane ( ray, AxisZ, BoxMinimum.z, start, final, test ) && test < intersect.Time )
	{
		SetIntersection(ray, intersect, AxisZ, DefaultWallsColor, WallMaterial, test);

		refraction = false;
		reflection = false;
		result = true;
	}
	
	if ( IntersectPlane ( ray, AxisZ, BoxMaximum.z, start, final, test ) && test < intersect.Time )
	{
		SetIntersection(ray, intersect, -AxisZ, DefaultWallsColor, WallMaterial, test);

		refraction = false;
		reflection = false;
		result = true;
	}

	if ( IntersectSphere ( ray, start, final, test, GlassSphere, true ) && test < intersect.Time )
	{
		vec3 normal = normalize ( (ray.Origin + ray.Direction * test) - GlassSphere.Center );  //Intersect.Point - GlassSphere.Center
		SetIntersection(ray, intersect, normal, GlassColor, GlassMaterial, test);

		reflection = false;
		refraction = true;
		result = true;
	}

	if ( IntersectSphere ( ray, start, final, test, GlassSphere, false ) && test < intersect.Time )
	{
		vec3 normal = normalize ( (ray.Origin + ray.Direction * test) - GlassSphere.Center );  //Intersect.Point - GlassSphere.Center
		SetIntersection(ray, intersect, normal, GlassColor, GlassMaterial, test);

		reflection = false;
		refraction = true;
		result = true;
	}

	if ( IntersectSphere ( ray, start, final, test, MatSphere, true ) && test < intersect.Time )
	{
		vec3 normal = normalize ( (ray.Origin + ray.Direction * test) - MatSphere.Center );
		SetIntersection(ray, intersect, normal, MatColor,MatMaterial, test);
		
		refraction = false;
		reflection = false;
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
		directions[0] = texture2DRect(PhotonReflectionDirectionsTexture1, vec2((gl_TexCoord[0].x+1)*(PhotonMapSize.x/2), (gl_TexCoord[0].y+1)*(PhotonMapSize.y/2)));
		directions[1] = texture2DRect(PhotonReflectionDirectionsTexture2, vec2((gl_TexCoord[0].x+1)*(PhotonMapSize.x/2), (gl_TexCoord[0].y+1)*(PhotonMapSize.y/2)));
		directions[2] = texture2DRect(PhotonReflectionDirectionsTexture3, vec2((gl_TexCoord[0].x+1)*(PhotonMapSize.x/2), (gl_TexCoord[0].y+1)*(PhotonMapSize.y/2)));

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
		float[3] probabilities = PhotonProbabilitiesInitialization();
		vec3[3] directions = PhotonReflectionDirectionsInitialization();
		int depth = 3;
	#else
		vec3 color = Zero;
		vec3 mainColor = Zero;
		vec3 secondaryColor = Zero;
		int depth = 10;
	#endif

	bool continueTracing = true;
	bool refraction = false;
	bool reflection = false;
	bool caustic = false;
	bool air = true;
	int rayCount = 0;
	float reflectionInfluence = 1.0;

	do
	{
		if ( Raytrace ( ray, EPSILON, final, intersect, reflection, refraction ) )
		{
			rayCount++;
			if (!caustic)
				if (reflection || refraction)
					caustic = true;
			continueTracing = reflection || refraction;
			#ifndef PHOTON_MAP
				if (rayCount == 1)
					mainColor = PhongPointLight(intersect);
				else
					secondaryColor += PhongPointLight(intersect)*reflectionInfluence;
			#else
				if (!continueTracing)
					continueTracing = (probabilities[rayCount] < intersect.Material.z);
			#endif

			if (continueTracing&&(rayCount<=depth))
			{
				reflectionInfluence *= 0.5;

				vec3 direction;

				if (reflection)
					direction = reflect( ray.Direction, intersect.Normal);
				else
					if (refraction)
					{
						direction = refract ( ray.Direction,
										 mix ( -intersect.Normal, intersect.Normal, float ( air ) ),
										 mix ( GlassAirIndex, AirGlassIndex, float ( air ) ) );
						air = !air; 
					}
					#ifdef PHOTON_MAP
					else
						direction = intersect.Normal + directions[rayCount];
					#endif
				
				ray = SRay ( intersect.Point, direction );
				final = IntersectBox ( ray, BoxMinimum, BoxMaximum ); 
				intersect.Time = BIG;
			}
		}
		else
			continueTracing = false;
	}
	while ((rayCount<depth)&&continueTracing);

	#ifndef PHOTON_MAP
		if (rayCount > 0)
		{
			if (rayCount > 1)
				color = mix(mainColor, secondaryColor, ReflectRefractCoef);
			else
				color = mainColor;

			PhotonGathering( intersect, color, reflectionInfluence );
			CausticGathering( intersect, color, reflectionInfluence );
			
		}
	#endif

	#ifdef PHOTON_MAP
		if (caustic)
			CausticPhoton = intersect.Point;
		else
			Photon = intersect.Point;
	#else
			gl_FragColor = vec4 ( color, 1.0 );
	#endif
}