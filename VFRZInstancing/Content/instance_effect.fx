#define VS_SHADERMODEL vs_5_0
#define PS_SHADERMODEL ps_5_0
	
	
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
	uint VertexID : SV_VertexID;
};

//16 bytes in total
struct DynamicVSinput
{
	float3 InstanceTransform;
	uint AtlasCoord;//x/y for column/row z for image index
};

//16 byte + 12 byte = 28 bytes
struct InstancingVSoutput
{
	float4 Position : SV_POSITION;
	float3 TexCoord : TEXCOORD0;
	float4 ColorD : COLOR0; // only xyz are needed
};

cbuffer ShaderData : register(b0)
{
    float2 ImageSizeArray[256];
};

StructuredBuffer<DynamicVSinput> TileBuffer;


InstancingVSoutput InstancingVS(in StaticVSinput input)
{
	uint tileID = input.VertexID/6;
	DynamicVSinput input1 = TileBuffer[tileID];
	InstancingVSoutput output;
	uint3 atlasCoordinate = uint3((input1.AtlasCoord & 0b00000000000000000000000011111111) >> 24,(input1.AtlasCoord & 0b00000000111111110000000000000000) >> 16, input1.AtlasCoord & 0b11111111111111110000000000000000);
	
	//actual Image Index
	uint index = atlasCoordinate.z;
	//get texture Size in the atlas
	float2 imageSize= ImageSizeArray[index];
	
	
	
	//how many Images are possible inside of the big texture
	float2 NumberOfTextures = float2(2048,2048) / float2(imageSize.x,imageSize.y); // all Images are 2048 x 2048 because 3DTexture doesnt support more and give blackscreen if bigger, maybe because old opengl 3_0
	

	float2 position = input.Position.xy * imageSize - float2(imageSize.x / 2, imageSize.y);;
	
	//calculate position with camera
	float4 pos = float4(position.xy + input1.InstanceTransform .xy,input1.InstanceTransform.z,1);
	pos = mul(pos, WorldViewProjection);
	
	
	output.Position = pos;
	output.TexCoord = float3((input.Position.x / NumberOfTextures.x) + (1.0f / NumberOfTextures.x * asfloat(atlasCoordinate.x * 255)),
							 (input.Position.y / NumberOfTextures.y) + (1.0f / NumberOfTextures.y * atlasCoordinate.y), index/NumberOf2DTextures + 0.1f / NumberOf2DTextures);//+0.1f / NumberOf2DTextures because texture3d want some between value?
	
	
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