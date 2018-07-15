Shader "Glass" {
Properties {
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("Shininess", Range (0.01, 1)) = 0.8

	}
	SubShader {
		Tags {
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
		}
		LOD 300
		
		CGPROGRAM
			#pragma surface surf BlinnPhong decal:add nolightmap
			
			half _Shininess;
			
			struct Input {
				float dummy;
			};

			
			void surf (Input IN, inout SurfaceOutput o) {
			
				o.Gloss = 1;
				o.Specular = _Shininess;


			}
		ENDCG
	}
	FallBack "Transparent/VertexLit"
}
