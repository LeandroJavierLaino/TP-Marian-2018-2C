// ---------------------------------------------------------
// Ejemplo toon Shading
// ---------------------------------------------------------

/**************************************************************************************/
/* Variables comunes */
/**************************************************************************************/

//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

float3 eyePosition = float3(0.00, 0.00, -100.00);
float time = 0;
float global_alpha = 1;
float gl_k0;
float gl_k1;
float gl_k2;


//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
    Texture = (texDiffuseMap);
    ADDRESSU = MIRROR;
    ADDRESSV = MIRROR;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

float screen_dx; // tamaño de la pantalla en pixels
float screen_dy;

//Input del Vertex Shader
struct VS_INPUT
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float4 Color : COLOR;
    float2 Texcoord : TEXCOORD0;
};

texture g_RenderTarget;
sampler RenderTarget =
sampler_state
{
    Texture = <g_RenderTarget>;
    ADDRESSU = CLAMP;
    ADDRESSV = CLAMP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

texture g_RenderTarget2;
sampler RenderTarget2 =
sampler_state
{
    Texture = <g_RenderTarget2>;
    ADDRESSU = CLAMP;
    ADDRESSV = CLAMP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

texture g_RenderTarget3;
sampler RenderTarget3 =
sampler_state
{
    Texture = <g_RenderTarget3>;
    ADDRESSU = CLAMP;
    ADDRESSV = CLAMP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

texture g_RenderTarget4;
sampler RenderTarget4 =
sampler_state
{
    Texture = <g_RenderTarget4>;
    ADDRESSU = CLAMP;
    ADDRESSV = CLAMP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

texture g_RenderTarget5;
sampler RenderTarget5 =
sampler_state
{
    Texture = <g_RenderTarget5>;
    ADDRESSU = CLAMP;
    ADDRESSV = CLAMP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

//Output del Vertex Shader
struct VS_OUTPUT
{
    float4 Position : POSITION0;
    float2 Texcoord : TEXCOORD0;
    float3 Norm : TEXCOORD1; // Normales
    float3 Pos : TEXCOORD2; // Posicion real 3d
};

float3 cube_color = float3(1,0.5,0);

float hash(float n)
{
	return frac(sin(n)*43758.5453);
}

float noise(float3 x)
{
	float3 p = floor(x);
	float3 f = frac(x);
	f = f*f*(3.0 - 2.0*f);
	float n = p.x + p.y*57.0 + 113.0*p.z;
	return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
		lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
		lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
			lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
}


//Vertex Shader
VS_OUTPUT vs_main(VS_INPUT Input)
{
    VS_OUTPUT Output;

	//Proyectar posicion
    Output.Position = mul(Input.Position, matWorldViewProj);
   
	//Las Texcoord quedan igual
    Output.Texcoord = Input.Texcoord;

	// Calculo la posicion real
    float4 pos_real = mul(Input.Position, matWorld);
    Output.Pos = float3(pos_real.x, pos_real.y, pos_real.z);
   
	// Transformo la normal y la normalizo
	//Output.Norm = normalize(mul(Input.Normal,matInverseTransposeWorld));
    Output.Norm = normalize(mul(Input.Normal, matWorld));
    return (Output);
   
}

//Pixel Shader
float4 ps_main(float3 Texcoord : TEXCOORD0, float3 N : TEXCOORD1,
	float3 Pos : TEXCOORD2) : COLOR0
{
    float4 fvBaseColor = tex2D(diffuseMap, Texcoord);
	fvBaseColor.a *= global_alpha;
	if (fvBaseColor.a < 0.1)
		discard;
    return fvBaseColor;
}


technique DefaultTechnique
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 vs_main();
        PixelShader = compile ps_3_0 ps_main();
    }
}



VS_OUTPUT vs_fire(VS_INPUT Input)
{
    VS_OUTPUT Output = (VS_OUTPUT)0;

	//Proyectar posicion
    Output.Position = mul(Input.Position, matWorldViewProj);
   
	//Las Texcoord quedan igual
    Output.Texcoord = Input.Texcoord + float2(0,time*100);

    Output.Pos = Input.Position.xyz;
   
    return (Output);
   
}

//Pixel Shader
float4 ps_fire(float3 Texcoord : TEXCOORD0, float3 N : TEXCOORD1,
	float3 Pos : TEXCOORD2) : COLOR0
{
	float4 fvBaseColor = tex2D(diffuseMap, Texcoord);
	float k = 1.0 - (50.0 + Pos.x) / 100.0;
	fvBaseColor.a *= k;
	fvBaseColor.rgb += k*0.3;
	return fvBaseColor;
}

technique Fire
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 vs_fire();
        PixelShader = compile ps_3_0 ps_fire();
    }
}


//Pixel Shader
float4 ps_edge(float3 Texcoord : TEXCOORD0, float3 N : TEXCOORD1,
	float3 Pos : TEXCOORD2) : COLOR0
{

	float4 rta;
	float ep2 = 0.01;
	if (Texcoord.x < ep2 || Texcoord.y < ep2 || Texcoord.x>1 - ep2 || Texcoord.y >1 - ep2)
		rta = float4(1,1,1,1);
	else
	{
		float kda = clamp(N.x * 0.5 + 0.2, 0, 1);
		float3 base_color = cube_color* kda;
		float ep = 0.25;
		float sx = 1;
		if (Texcoord.x < ep)
			sx = 1 - (ep - Texcoord.x) / ep;
		else
			if (Texcoord.x > 1 - ep)
				sx = 1 - (Texcoord.x - 1 + ep) / ep;
		float sy = 1;
		if (Texcoord.y < ep)
			sy = 1 - (ep - Texcoord.y) / ep;
		else
			if (Texcoord.y > 1 - ep)
				sy = 1 - (Texcoord.y - 1 + ep) / ep;

		if (sx*sy > 0.95)
			discard;

		rta = float4(lerp(cube_color * 2, base_color, sx*sy), 1);
	}

	return rta;
	/*
	if (Texcoord.x>ep &&  Texcoord.y > ep && Texcoord.x<1 - ep &&  Texcoord.y <1 - ep)
		return float4(base_color, 0.5);
	else
		return float4(1, 1, 1, 1);
	*/


}


float4 ps_buildings(float3 Texcoord : TEXCOORD0, float3 N : TEXCOORD1,
	float3 Pos : TEXCOORD2) : COLOR0
{
	float kx = 1 - abs(1 - 2 * Texcoord.x);
	float ky = 1 - abs(1 - 2 * Texcoord.y);
	return float4(cube_color*(1+kx),pow(kx*ky , 1));

	//return float4(cube_color, 0.5);
}


float4 ps_solid(float3 Texcoord : TEXCOORD0, float3 N : TEXCOORD1,
	float3 Pos : TEXCOORD2) : COLOR0
{
	return float4(cube_color, 1);
}

float4 ps_terrain(float3 Texcoord : TEXCOORD0, float3 N : TEXCOORD1,
	float3 Pos : TEXCOORD2) : COLOR0
{
	float ep = 0.03;
	if (Texcoord.x > ep &&  Texcoord.y > ep && Texcoord.x < 1 - ep &&  Texcoord.y < 1 - ep)
	discard;

	return float4(1, 1,0, 1);
}

float4 ps_glow_ring(float3 Texcoord : TEXCOORD0, float3 Pos : TEXCOORD2) : COLOR0
{
	float2 xy = 2 * Texcoord.xy - float2(1,1);
	float ep = 0.3;
	float d = abs(length(xy)- 1 + ep);
	if (d> ep)
		discard;

	return float4(0,1,1, (ep-d) / ep);
}


float4 ps_glow_bar(float3 Texcoord : TEXCOORD0, float3 Pos : TEXCOORD2) : COLOR0
{
	float kx = 1 - abs(1 - 2 * Texcoord.x);
	float ky = 1 - abs(1 - 2 * Texcoord.y);
	return float4(cube_color,pow(kx*ky , 3));
}

float4 ps_glow_lines(float3 Texcoord : TEXCOORD0, float3 Pos : TEXCOORD2) : COLOR0
{
	float y = .5 + gl_k0 * cos((Texcoord.x + gl_k1*time)*gl_k2);
	float ky = 1.0 - 10.0*abs(y - Texcoord.y);
	return float4(cube_color,ky);
}

float4 ps_piso(float3 Texcoord : TEXCOORD0) : COLOR0
{
	float kx = Texcoord.x * 100;
	float ky = Texcoord.y * 100;
	float tx = fmod(kx, 10);
	float ty = fmod(ky, 30);
	if (tx > 5 && ty> 5)
		discard;

	float alpha = max((1 - abs(2 * tx / 5 - 1)), (1 - abs(2 * ty / 5 - 1)));
	return float4(cube_color, alpha);
}


technique EdgeCube
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_edge();
	}
}


technique Buildings
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_buildings();
	}
}


technique SolidColor
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_solid();
	}
}

technique GlowBar
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_glow_bar();
	}
}


technique Terrain
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_terrain();
	}
}

technique GlowRing
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_glow_ring();
	}
}

technique GlowLines
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_glow_lines();
	}
}

technique Piso
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_piso();
	}
}

void VSCopy(float4 vPos : POSITION, float2 vTex : TEXCOORD0, out float4 oPos : POSITION, out float2 oScreenPos : TEXCOORD0)
{
    oPos = vPos;
    oScreenPos = vTex;
    oPos.w = 1;
}

float4 PSFrameCombine(in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) : COLOR0
{
    return (tex2D(RenderTarget, Tex) + tex2D(RenderTarget2, Tex) + tex2D(RenderTarget3, Tex)
		+ +tex2D(RenderTarget4, Tex) + +tex2D(RenderTarget5, Tex)) * 0.2;
}


technique FrameMotionBlur
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 VSCopy();
        PixelShader = compile ps_3_0 PSFrameCombine();
    }
}

float4 PSFrameCopy(in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) : COLOR0
{
    return tex2D(RenderTarget, Tex);
}

technique FrameCopy
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 VSCopy();
        PixelShader = compile ps_3_0 PSFrameCopy();
    }
}


VS_OUTPUT vs_skybox(VS_INPUT input)
{
	VS_OUTPUT output = (VS_OUTPUT)0;
	float4 pos = input.Position;
	pos.xyz *= 10;
	pos.xyz += eyePosition;
	output.Position = mul(pos, matWorldViewProj).xyww;
	output.Texcoord = input.Texcoord;
	output.Pos = mul(pos, matWorld);
	return output;
}

float4 ps_skybox(float2 Texcoord : TEXCOORD0 ,float3 pos : TEXCOORD2) : COLOR0
{
	return tex2D(diffuseMap, float2(Texcoord.x,1 - Texcoord.y));
}


technique SkyBox
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_skybox();
		PixelShader = compile ps_3_0 ps_skybox();
	}
}
