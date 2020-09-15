#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif


	Texture2DArray test;

matrix WorldViewProjection;
float2 NumberOfTextures;

sampler TextureSampler : register(s0);

struct InstancingVSinput
{
	float4 Position : POSITION0;
	float3 TexCoord : TEXCOORD0;
};

struct InstancingVSoutput
{
	float4 Position : POSITION0;
	float3 TexCoord : TEXCOORD0;
};

InstancingVSoutput InstancingVS(InstancingVSinput input, float4 instanceTransform : POSITION1,
								float3 atlasCoord : TEXCOORD1)
{
	InstancingVSoutput output;
	instanceTransform.xy *= 2;
	float4 pos = (input.Position + instanceTransform);
	pos = mul(pos, WorldViewProjection);

	output.Position = pos;
	output.TexCoord = float3((input.TexCoord.x / NumberOfTextures.x) + (1.0f / NumberOfTextures.x * atlasCoord.x),
							 (input.TexCoord.y / NumberOfTextures.y) + (1.0f / NumberOfTextures.y * atlasCoord.y),atlasCoord.z);
	return output;
}

float4 InstancingPS(InstancingVSoutput input) : COLOR0
{
		float4 caluclatedColor = tex2D(TextureSampler, input.TexCoord.xy);
		if(caluclatedColor.a == 0){
			clip(-1);
		}
		float4 caluclatedColor1 = test.Sample(TextureSampler, input.TexCoord);
	return caluclatedColor;
}

technique Instancing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL InstancingVS();
		PixelShader = compile PS_SHADERMODEL InstancingPS();
	}
};