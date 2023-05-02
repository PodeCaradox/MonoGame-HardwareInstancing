﻿#define VS_SHADERMODEL vs_5_0
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
int CalculateRows(int2 start, int mapSizeX)
{
	int rows;
	if (start.y < start.x)
	{
		rows = mapSizeX - (start.x - start.y);
	}
	else
	{
		rows = mapSizeX + (start.y - start.x);
	}


	return rows;
}

int GetColumnsUntilBorder(int2 index)
{
	if (index.x < index.y)
	{
		return index.x;
	}
	return index.y;
}

StructuredBuffer<Tile> AllTiles;
RWStructuredBuffer<Tile> VisibleTiles;
int StartPosX;
int StartPosY;
int Columns;
int Rows;
int MapSizeX;
int MapSizeY;

[numthreads(GroupSize, GroupSize, 1)]
void InstancingCS(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
        uint  localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{   
	int2 index = int2(StartPosX,StartPosY);
	uint column = globalID.x;
	uint row = globalID.y;
	
	index.x -= row % 2;
	row /= 2;
	index.y += row;
	index.x -= row;
	int2 actual_row_start = index;
	index.y += column;
	index.x += column;
	

	if(index.x < 0 || index.y < 0 || index.y >= MapSizeY || index.x >= MapSizeX){ 
		return;
	}

	//calc index for visible tiles array start
	int visibleIndex = 0;
	int rows_behind;
	int2 start = int2(StartPosX, StartPosY);
	int outside = 1;
	//check if we are outside with the Top Right Point of the camera
	for (int j = 0; j < Columns; j++)
	{
		start.x++;
		start.y++;
		if (start.x >= 0 && start.y >= 0 && start.y < MapSizeY && start.x < MapSizeX)
		{
			outside = 0;
			break;
		}
	}

	//calculate the starting point when outside of map on the right.
	if (outside == 1)
	{
		//above map
		if (StartPosX + StartPosY < MapSizeX)
		{
			int2 left = int2(StartPosX - Rows, StartPosY + Rows);
			left.x += left.y;
			left.y -= left.y;
			
			int2 righ_bottom_screen = int2(StartPosX + Columns, StartPosY + Columns);
			//check if we are passed the last Tile for MapSizeX with the Camera
			if (righ_bottom_screen.x + righ_bottom_screen.y > MapSizeX)
			{
				start = int2(MapSizeX - 1, 0);
			}
			else
			{
				//we are above the Last Tile so x < MapSizeX for Camera right bottom Position
				righ_bottom_screen.x += righ_bottom_screen.y;
				righ_bottom_screen.y -= righ_bottom_screen.y;
				start = righ_bottom_screen;
			}

			//difference is all tiles on the x axis and because we calculate here x,y different to Isomectric View we need to divide by 2 and for odd number add 1 so % 2
			int difference = start.x - left.x;
			difference += difference % 2;
			difference /= 2;
			start.x -= difference;
			start.y -= difference;
		}
		else // underneath map
		{
			int to_the_left = StartPosX - MapSizeX;
			start = int2(StartPosX - to_the_left, StartPosY + to_the_left);
		}
	}//inside the map
	else
	{
		start = int2(StartPosX, StartPosY);
	}

	//Calc how many rows are allready drawn behind us, until camera view end on the right side
	rows_behind = CalculateRows(index, MapSizeX) - CalculateRows(start, MapSizeX);

	//this will be a array in the shader later
	//calculate how many tiles are in each Row will be drawn so we ge Correct Array index
	for (int i = 0; i < rows_behind; i++) {
		int current_row = i / 2;
		int2 pos = int2(start.x - i % 2 - current_row, start.y + current_row);
		int vertical_tiles = Columns;
		if (pos.x < 0 || pos.y < 0) {
			if (pos.x < pos.y) {
				vertical_tiles += pos.x;
				pos.y -= pos.x;
				pos.x = 0;
			}
			else {
				vertical_tiles += pos.y;
				pos.x -= pos.y;
				pos.y = 0;
			}
		}
		pos.x += vertical_tiles;
		pos.y += vertical_tiles;

		if (pos.x >= MapSizeX) {
			int tiles_overflow = pos.x - MapSizeX;   
			vertical_tiles -= tiles_overflow;
			pos.y -= tiles_overflow;
		}

		if (pos.y >= MapSizeY) {
			int tiles_overflow = pos.y - MapSizeY;
			vertical_tiles -= tiles_overflow;
		}
		visibleIndex += vertical_tiles;
	}

	//get all Colums to the actual Index
	int columns = GetColumnsUntilBorder(index);

	//get correct Index if the Camera is inside of the Map so we subtract all Colums above of the camera view
	if (actual_row_start.x >= 0 && actual_row_start.y >= 0)
	{
		columns -= GetColumnsUntilBorder(actual_row_start);
	}

	visibleIndex += columns;
	//calc index for visible tiles array end


	VisibleTiles[visibleIndex] = AllTiles[index.y * MapSizeX + index.x];
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
	float2 NumberOfTextures = float2(2048,2048) / float2(imageSize.x,imageSize.y); // all Images are 2048 x 2048 because 3DTexture doesnt support more and give blackscreen if bigger. Old Hardware cant support that much
	

	float2 position = input.Position.xy * imageSize - float2(imageSize.x / 2, imageSize.y);;
	
	//calculate position with camera
	float4 pos = float4(position.xy + tile.InstanceTransform.xy, tile.InstanceTransform.z, 1);
	pos = mul(pos, WorldViewProjection);
	
	
	output.Position = pos;
	output.TexCoord = float3((input.Position.x / NumberOfTextures.x) + (1.0f / NumberOfTextures.x * atlasCoordinate.x),
							 (input.Position.y / NumberOfTextures.y) + (1.0f / NumberOfTextures.y * atlasCoordinate.y), index/NumberOf2DTextures + 0.1f / NumberOf2DTextures);//+0.1f / NumberOf2DTextures because texture3d want some between value, in future use 2dTextureArray?
	
	
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