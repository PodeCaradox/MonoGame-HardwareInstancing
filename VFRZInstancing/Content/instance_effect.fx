#define VS_SHADERMODEL vs_5_0
#define PS_SHADERMODEL ps_5_0
#define CS_SHADERMODEL cs_5_0 
	
	
//16 bytes in total
struct Tile
{
	float3 InstanceTransform;
	uint AtlasCoord;//x/y for column/row z for image index
};
	
//=============================================================================
// Compute Shader
//=============================================================================
#define GroupSize 32


StructuredBuffer<Tile> AllTiles;
RWStructuredBuffer<Tile> VisibleTiles;
RWStructuredBuffer<uint> CountData;
int StartPosX;
int StartPosY;
int MapSizeX;
int MapSizeY;

[numthreads(GroupSize, GroupSize, 1)]
void InstancingCS(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
        uint  localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{   
	int2 index = int2(StartPosX,StartPosY);
	uint column = globalID.x;
	uint row = globalID.y;
	if(row % 2 == 1){
		index.x--;
	}
	row /= 2;
	index.y += row;
	index.x -= row;
	index.y += column;
	index.x += column;
	
	
	if(index.x < 0 || index.y < 0 || index.y >= MapSizeY || index.x >= MapSizeX) return;
	
	uint outID;
	InterlockedAdd(CountData[0], 1, outID); // increment the instance count in the indirect draw buffer (starts at byte 4) 
	VisibleTiles[outID] = AllTiles[index.y * MapSizeX + index.x];
}


//==============================================================================
// Vertex shader
//==============================================================================

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

StructuredBuffer<Tile> TileBuffer;

InstancingVSoutput InstancingVS(in StaticVSinput input)
{
	uint tileID = input.VertexID/6;
	Tile tile = TileBuffer[tileID];
	InstancingVSoutput output;
	uint3 atlasCoordinate = uint3((tile.AtlasCoord & 0x000000ff),(tile.AtlasCoord & 0x0000ff00) >> 8, tile.AtlasCoord >> 16);

	//actual Image Index
	uint index = atlasCoordinate.z;
	//get texture Size in the atlas
	float2 imageSize= ImageSizeArray[index];
	
	//how many Images are possible inside of the big texture
	float2 NumberOfTextures = float2(2048,2048) / float2(imageSize.x,imageSize.y); // all Images are 2048 x 2048 because 3DTexture doesnt support more and give blackscreen if bigger, maybe because old opengl 3_0
	

	float2 position = input.Position.xy * imageSize - float2(imageSize.x / 2, imageSize.y);;
	
	//calculate position with camera
	float4 pos = float4(position.xy + tile.InstanceTransform .xy,tile.InstanceTransform.z,1);
	pos = mul(pos, WorldViewProjection);
	
	
	output.Position = pos;
	output.TexCoord = float3((input.Position.x / NumberOfTextures.x) + (1.0f / NumberOfTextures.x * atlasCoordinate.x),
							 (input.Position.y / NumberOfTextures.y) + (1.0f / NumberOfTextures.y * atlasCoordinate.y), index/NumberOf2DTextures + 0.1f / NumberOf2DTextures);//+0.1f / NumberOf2DTextures because texture3d want some between value?
	
	
	return output;
}

//==============================================================================
// Pixel shader 
//==============================================================================

float4 InstancingPS(InstancingVSoutput input) : SV_TARGET
{
	float4 caluclatedColor = SpriteTexture.Sample(SpriteTextureSampler, input.TexCoord);
	if (caluclatedColor.a == 0)
    {
        discard;
    }
	return caluclatedColor;
}


//===============================================================================
// Techniques
//===============================================================================

technique Instancing
{
	pass Pass1
	{
		ComputeShader = compile CS_SHADERMODEL InstancingCS();
		VertexShader = compile VS_SHADERMODEL InstancingVS();
		PixelShader = compile PS_SHADERMODEL InstancingPS();
	}
};