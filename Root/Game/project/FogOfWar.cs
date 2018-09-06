
//1.生成表示地图信息，生成一个bool类型二维数组表示障碍物信息，true表示改坐标有障碍物
    public void GenerateMapData(float beginX, float beginY, float deltax, float deltay, float heightRange)
    {
        for (int i = 0; i < m_Width; i++)
        {
            for (int j = 0; j < m_Height; j++)
            {
                m_MapData[i, j] = MapInfoUtils.IsObstacle(beginX, beginY, deltax, deltay, heightRange, i, j);
            }
        }
    }
//2.计算视野区域，每次玩家位置变化后需要根据生成的障碍物信息重新计算视野范围.
    //射线检测可视范围，将可见区域设置为可见
    public void SetAsVisible(int x, int y)
    {
        m_MaskCache[y*m_Width + x] = 1;
    }
//3.生成战争迷雾mask贴图，R通道记录叠加的每次可见区域，G通道记录当前可见区域，B通道缓存上一次更新的可见区域
 public bool GenerateOrRefreshTexture()
 {
    if (m_MaskTexture == null)
    {
         m_MaskTexture = new Texture2D(m_Width, m_Height, TextureFormat.RGB24, false);
         tex.wrapMode = TextureWrapMode.Clamp;
    }
    for (int i = 0; i < m_MaskTexture.width; i++)
    {
       for (int j = 0; j < m_MaskTexture.height; j++)
       {
           bool isVisible = m_MaskCache[i, j] == 1;
           Color origin = isNew ? Color.black : m_MaskTexture.GetPixel(i, j);
           origin.r = Mathf.Clamp01(origin.r + origin.g);
           origin.b = origin.g;
           origin.g = isVisible ? 1 : 0;
           m_MaskTexture.SetPixel(i, j, origin);
           m_Visible[i, j] = (byte) (isVisible ? 1 : 0);
           m_MaskCache[i, j] = 0;
       }
    }
    m_MaskTexture.SetPixels(m_ColorBuffer);
    m_MaskTexture.Apply();
    m_UpdateMark = UpdateMark.None;
    return true;
  }
//4.投影战争迷雾,将生成的战争迷雾mask贴图投射到场景，直接采用后处理的方式。
  //生成投影矩阵并将矩阵参数设置到shader，(xSize迷雾区域宽度 zSize迷雾区域高度)
    Matrix4x4 m_WorldToProjector = default(Matrix4x4);
    m_WorldToProjector.m00 = 1.0f/xSize;
    m_WorldToProjector.m03 = -1.0f/xSize*position.x + 0.5f;
    m_WorldToProjector.m12 = 1.0f/zSize;
    m_WorldToProjector.m13 = -1.0f/zSize*position.z + 0.5f;
    m_WorldToProjector.m33 = 1.0f;
    m_EffectMaterial.SetMatrix("internal_WorldToProjector", m_WorldToProjector);
    //设置生成的mask纹理数据到shader
    m_EffectMaterial.SetTexture("_FogTex", fogTexture);
    //计算interpolatedRay
    //绘制
     void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
          RenderTexture.active = dest;
          fxMaterial.SetTexture("_MainTex", src);
            GL.PushMatrix();
            GL.LoadOrtho();
            fxMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.MultiTexCoord2(0, 0.0f, 0.0f);
            GL.Vertex3(0.0f, 0.0f, 3.0f); 
            GL.MultiTexCoord2(0, 1.0f, 0.0f);
            GL.Vertex3(1.0f, 0.0f, 2.0f); 
            GL.MultiTexCoord2(0, 1.0f, 1.0f);
            GL.Vertex3(1.0f, 1.0f, 1.0f);
            GL.MultiTexCoord2(0, 0.0f, 1.0f);
            GL.Vertex3(0.0f, 1.0f, 0.0f); 
            GL.End();
            GL.PopMatrix();
    }
   
//战争迷雾Shader
Shader "Hidden/FogOfWarEffect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_FogColor("FogColor", color) = (0,0,0,1)
		_FogTex ("FogTex", 2D) = "black" {}
		_MixValue("MixValue", float) = 0
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv_depth : TEXCOORD1;
				float3 interpolatedRay : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D _CameraDepthTexture;

			sampler2D _FogTex;

			half _MixValue;

			half4 _FogColor;

			float4x4 _FrustumCorners;
			float4x4 internal_WorldToProjector;

			v2f vert (appdata v)
			{
				v2f o;
				half index = v.vertex.z;
				v.vertex.z = 0.1;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				o.uv_depth = v.uv.xy;
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1 - o.uv.y;
				#endif
				o.interpolatedRay = _FrustumCorners[(int)index].xyz;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.uv));

				fixed depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv_depth)));
				
				fixed4 pos = fixed4(depth*i.interpolatedRay, 1);
				
				pos.xyz += _WorldSpaceCameraPos;
				pos = mul(internal_WorldToProjector, pos);
				
				
				pos.xy /= pos.w;

				fixed3 tex = tex2D(_FogTex, pos.xy).rgb;

				float2 atten = saturate((0.5 - abs(pos.xy - 0.5)) / (1 - 0.9));

				fixed3 col;
				col.rgb = lerp(_FogColor.rgb, fixed3(1, 1, 1), tex.r*_FogColor.a);

				fixed visual = lerp(tex.b, tex.g, _MixValue);
				col.rgb = lerp(col.rgb, fixed3(1, 1, 1), visual)*atten.x*atten.y;

				c.rgb *= col.rgb;
				c.a = 1;
				return c;
			}
			ENDCG
		}
	}
}

