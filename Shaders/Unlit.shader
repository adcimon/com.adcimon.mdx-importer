Shader "MDX/Unlit"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" { }
		_Color("Main Color", Color) = (1, 1, 1, 1)

		[Header(Alpha)]
		[Enum(None, 0, Discard, 1, TeamColor, 2)] _AlphaMode("Alpha Mode", Int) = 0
		_AlphaThreshold("Alpha Threshold", Range(0.0, 1.0)) = 0.8
		_TeamColor("Team Color", Color) = (1, 0, 0, 1)

		[Header(Rendering)]
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Int) = 2
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Int) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Z Test", Int) = 4

		[Header(Blending)]
		[Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Operation", Int) = 0
		[Enum(UnityEngine.Rendering.BlendOp)] _BlendAlphaOp("Alpha Operation", Int) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendSrcFactor("Source Factor", Int) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendDstFactor("Destination Factor", Int) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendSrcAlphaFactor("Source Alpha Factor", Int) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendDstAlphaFactor("Destination Alpha Factor", Int) = 0

		[Header(Stencil)]
		_StencilRef("Reference Value [0, 255]", Float) = 0
		_StencilReadMask("Read Mask [0, 255]", Int) = 255
		_StencilWriteMask("Write Mask [0, 255]", Int) = 255
		[Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Compare Function", Int) = 8
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilPass("Pass Operation", Int) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilFail("Fail Operation", Int) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail("Z Fail Operation", Int) = 0
	}

	SubShader
	{
		Tags { "Queue" = "Geometry" }

		Cull [_Cull]
		ZWrite [_ZWrite]
		ZTest [_ZTest]

		BlendOp[_BlendOp],[_BlendAlphaOp]
		Blend[_BlendSrcFactor][_BlendDstFactor],[_BlendSrcAlphaFactor][_BlendDstAlphaFactor]

		Stencil
		{
			Ref[_StencilRef]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
			Comp[_StencilComp]
			Pass[_StencilPass]
			Fail[_StencilFail]
			ZFail[_StencilZFail]
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment
			#include "UnityCG.cginc"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _Color;
			int _AlphaMode;
			float _AlphaThreshold;
			float4 _TeamColor;

			Varyings Vertex( Attributes input )
			{
				Varyings output;
				output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.uv = input.uv;
				return output;
			}

			half4 Fragment( Varyings input ) : SV_TARGET
			{
				half4 color = half4(1, 0, 1, 1);
				half4 textureColor = tex2D(_MainTex, input.uv);

				if( _AlphaMode == 0 ) // None
				{
					color = textureColor;
				}
				else if( _AlphaMode == 1 ) // Discard
				{
					color = textureColor;
					if( color.a < _AlphaThreshold )
					{
						discard;
					}
				}
				else if( _AlphaMode == 2 ) // TeamColor
				{
					color.rgb = textureColor.a * textureColor.rgb + (1 - textureColor.a) * _TeamColor.rgb;
					color.a = textureColor.a;
				}

				color = color * _Color;
				return color;
			}
			ENDHLSL
		}
	}
}