// COPYRIGHT 2010, 2011, 2013 by the Open Rails project.
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
//                     L I G H T   G L O W   S H A D E R                      //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float4x4 WorldViewProjection;  // model -> world -> view -> projection
float2   Fade;          // overall fade (0 = off, 1 = on); transition fade (0 = original, 1 = transition)
texture  LightGlowTexture;

float4 Fog;         // Barva (rgb) a hustota (a) 
float3 ViewerPos;   // Musí být předáno z aplikace (shodné se SceneryShader) 
float GroundLevel;
float HeightFalloff;
float GlobalDensity;
float GlowScale = 1.0;

// Hodnota 0.0 = pouze 1. textura, 1.0 = pouze 2. textura (Bulb)
float TextureBlend = 0.0;

// Druhá textura pro prolínání
texture LightGlowTexture2;

sampler LightGlowSampler = sampler_state
{
	Texture = (LightGlowTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Wrap;
};

sampler2D LightGlowSampler2 = sampler_state
{
    Texture = <LightGlowTexture2>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float3 PositionO        : POSITION0; // original position x, y, z
	float3 PositionT        : POSITION1; // transition position x, y, z
	float3 NormalO          : NORMAL0;   // original normal x, y, z
	float3 NormalT          : NORMAL1;   // transition normal x, y, z
	float4 ColorO           : COLOR0;    // original color r, g, b, a
	float4 ColorT           : COLOR1;    // transition color r, g, b, a
	float4 TexCoords_Radius : TEXCOORD0; // tex coords u, v; original radius; transition radius
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position  : POSITION0;
	float4 Color     : COLOR0;
	float2 TexCoords : TEXCOORD0;		
	float4 FogParams : TEXCOORD1;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

VERTEX_OUTPUT VSLightGlow(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	float radius = lerp(In.TexCoords_Radius.z, In.TexCoords_Radius.w, Fade.y) * GlowScale;
	float3 position = lerp(In.PositionO, In.PositionT, Fade.y);
	float3 normal = lerp(In.NormalO, In.NormalT, Fade.y);
    
	float3 upVector = float3(0, 1, 0);
	float3 sideVector = normalize(cross(upVector, normal));
	upVector = normalize(cross(sideVector, normal));
    
	position += (In.TexCoords_Radius.x - 0.5f) * sideVector * radius;
	position += (In.TexCoords_Radius.y - 0.5f) * upVector * radius;
    
	Out.Position = mul(WorldViewProjection, float4(position, 1));
	
	Out.Color = lerp(In.ColorO, In.ColorT, Fade.y);
	Out.Color.a *= Fade.x;
	Out.TexCoords = In.TexCoords_Radius.xy;

	// Výpočet dat pro mlhu 
	Out.FogParams.xyz = position - ViewerPos; // Relativní pozice ke kameře
	Out.FogParams.w = position.y;             // Absolutní výška světla

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSLightGlow(in VERTEX_OUTPUT In) : COLOR0
{
	float4 texColor = tex2D(LightGlowSampler, In.TexCoords.xy);
	float4 texColor2 = tex2D(LightGlowSampler2, In.TexCoords.xy);

	float4 finalColor = In.Color * lerp(texColor, texColor2, TextureBlend);

	// Parametry mlhy (laditelné) 
	float groundLevel = GroundLevel;     
	float heightFalloff = HeightFalloff;   
	float globalDensity = Fog.a * GlobalDensity;

	// Výpočet vzdálenosti a výškového útlumu 
	float dist = length(In.FogParams.xyz);
	float worldY = In.FogParams.w;
	float diff = max(worldY - groundLevel, 0.0);
	float heightFactor = exp(-diff * heightFalloff);
    
	// Faktor viditelnosti (1.0 = čisté, 0.0 = úplná mlha) 
	float fogFactor = exp(-dist * globalDensity * heightFactor);
	
	// ZMĚKČENÍ: Umocněním faktoru docílíme toho, že světlo v mlze ztratí ostrost
	float alphaSoftness = pow(saturate(fogFactor), 2);

	// Aplikace na barvu i průhlednost 
	//finalColor.rgb *= alphaSoftness;
	//finalColor.a *= alphaSoftness;
	
	return finalColor;
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// IMPORTANT: ATI graphics cards/drivers do NOT like mixing shader model      //
//            versions within a technique/pass. Always use the same vertex    //
//            and pixel shader versions within each technique/pass.           //
////////////////////////////////////////////////////////////////////////////////

technique LightGlow {
	pass Pass_0 {
		VertexShader = compile vs_5_0 VSLightGlow();
		PixelShader = compile ps_5_0 PSLightGlow();
	}
}
