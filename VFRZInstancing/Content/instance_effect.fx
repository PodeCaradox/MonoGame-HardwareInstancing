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
RWStructuredBuffer<int> RowsIndex;
int StartPosX;
int StartPosY;
int Columns;
int Rows;
int MapSizeX;
int MapSizeY;

int is_in_map_bounds(int2 map_position) {
	if (map_position.x >= 0 && map_position.y >= 0 && map_position.y < MapSizeY && map_position.x < MapSizeX) { return 1; }

	return 0;
}

int calculate_rows(int2 start, int mapSizeX) {
	int rows = 0;

	if (start.y < start.x)
	{
		rows = (mapSizeX - 1) - (start.x - start.y);
	}
	else {
		rows = (mapSizeX - 1) + (start.y - start.x);
	}

	if (rows < 0) return 0;

	return rows;
}

int get_columns_until_border(int2 index) {
	if (index.x < index.y)
	{
		return index.x;
	}
	return index.y;
}

int is_outside_of_map(int2 start_pos) {
	int2 pos = start_pos;
	for (int i = 0; i < Columns; i += 1) {
		pos.x += 1;
		pos.y += 1;
		if (is_in_map_bounds(pos) == 1) {
			return 0;
		}
	}
	return 1;
}

int2 calc_start_point_outside_map(int2 start_pos) {
	int2 start = start_pos;
	//above right side of map
	if (StartPosX + StartPosY < MapSizeX) {
		int2 left = int2(StartPosX - Rows, StartPosY + Rows);
		left.x += left.y;
		left.y -= left.y;

		int2 right_bottom_screen = int2(StartPosX + Columns, StartPosY + Columns);
		//check if we are passed the last Tile for MapSizeX with the Camera
		if (right_bottom_screen.x + right_bottom_screen.y > MapSizeX) {
			start = int2(MapSizeX, 0);

		}
		else {
			//we are above the Last Tile so x < MapSizeX for Camera right bottom Position
			right_bottom_screen.x += right_bottom_screen.y;
			right_bottom_screen.y -= right_bottom_screen.y;
			start = right_bottom_screen;
		}

		//difference is all tiles on the x axis and because we calculate here x,y different to Isomectric View we need to divide by 2 and for odd number add 1 so % 2
		int difference = start.x - left.x;
		difference += difference % 2;
		difference /= 2;
		start.x -= difference;
		start.y -= difference;
		return start;
	}
	//underneath right side of map
	int to_the_left = StartPosX - MapSizeX;
	return int2(StartPosX - to_the_left, StartPosY + to_the_left);
}

int2 get_start_point(int2 start_pos) {
	int outside = is_outside_of_map(start_pos);
	if (outside == 1) { //calculate the starting point when outside of map on the right.
		return calc_start_point_outside_map(start_pos);
	}
	//inside the map
	return int2(StartPosX, StartPosY);
}

int calc_visible_index(int2 index, int2 actual_row_start) {
	int visible_index = 0;

	int2 start = get_start_point(int2(StartPosX, StartPosY));

	int rows_behind = calculate_rows(index, MapSizeX) - calculate_rows(start, MapSizeX);


	visible_index = RowsIndex[rows_behind];
	

	//index in current column
	int columns = get_columns_until_border(index);
	if (actual_row_start.x >= 0 && actual_row_start.y >= 0) {
		columns -= get_columns_until_border(actual_row_start);
	}

	visible_index += columns;
	return visible_index;
}




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

	int visible_index = calc_visible_index(index, actual_row_start);


	VisibleTiles[visible_index] = AllTiles[index.y * MapSizeX + index.x];
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