/// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

////////////////////////////////////////////////////////////////////////////////
//                 S C E N E R Y   O B J E C T   S H A D E R                  //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float4x4 World;         // model -> world
float4x4 View;          // world -> view
float4x4 Projection;    // view -> projection
float4x4 LightViewProjectionShadowProjection0;  // world -> light view -> light projection -> shadow map projection
float4x4 LightViewProjectionShadowProjection1;
float4x4 LightViewProjectionShadowProjection2;
float4x4 LightViewProjectionShadowProjection3;
texture  ShadowMapTexture0;
texture  ShadowMapTexture1;
texture  ShadowMapTexture2;
texture  ShadowMapTexture3;
float4   ShadowMapLimit;
float4   ZBias_Lighting;  // x = z-bias, y = diffuse, z = specular, w = step(1, z)
float4   Fog;  // rgb = color of fog; a = reciprocal of distance from camera, everything is
			   // normal color; FogDepth = FogStart, i.e. FogEnd = 2 * FogStart.
float4   LightVector_ZFar;  // xyz = direction vector to sun (world), w = z-far distance
float4   HeadlightPosition;     // xyz = position; w = lighting fading.
float4   HeadlightDirection;    // xyz = normalized direction (length = distance to light); w = 0.5 * (1 - min dot product).
float    HeadlightRcpDistance;  // reciprocal length = reciprocal distance to light
float4   HeadlightColor;        // rgba = color
float2   Overcast;      // Lower saturation & brightness when overcast. x = FullBrightness, y = HalfBrightness
float3   ViewerPos;     // Viewer's world coordinates.
float    ImageTextureIsNight;
float    NightColorModifier;
float    HalfNightColorModifier;
float    VegetationAmbientModifier;
float    SignalLightIntensity;
float4   EyeVector;
float3   SideVector;
float    ReferenceAlpha;
texture  ImageTexture;
texture  OverlayTexture;
float	 OverlayScale;
float	 MaxShadowBrightness;
float	 GroundLevel;
float 	 HeightFalloff;
float	 GlobalDensity;

#define MAX_HEADLIGHTS 16

// Globální proměnné pro více světel 
float4 HeadlightPositions[MAX_HEADLIGHTS];
float4 HeadlightDirections[MAX_HEADLIGHTS];
float4 HeadlightColors[MAX_HEADLIGHTS];
float  HeadlightRcpDistances[MAX_HEADLIGHTS];
int    ActiveHeadlightCount;

sampler Image = sampler_state
{
	Texture = (ImageTexture);
	MagFilter = Anisotropic;
	MinFilter = Anisotropic;
	MipFilter = Anisotropic;
	MaxAnisotropy = 16;
};

sampler Overlay = sampler_state
{
	Texture = (OverlayTexture);
	MagFilter = Anisotropic;
	MinFilter = Anisotropic;
	MipFilter = Anisotropic;
	MipLodBias = 0;
	AddressU = Wrap;
	AddressV = Wrap;
	MaxAnisotropy = 16;
};

sampler ShadowMap0 = sampler_state
{
	Texture = (ShadowMapTexture0);
	MagFilter = Anisotropic;
	MinFilter = Anisotropic;
	MipFilter = Anisotropic;
	MaxAnisotropy = 16;
};

sampler ShadowMap1 = sampler_state
{
	Texture = (ShadowMapTexture1);
	MagFilter = Anisotropic;
	MinFilter = Anisotropic;
	MipFilter = Anisotropic;
	MaxAnisotropy = 16;
};

sampler ShadowMap2 = sampler_state
{
	Texture = (ShadowMapTexture2);
	MagFilter = Anisotropic;
	MinFilter = Anisotropic;
	MipFilter = Anisotropic;
	MaxAnisotropy = 16;
};

sampler ShadowMap3 = sampler_state
{
	Texture = (ShadowMapTexture3);
	MagFilter = Anisotropic;
	MinFilter = Anisotropic;
	MipFilter = Anisotropic;
	MaxAnisotropy = 16;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position  : POSITION;
	float2 TexCoords : TEXCOORD0;
	float3 Normal    : NORMAL;
	float4x4 Instance : TEXCOORD1;
};

struct VERTEX_INPUT_FOREST
{
	float4 Position  : POSITION;
	float2 TexCoords : TEXCOORD0;
	float3 Normal    : NORMAL;
};

struct VERTEX_INPUT_SIGNAL
{
	float4 Position  : POSITION;
	float2 TexCoords : TEXCOORD0;
	float4 Color     : COLOR0;
};

struct VERTEX_INPUT_TRANSFER
{
	float4 Position  : POSITION;
	float2 TexCoords : TEXCOORD0;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position     : POSITION;  // position x, y, z, w
	float4 RelPosition  : TEXCOORD0; // rel position x, y, z; position z
	float2 TexCoords    : TEXCOORD1; // tex coords x, y
	float4 Color        : COLOR0;    // color r, g, b, a
	float4 Normal_Light : TEXCOORD2; // normal x, y, z; light dot
	float4 LightDir_Fog : TEXCOORD3; // light dir x, y, z; fog fade
	float4 Shadow       : TEXCOORD4; // ps2<shadow map texture and depth x, y, z> ps3<abs position x, y, z, w>
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

void _VSNormalProjection(in VERTEX_INPUT In, inout VERTEX_OUTPUT Out)
{
	// Project position, normal and copy texture coords
	Out.Position = mul(mul(mul(In.Position, World), View), Projection);
	Out.RelPosition.xyz = mul(In.Position, World).xyz - ViewerPos;
	Out.RelPosition.w = Out.Position.z;
	Out.TexCoords.xy = In.TexCoords;
	Out.Normal_Light.xyz = normalize(mul(In.Normal, (float3x3)World).xyz);

	// Normal lighting (range 0.0 - 1.0)
	// Need to calc. here instead of _VSLightsAndShadows() to avoid calling it from VSForest(), where it has gone into pre-shader in Shaders.cs
	Out.Normal_Light.w = dot(Out.Normal_Light.xyz, LightVector_ZFar.xyz) * 0.5 + 0.5;		
}

void _VSSignalProjection(uniform bool Glow, in VERTEX_INPUT_SIGNAL In, inout VERTEX_OUTPUT Out)
{
	// Project position, normal and copy texture coords
	In.Position.z = 0.05;
	float3 relPos = (float3)mul(In.Position, World) - ViewerPos;
	// Position 5cm in front of signal.
	if (Glow) {
	// Position glow a further 1cm in front of the light.
		In.Position.z = 0.06;
		// The glow around signal lights scales according to distance; there is a cut-off which controls when the glow
		// starts, a scaling factor which determines how quickly it expands (logarithmically), and ZBias_Lighting.x is
		// an overall "glow power" control which determines the effectiveness of glow on any individual light. This is
		// used to have different glows in the day and night, and to prevent theatre boxes from glowing!
		
		const float GlowCutOffM = 0;
		const float GlowScalingFactor = 50;  
		
		In.Position.xy *= log(1.6 + max(0, length(relPos) - GlowCutOffM) / GlowScalingFactor) * ZBias_Lighting.x * 0.4;	
	}
	Out.Position = mul(mul(mul(In.Position, World), View), Projection);
	Out.RelPosition.xyz = relPos;
	Out.RelPosition.w = Out.Position.z;
	Out.TexCoords.xy = In.TexCoords;
	Out.Color = In.Color;	
}

void _VSTransferProjection(in VERTEX_INPUT_TRANSFER In, inout VERTEX_OUTPUT Out)
{
	// Project position, normal and copy texture coords
	Out.Position = mul(mul(mul(In.Position, World), View), Projection);
	Out.RelPosition.xyz = mul(In.Position, World).xyz - ViewerPos;
	Out.RelPosition.w = Out.Position.z;
	Out.TexCoords.xy = In.TexCoords;
	Out.Normal_Light.w = 1;
}

void _VSLightsAndShadows(in float4 InPosition, inout VERTEX_OUTPUT Out)
{
	// Headlight lighting
	Out.LightDir_Fog.xyz = mul(InPosition, World).xyz - HeadlightPosition.xyz;

	// Fog fading
	float Haze_scenery = 2.05; 
	float Haze_horizon = 2.35;
	float MaxDim;
	
	//Přidá opar do světa, pokud je jasno
	if (Overcast.x < 0.40)
		{
			MaxDim = (1.40 - Overcast.x);
			Haze_scenery = Haze_scenery * MaxDim;
			if (Haze_scenery > 2.15) Haze_scenery = 2.15;
		}	
	else
	if (Overcast.x > 0.60)
		{
			MaxDim = (1.60 - Overcast.x);
			Haze_scenery = Haze_scenery * MaxDim;
			if (Haze_scenery < 2.0) Haze_scenery = 2.0;
		}	
	
	if (Fog.a > 0.1)
	{
		Haze_scenery = 2.0;
		Haze_horizon = 1.0;
	}
	
	Out.LightDir_Fog.w = (Haze_scenery / (1.0 + exp(length(Out.Position.xyz) * Haze_horizon * Fog.a * -2.0))) - 1.0;
	if (Out.LightDir_Fog.w > 1.025) Out.LightDir_Fog.w = 1.025;

	// Absolute position for shadow mapping
	Out.Shadow = mul(InPosition, World);	
}

VERTEX_OUTPUT VSGeneral(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	if (determinant(In.Instance) != 0) {
		In.Position = mul(In.Position, transpose(In.Instance));
		In.Normal = mul(In.Normal, (float3x3)transpose(In.Instance));
	}

	_VSNormalProjection(In, Out);
	_VSLightsAndShadows(In.Position, Out);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;
	
	return Out;
}
VERTEX_OUTPUT VSTransfer(in VERTEX_INPUT_TRANSFER In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSTransferProjection(In, Out);
	_VSLightsAndShadows(In.Position, Out);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x) / 1000;

	return Out;
}

VERTEX_OUTPUT VSTerrain(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSNormalProjection(In, Out);
	_VSLightsAndShadows(In.Position, Out);
	return Out;
}

VERTEX_OUTPUT VSForest(in VERTEX_INPUT_FOREST In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	// Start with the three vectors of the view.
	float3 upVector = float3(0, -1, 0); // This constant is also defined in Shareds.cs

	// Move the vertex left/right/up/down based on the normal values (tree size).
	float3 newPosition = (float3)In.Position;
	newPosition += (In.TexCoords.x - 0.5f) * SideVector * In.Normal.x;
	newPosition += (In.TexCoords.y - 1.0f) * upVector * In.Normal.y;
	In.Position = float4(newPosition, 1);

	// Project vertex with fixed w=1 and normal=eye.
	Out.Position = mul(mul(mul(In.Position, World), View), Projection);
	Out.RelPosition.xyz = mul(In.Position, World).xyz - ViewerPos;
	Out.RelPosition.w = Out.Position.z;
	Out.TexCoords.xy = In.TexCoords;
	Out.Normal_Light = EyeVector;

	_VSLightsAndShadows(In.Position, Out);

	return Out;
}

VERTEX_OUTPUT VSSignalLight(in VERTEX_INPUT_SIGNAL In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSSignalProjection(false, In, Out);
	return Out;
}

VERTEX_OUTPUT VSSignalLightGlow(in VERTEX_INPUT_SIGNAL In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSSignalProjection(true, In, Out);
	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

// Gets the ambient light effect.
float _PSGetAmbientEffect(in VERTEX_OUTPUT In)
{
	return In.Normal_Light.w * ZBias_Lighting.y * 2.0;
}

// Gets the specular light effect.
float _PSGetSpecularEffect(in VERTEX_OUTPUT In)
{
	float3 halfVector = normalize(-In.RelPosition.xyz) + LightVector_ZFar.xyz;
	return In.Normal_Light.w * ZBias_Lighting.w * pow(saturate(dot(In.Normal_Light.xyz, normalize(halfVector))), ZBias_Lighting.z);
}

// Gets the shadow effect.
float3 _PSGetShadowEffect(in VERTEX_OUTPUT In)
{
	float depth = In.RelPosition.w;
	float3 rv = { 0, 0, 0 };
	if (depth < ShadowMapLimit.x) {
		float3 pos0 = mul(In.Shadow, LightViewProjectionShadowProjection0).xyz;
		rv = float3(tex2D(ShadowMap0, pos0.xy).xy, pos0.z);
	}
	else {
		if (depth < ShadowMapLimit.y) {
			float3 pos1 = mul(In.Shadow, LightViewProjectionShadowProjection1).xyz;
			rv = float3(tex2D(ShadowMap1, pos1.xy).xy, pos1.z);
		}
		else {
			if (depth < ShadowMapLimit.z) {
				float3 pos2 = mul(In.Shadow, LightViewProjectionShadowProjection2).xyz;
				rv = float3(tex2D(ShadowMap2, pos2.xy).xy, pos2.z);
			}
			else {
				if (depth < ShadowMapLimit.w) {
					float3 pos3 = mul(In.Shadow, LightViewProjectionShadowProjection3).xyz;
					rv = float3(tex2D(ShadowMap3, pos3.xy).xy, pos3.z);
				}
			}
		}
	}
	return rv;
}

void _PSApplyShadowColor(inout float3 Color, in VERTEX_OUTPUT In)
{
	float depth = In.RelPosition.w;
	if (depth < ShadowMapLimit.x) {
		Color.rgb *= 0.9;
		Color.r += 0.1;
	}
	else {
		if (depth < ShadowMapLimit.y) {
			Color.rgb *= 0.9;
			Color.g += 0.1;
		}
		else {
			if (depth < ShadowMapLimit.z) {
				Color.rgb *= 0.9;
				Color.b += 0.1;
			}
			else {
				if (depth < ShadowMapLimit.w) {
					Color.rgb *= 0.9;
					Color.rg += 0.1;
				}
			}
		}
	}
}

float _PSGetShadowEffect(uniform bool NormalLighting, in VERTEX_OUTPUT In)
{
	float3 moments;
	moments = _PSGetShadowEffect(In);

	bool not_shadowed = moments.z - moments.x < 0.000001;
	float E_x2 = moments.y;
	float Ex_2 = moments.x * moments.x;
	float variance = clamp(E_x2 - Ex_2, 0.0001, 1.0);
	float m_d = moments.z - moments.x;
	float p = pow(variance / (variance + m_d * m_d), 3000);
	if (NormalLighting)
		return saturate(not_shadowed + p) * saturate(In.Normal_Light.w * 5 - 2 );
	return saturate(not_shadowed + p);
}

// Gets the overcast color.
float3 _PSGetOvercastColor(in float4 Color, in VERTEX_OUTPUT In)
{
	// Value used to determine equivalent grayscale color.
	const float3 LumCoeff = float3(0.2125, 0.7154, 0.0721);
	
	float intensity = dot((float3)Color, LumCoeff);
	return lerp(intensity, Color.rgb, 1.0);	
}

// Applies the lighting effect of the train's headlights, including
// fade-in/fade-out animations.
void _PSApplyHeadlights(inout float3 Color, in float4 OriginalColor, in VERTEX_OUTPUT In)
{
    // Celkový násobič síly reflektorů (upravte podle potřeby, např. 2.0 až 5.0)
    const float INTENSITY_BOOST = 2.5;

    [unroll]
    for (int i = 0; i < MAX_HEADLIGHTS; i++)
    {
        float activeMask = (i < ActiveHeadlightCount) ? 1.0 : 0.0;

        float3 lightToSurfaceVec = In.Shadow.xyz - HeadlightPositions[i].xyz;
        float dist = length(lightToSurfaceVec);
        float3 headlightToSurface = normalize(lightToSurfaceVec);

        float coneDot = dot(headlightToSurface, HeadlightDirections[i].xyz);
        float cosOuterAngle = HeadlightDirections[i].w;

        // 1. Ořez úhlu kuželu
        float coneAttenuation = saturate((coneDot - cosOuterAngle) / (1.0 - cosOuterAngle));

        // 2. Zjemněný dopad podle normály (lerp zabrání nepřirozenému tmavnutí na plochém terénu)
        float rawNormal = saturate(dot(In.Normal_Light.xyz, -headlightToSurface));
        float normalAttenuation = lerp(0.4, 1.0, rawNormal); 

        // 3. Pomalejší pokles svítivosti se vzdáleností (odmocnina pro delší dosvit)
        float distFactor = saturate(1.0 - dist * HeadlightRcpDistances[i]);
        float distanceAttenuation = sqrt(distFactor); 

        // Výsledná intenzita s násobičem
        float shading = coneAttenuation * normalAttenuation * distanceAttenuation * activeMask * INTENSITY_BOOST;

        Color += (float3)OriginalColor * HeadlightColors[i].rgb * HeadlightColors[i].a * shading;
    }
}

// Applies distance fog to the pixel.
void _PSApplyFog(inout float3 Color, in VERTEX_OUTPUT In)
{
    float3 fogColor = Fog.rgb * 1.25;
    float dist = length(In.RelPosition.xyz);
    
    // --- OPRAVA: POUŽITÍ ViewerPos.y ---
    // In.RelPosition.y je relativní výška objektu vůči kameře.
    // Přičtením ViewerPos.y získáme absolutní výšku objektu v mapě.
    float worldY = In.RelPosition.y + ViewerPos.y;

    // Nastavení parametrů
    float groundLevel = GroundLevel;      // Výšková hladina, kde je mlha nejhustší
    float heightFalloff = HeightFalloff;   // Jak rychle mlha mizí s výškou (ladit dle potřeby)
    float globalDensity = Fog.a * GlobalDensity;

    // Výpočet útlumu podle světové výšky
    // max(..., 0) zajistí, že pod groundLevel zůstává mlha maximálně hustá
    float diff = max(worldY - groundLevel, 0.0);
    float heightFactor = exp(-diff * heightFalloff);
    
    // Výsledný faktor mlhy
    // Mlha je nyní závislá na vzdálenosti, ale její síla je "tlumena" výškou objektu
    float fogFactor = exp(-dist * globalDensity * heightFactor);
    float fogT = 1.0 - saturate(fogFactor);

    Color = lerp(Color, fogColor, fogT);
}


void _PSSceneryFade(inout float4 Color, in VERTEX_OUTPUT In)
{
	if (ReferenceAlpha < 0.01) Color.a = 1;
	Color.a *= saturate((LightVector_ZFar.w - length(In.RelPosition.xyz)) / 500);
}


float4 PSImageTransfer(uniform bool ClampTexCoords, in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.50;
 
	float4 Color = tex2D(Image, In.TexCoords.xy);
	if (ClampTexCoords) {
		// We need to clamp the rendering to within the [0..1] range only.
		if (saturate(In.TexCoords.x) != In.TexCoords.x || saturate(In.TexCoords.y) != In.TexCoords.y) {
			Color.a = 0;
		}
	}
	// Alpha testing:
	clip(Color.a - ReferenceAlpha);

	// Ambient and shadow effects apply first; night-time textures cancel out all normal lighting.
	if (Fog.a != 0) MaxShadowBrightness = Fog.a * 1000 * 1.0;	
	MaxShadowBrightness = clamp(MaxShadowBrightness, 0.30, 1.0);

	float3 litColor = Color.rgb * lerp(MaxShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) * _PSGetShadowEffect(true, In) + ImageTextureIsNight));

	// Specular effect next.
	litColor += _PSGetSpecularEffect(In) * _PSGetShadowEffect(true, In);
	
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	if (Overcast.x != 0) MaxShadowBrightness = Overcast.x * 1.5;	
	MaxShadowBrightness = clamp(MaxShadowBrightness, 0.30, 1.0);
		
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), MaxShadowBrightness);

	// Night-time darkens everything, except night-time textures.	
	
	litColor *= NightColorModifier;
	
	//Ubere světlo, pokud je mlha 
	float MaxDim1 = 0;
	if (Fog.a > 0) MaxDim1 = Fog.a * 1000 * 0.1;
	if (MaxDim1 > 1.0) MaxDim1 = 1.0;
	
	float MaxDim2 = 0;
	if (Overcast.x > 0.0) MaxDim2 = Overcast.x * 1.0;
	if (MaxDim2 > 1.2) MaxDim2 = 1.2;	

	float MaxDim3;
	MaxDim3 = MaxDim1 + MaxDim2;
	if (MaxDim3 > 2.6) MaxDim3 = 2.6;

	float AmbientLightCoef = 1.0;
	if (_PSGetAmbientEffect(In) > 0.6) AmbientLightCoef = 0.6 / _PSGetAmbientEffect(In);
	if (AmbientLightCoef < 0.7) AmbientLightCoef = 0.7;

	//Přidá světlo, pokud není mlha
	litColor.rgb *= 0.7 * (3.0 - MaxDim3) * AmbientLightCoef * 0.85;
	
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);

	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);

	//_PSApplyShadowColor(litColor, In);
	return float4(litColor, Color.a);
}



float4 PSImage(in VERTEX_OUTPUT In) : COLOR0
{
	return PSImageTransfer(false, In);
}

float4 PSTransfer(in VERTEX_OUTPUT In) : COLOR0
{
	return PSImageTransfer(true, In);
}

float4 PSVegetation(in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;

	float4 Color = tex2D(Image, In.TexCoords.xy);
	
	// Alpha testing:
	clip(Color.a - ReferenceAlpha);
	
	// Ambient effect applies first; night-time textures cancel out all normal lighting.
	if (Fog.a != 0) MaxShadowBrightness = Fog.a * 1000 * 1.0;	
	MaxShadowBrightness = clamp(MaxShadowBrightness, 0.5, 1.0);
	
	float3 litColor = Color.rgb * lerp(MaxShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) * _PSGetShadowEffect(true, In) + ImageTextureIsNight));
	
	// No Specular effect next.	
	
	// Overcast blanks out ambient, shadow effects (so use original Color).
	if (Overcast.x != 0) MaxShadowBrightness = Overcast.x * 1.5;	
	MaxShadowBrightness = clamp(MaxShadowBrightness, 0.5, 1.0);
	
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), MaxShadowBrightness);

	// Night-time darkens everything, except night-time textures.	
	litColor *= NightColorModifier;
	
	//Ubere světlo, pokud je mlha 
	float MaxDim1 = 0;
	if (Fog.a > 0) MaxDim1 = Fog.a * 1000 * 0.1;
	if (MaxDim1 > 1.0) MaxDim1 = 1.0;
	
	float MaxDim2 = 0;
	if (Overcast.x > 0.0) MaxDim2 = Overcast.x * 1.0;
	if (MaxDim2 > 1.2) MaxDim2 = 1.2;	

	float MaxDim3;
	MaxDim3 = MaxDim1 + MaxDim2;
	if (MaxDim3 > 2.6) MaxDim3 = 2.6;

	float AmbientLightCoef = 1.0;
	if (_PSGetAmbientEffect(In) > 0.6) AmbientLightCoef = 0.6 / _PSGetAmbientEffect(In);
	if (AmbientLightCoef < 0.7) AmbientLightCoef = 0.7;

	//Přidá světlo, pokud není mlha
	litColor.rgb *= 0.7 * (3.0 - MaxDim3) * AmbientLightCoef * 0.70;	

	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);

	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);

	//_PSApplyShadowColor(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSTerrain(in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.50;

	float4 Color = tex2D(Image, In.TexCoords.xy);

	// Ambient and shadow effects apply first; night-time textures cancel out all normal lighting.
	if (Fog.a != 0) MaxShadowBrightness = Fog.a * 1000 * 1.0;	
	MaxShadowBrightness = clamp(MaxShadowBrightness, 0.30, 1.0);
	
	float3 litColor = Color.rgb * lerp(MaxShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) * _PSGetShadowEffect(true, In) + ImageTextureIsNight));

	// No specular effect for terrain.

	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	if (Overcast.x != 0) MaxShadowBrightness = Overcast.x * 1.5;	
	MaxShadowBrightness = clamp(MaxShadowBrightness, 0.30, 1.0);
	
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), MaxShadowBrightness);

	// Night-time darkens everything, except night-time textures.
	
	litColor *= NightColorModifier;

	//Ubere světlo, pokud je mlha 
	float MaxDim1 = 0;
	if (Fog.a > 0) MaxDim1 = Fog.a * 1000 * 0.1;
	if (MaxDim1 > 1.0) MaxDim1 = 1.0;
	
	float MaxDim2 = 0;
	if (Overcast.x > 0.0) MaxDim2 = Overcast.x * 1.0;
	if (MaxDim2 > 1.2) MaxDim2 = 1.2;	

	float MaxDim3;
	MaxDim3 = MaxDim1 + MaxDim2;
	if (MaxDim3 > 2.6) MaxDim3 = 2.6;

		float AmbientLightCoef = 1.0;
	if (_PSGetAmbientEffect(In) > 0.6) AmbientLightCoef = 0.6 / _PSGetAmbientEffect(In);
	if (AmbientLightCoef < 0.7) AmbientLightCoef = 0.7;

	//Přidá světlo, pokud není mlha
	litColor.rgb *= (float3)tex2D(Overlay, In.TexCoords.xy * OverlayScale) * (3.0 - MaxDim3) * AmbientLightCoef * 1.0;

	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);

	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);

	//_PSApplyShadowColor(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSDarkShade(in VERTEX_OUTPUT In) : COLOR0
{
	const float ShadowBrightness = 0.5;

	float4 Color = tex2D(Image, In.TexCoords.xy);

	// Alpha testing:
	clip(Color.a - ReferenceAlpha);

	// Fixed ambient and shadow effects at darkest level.
	float3 litColor = Color.rgb * ShadowBrightness;

	// No specular effect for dark shade.

	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	if (Overcast.x != 0) MaxShadowBrightness = Overcast.x * 1.50;
	if (MaxShadowBrightness > 0.5) MaxShadowBrightness = 0.5;

	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), MaxShadowBrightness);

	// Night-time darkens everything, except night-time textures.
	
	litColor *= NightColorModifier;

	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);

	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);
	return float4(litColor, Color.a);
}

float4 PSHalfBright(in VERTEX_OUTPUT In) : COLOR0
{
	const float HalfShadowBrightness = 0.75;

	float4 Color = tex2D(Image, In.TexCoords.xy);

	// Alpha testing:
	clip(Color.a - ReferenceAlpha);

	// Fixed ambient and shadow effects at mid-dark level.
	float3 litColor = Color.rgb * HalfShadowBrightness;

	// No specular effect for half-bright.

	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	if (Overcast.y != 0) MaxShadowBrightness = Overcast.y * 1.50;
	if (MaxShadowBrightness > 0.75) MaxShadowBrightness = 0.75;

	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), MaxShadowBrightness);

	// Night-time darkens everything, except night-time textures.
	litColor *= HalfNightColorModifier;

	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);

	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);
	return float4(litColor, Color.a);
}

float4 PSFullBright(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = tex2D(Image, In.TexCoords.xy);

	// Alpha testing:
	clip(Color.a - ReferenceAlpha);

	// Fixed ambient and shadow effects at brightest level.
	float3 litColor = Color.rgb;

	// No specular effect for full-bright.
	// No overcast effect for full-bright.
	// No night-time effect for full-bright.

	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);

	// And fogging is last.
	_PSApplyFog(litColor, In);
	_PSSceneryFade(Color, In);
	return float4(litColor, Color.a);
}

float4 PSSignalLight(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = tex2D(Image, In.TexCoords.xy);	

	// Alpha testing:
	clip(Color.a - ReferenceAlpha);

	// No ambient and shadow effects for signal lights.

	// Apply signal coloring effect.
	float3 litColor = lerp(Color.rgb * 0.25, In.Color.rgb, Color.r);

	// Aplikace mlhy na světlo
    _PSApplyFog(litColor, In);

	litColor = lerp(litColor, In.Color.rgb, clamp(0.025 / NightColorModifier, 0.025, 0.5));

	// No specular effect, overcast effect, night-time darkening, headlights effect for signal lights.
	return float4(litColor, Color.a * SignalLightIntensity);
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// IMPORTANT: ATI graphics cards/drivers do NOT like mixing shader model      //
//            versions within a technique/pass. Always use the same vertex    //
//            and pixel shader versions within each technique/pass.           //
////////////////////////////////////////////////////////////////////////////////

technique ImagePS {
	pass Pass_0 {
		VertexShader = compile vs_5_0 VSGeneral();
		PixelShader = compile ps_5_0 PSImage();
	}
}


technique TransferPS {
	pass Pass_0 {
		VertexShader = compile vs_5_0 VSTransfer();
		PixelShader = compile ps_5_0 PSTransfer();
	}
}

technique Forest {
	pass Pass_0 {
		VertexShader = compile vs_5_0 VSForest();
		PixelShader = compile ps_5_0 PSVegetation();
	}
}

technique VegetationPS {
	pass Pass_0 {
		VertexShader = compile vs_5_0 VSGeneral();
		PixelShader = compile ps_5_0 PSVegetation();
	}
}

technique TerrainPS {
	pass Pass_0 {
		VertexShader = compile vs_5_0 VSTerrain();
		PixelShader = compile ps_5_0 PSTerrain();
	}
}

technique DarkShadePS {
	pass Pass_0 {
		VertexShader = compile vs_5_0 VSGeneral();
		PixelShader = compile ps_5_0 PSDarkShade();
	}
}

technique HalfBrightPS {
	pass Pass_0 {
		VertexShader = compile vs_5_0 VSGeneral();
		PixelShader = compile ps_5_0 PSHalfBright();
	}
}

technique FullBrightPS {
	pass Pass_0 {
		VertexShader = compile vs_5_0 VSGeneral();
		PixelShader = compile ps_5_0 PSFullBright();
	}
}

technique SignalLight {
	pass Pass_0 {
		VertexShader = compile vs_5_0 VSSignalLight();
		PixelShader = compile ps_5_0 PSSignalLight();
	}
}

technique SignalLightGlow {
	pass Pass_0 {
		VertexShader = compile vs_5_0 VSSignalLightGlow();
		PixelShader = compile ps_5_0 PSSignalLight();
	}
}
