#if OPENGL
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_5_0
	#define PS_SHADERMODEL ps_5_0
#endif

//lookupTable for Color only first 5 bits are used for color the other 3 are for Image index so we can extend it to have a total of 2048 Images
// normally 256 lookups, but we can get to 8 because each 32 steps are the same numbers
static float2 lookUpArray[8] =
{
	float2(0,0),
	float2(32,256),
	float2(64,512),
	float2(96,768),
	float2(128,1024),
	float2(160,1280),
	float2(192,1536),
	float2(224,1792)
};

matrix WorldViewProjection;

//Number of Textures inside of the Texture3D
float NumberOf2DTextures;


//Vector2 for imageSizes
//cant use float2 array with size 2048, for some hardware limitions, the shader crashes because they will be converted to float4, instead using float4 like internally done, but less size in memory
float4 ImageSizeArray[1024];


Texture3D SpriteTexture : register(t0);

sampler3D SpriteTextureSampler : register(s0) = sampler_state
{
	Texture = <SpriteTexture>;
	Filter = Point;  
    AddressU = Wrap;
    AddressV = Wrap;
	MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

//8 bytes in total
struct StaticVSinput
{
	float4 Position : COLOR2; // only xyz are needed
	float4 TexCoord : COLOR3;//only x/y is needed
};

//16 bytes in total
struct DynamicVSinput
{
	float3 InstanceTransform : TEXCOORD1;
	float4 AtlasCoord : COLOR1;//x/y for column/row z for image index and w for ShadowColor
};

//16 byte + 12 byte + 4 byte = 32 bytes
struct InstancingVSoutput
{
	float4 Position : POSITION0;
	float3 TexCoord : TEXCOORD0;
	float4 Color : COLOR0;
};

struct PixelShaderOutput
{
    float4 Diffuse : COLOR0;
};

InstancingVSoutput InstancingVS(StaticVSinput input, DynamicVSinput input1)
{
	InstancingVSoutput output;
	//Colors * 255 because its between 0 - 1
	float4 atlasCoordinate = input1.AtlasCoord * 255;
	
	//we need a lookup table because opengl 3_0 has no bitshifting
	float2 bitShifting = lookUpArray[atlasCoordinate.w / 32];
	
	//Shadow Color max is 31 = 5 bits, darkest will be 69
	atlasCoordinate.w = (69 +(atlasCoordinate.w - bitShifting.x)*6)/255;
	
	//actual Image Index with the Bits shiftet
	atlasCoordinate.z = atlasCoordinate.z + bitShifting.y;
	

	//get texture Size in the atlas, calculations are needed because float4 isntead of float2
	float2 imageSize;
	if(fmod(atlasCoordinate.z,2) == 0){
	imageSize= ImageSizeArray[atlasCoordinate.z/2].xy;
	}
	else{
	imageSize= ImageSizeArray[atlasCoordinate.z/2].zw;
	}
	
	
	//how many Images are possible inside of the big texture
	float2 NumberOfTextures = float2(2048,2048) / float2(imageSize.x,imageSize.y); // all Images are 2048 x 2048 because 3DTexture doesnt support more and give blackscreen if bigger, maybe because old opengl 3_0
	
	//Calculate ImageSizeToDraw
	input.Position.xy = input.Position.xy * imageSize;
	
	//calculate position with camera
	float4 pos = float4(input.Position.xyz + input1.InstanceTransform,1);
	pos = mul(pos, WorldViewProjection);
	


	output.Position = pos;
	output.TexCoord = float3((input.TexCoord.x / NumberOfTextures.x) + (1.0f / NumberOfTextures.x * atlasCoordinate.x),
							 (input.TexCoord.y / NumberOfTextures.y) + (1.0f / NumberOfTextures.y * atlasCoordinate.y),atlasCoordinate.z/NumberOf2DTextures + 0.1f / NumberOf2DTextures);//+0.1f / NumberOf2DTextures because texture3d want some between value?
	
	output.Color = float4(atlasCoordinate.w,atlasCoordinate.w,atlasCoordinate.w,1);
	return output;
}

PixelShaderOutput InstancingPS(InstancingVSoutput input)
{
	PixelShaderOutput Output;

	float4 caluclatedColor = tex3D(SpriteTextureSampler, input.TexCoord) * input.Color;
	Output.Diffuse = caluclatedColor; 
	if(caluclatedColor.a == 0){
		clip(-1);
	}
	
	
	return Output;
}

technique Instancing
{
	pass Pass1
	{
		VertexShader = compile VS_SHADERMODEL InstancingVS();
		PixelShader = compile PS_SHADERMODEL InstancingPS();
	}
};