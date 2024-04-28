#define NON_ARRAY_TEXTURE
#include "VPCommonCore.cginc"

struct appdata {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed3 diff : COLOR0;
                float2 uv : TEXCOORD0;
                float3 normal: TEXCOORD1;
                #if defined(VERTEXLIGHT_ON)
					fixed3 vertexLightColor: TEXCOORD2;
                #endif
				UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _TexSides, _TexBottom;
            fixed _VoxelLight;
            fixed _FlashDelay;
            fixed _AnimSeed;
            fixed3 _Color;

            v2f vert (appdata v)
            {
                v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                v.texcoord.y = lerp(v.texcoord.y, v.vertex.y + 0.5, v.normal.y==0);
                float disp = sin(-_Time.w * _FlashDelay + _AnimSeed);
                v.vertex.xyz *= 1.0 + abs(disp) * 0.1;
                v.vertex.y += 0.5 + disp * 0.25;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				VOXELPLAY_MODIFY_VERTEX(v.vertex, worldPos)

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                o.normal = v.normal;
                // Daylight
				fixed  daylight    = max(0, _WorldSpaceLightPos0.y * 2.0);
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = 0.25 + max(0.25, dot(worldNormal, _WorldSpaceLightPos0.xyz)) * daylight;
                // factor in the light color
                o.diff = max(saturate(nl), _VPAmbientLight) * _VoxelLight * _LightColor0.rgb;
                #if defined(VERTEXLIGHT_ON)
                o.vertexLightColor = Shade4PointLights(unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,unity_LightColor[0].rgb, unity_LightColor[1].rgb,unity_LightColor[2].rgb, unity_LightColor[3].rgb,unity_4LightAtten0, worldPos, worldNormal);
                #endif
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
	            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                fixed4 col1 = tex2D(_MainTex, i.uv);
                fixed4 col2 = tex2D(_TexSides, i.uv);
                fixed4 col3 = tex2D(_TexBottom, i.uv);
                fixed4 col = lerp(col1, col2, i.normal.y == 0);
                col = lerp(col, col3, i.normal.y<0);
                col = saturate(lerp(col, col * 1.1, abs(sin((-_Time.w + i.uv.y + i.uv.x)) * _FlashDelay)));
                col.rgb *= _Color;
                #if defined(VERTEXLIGHT_ON)
                col.rgb *= i.diff + i.vertexLightColor;
                #else
                col.rgb *= i.diff;
                #endif
                return col;
            }
 