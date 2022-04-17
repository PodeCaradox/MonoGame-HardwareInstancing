#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
	
	
//Number of Textures inside of the Texture3D
float NumberOf2DTextures;
matrix WorldViewProjection;

Texture3D SpriteTexture;
sampler SpriteTextureSampler : register(s0) = sampler_state 
{
    Texture = (SpriteTexture);
};

//8 bytes in total
struct StaticVSinput
{
	float4 Position : COLOR0; // only xyz are needed
	float4 TexCoord : COLOR1;//only x/y is needed
};

//16 bytes in total
struct DynamicVSinput
{
	float3 InstanceTransform : POSITION0;
	float4 AtlasCoord : COLOR2;//x/y for column/row z for image index and w for ShadowColor
};

//16 byte + 12 byte = 28 bytes
struct InstancingVSoutput
{
	float4 Position : SV_POSITION;
	float3 TexCoord : TEXCOORD0;
};

cbuffer ShaderData : register(b0)
{
    float2 ImageSizeArray[256];
};



InstancingVSoutput InstancingVS(in StaticVSinput input, in DynamicVSinput input1)
{
	InstancingVSoutput output;
	//Colors * 255 because its between 0 - 1
	float4 atlasCoordinate = input1.AtlasCoord * 255;
	
	//actual Image Index
	float index = atlasCoordinate.z * 256 + atlasCoordinate.w;
	

	//get texture Size in the atlas
	float2 imageSize= ImageSizeArray[index];
	
	
	
	//how many Images are possible inside of the big texture
	float2 NumberOfTextures = float2(2048,2048) / float2(imageSize.x,imageSize.y); // all Images are 2048 x 2048 because 3DTexture doesnt support more and give blackscreen if bigger, maybe because old opengl 3_0
	

	input.Position.xy = input.Position.xy * imageSize - float2(imageSize.x / 2, imageSize.y);;
	
	//calculate position with camera
	float4 pos = float4(input.Position.xyz + input1.InstanceTransform,1);
	pos = mul(pos, WorldViewProjection);
	


	output.Position = pos;
	output.TexCoord = float3((input.TexCoord.x / NumberOfTextures.x) + (1.0f / NumberOfTextures.x * atlasCoordinate.x),
							 (input.TexCoord.y / NumberOfTextures.y) + (1.0f / NumberOfTextures.y * atlasCoordinate.y), index/NumberOf2DTextures + 0.1f / NumberOf2DTextures);//+0.1f / NumberOf2DTextures because texture3d want some between value?
	
	
	return output;
}

float4 InstancingPS(InstancingVSoutput input) : SV_TARGET
{
	

	  float4 caluclatedColor = SpriteTexture.Sample(SpriteTextureSampler, input.TexCoord);
	//if(caluclatedColor.a == 0){
	//	clip(-1);
	//}
	
	
	return caluclatedColor;
}

technique Instancing
{
	pass Pass1
	{
		VertexShader = compile VS_SHADERMODEL InstancingVS();
		PixelShader = compile PS_SHADERMODEL InstancingPS();
	}
};