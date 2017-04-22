Shader "Custom/OneColor" {
  Properties {
    _Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
  }
  SubShader {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
		Pass {
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
			
				struct v2f {
					float4 pos : SV_POSITION;
				};
			
				fixed4 _Color;
			
				v2f vert(appdata_base v) {
					v2f o;
					o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
					return o;
				}
			
				sampler2D _MainTex;
			
				float4 frag(v2f IN) : COLOR {
					half4 c = _Color;
					return c;
				}
			ENDCG
		}
	}
}