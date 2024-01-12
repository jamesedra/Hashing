#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<uint> _Hashes;
#endif

// converts a 1D line to 2D grid

// example:

// [1, 2, 3, 4....16]
// to
// [ 1] [ 2] [ 3] [ 4]
// [ 5] [ 6] [ 7] [ 8]
// [ 9] [10] [11] [12]
// [13] [14] [15] [16]

// positions of each grid will be based on a (u,v) coordinate system
// example:
// [0,0][0,1][0,2][0,3]
// [1,0][1,1][1,2][1,3]
// [2,0][2,1][2,2][2,3]
// [3,0][3,1][3,2][3,3]

// config is the value for resolution
// config x and y values determines how the 1D line gets cut into segments
float4 _Config;

void ConfigureProcedural () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		// get coordinates u and v
		// simulate integer division by floor of (float < 1) * int
		float v = floor(_Config.y * unity_InstanceID);
		float u = unity_InstanceID - _Config.x * v;

		unity_ObjectToWorld = 0.0;

		// place the instance on the XZ plane ** float4(x,y,z,a), keep Y value 0
		unity_ObjectToWorld._m03_m13_m23_m33 = float4(
			_Config.y * (u + 0.5) - 0.5,
			0.0,
			_Config.y * (v + 0.5) - 0.5,
			1.0
		);
		unity_ObjectToWorld._m00_m11_m22 = _Config.y;
	#endif
}

// produces an RGB color
float3 GetHashColor () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		uint hash = _Hashes[unity_InstanceID];
		return _Config.y * _Config.y * hash;
	#else
		return 1.0;
	#endif
}

void ShaderGraphFunction_float (float3 In, out float3 Out, out float3 Color) {
	Out = In;
	Color = GetHashColor();
}

void ShaderGraphFunction_float (half3 In, out half3 Out, out half3 Color) {
	Out = In;
	Color = GetHashColor();
}