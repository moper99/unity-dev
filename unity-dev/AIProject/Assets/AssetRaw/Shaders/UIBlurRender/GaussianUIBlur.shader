Shader "Custom/GaussianUIBlur"
{
        Properties
        {
            _MainTex("GrapTexture", 2D) = "white" {}
            _BlurSize ("_BlurSize", Range(0, 30)) = 2.5
            _MultiplyColor ("Multiply Tint color", Color) = (0.43, 0.43, 0.43)
            _Alpha("_Alpha", Range(0.0, 1.0)) = 0.2
        }
    
        HLSLINCLUDE
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
        struct appdata_t
        {
            half4 vertex : POSITION;
        };

        struct v2f
        {
            half4 vertex : SV_POSITION;
            half2 uv[5] : TEXCOORD0;
        };
        

        half4 _MainTex_TexelSize;
        half _BlurSize;
        half3 _MultiplyColor;
        half _Alpha;
  
        
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        //unity 内置关心拷贝过来
        half4 URPComputeGrabScreenPos (half4 pos) {
            #if UNITY_UV_STARTS_AT_TOP
                half scale = -1.0;
            #else
                half scale = 1.0;
            #endif
                half4 o = pos * 0.5f;
            o.xy = half2(o.x, o.y*scale) + o.w;
            o.zw = pos.zw;
            return o;
        }
        
        v2f VertBlurVertical(appdata_t v)
        {
            v2f o;
            o.vertex = TransformObjectToHClip(v.vertex.xyz);
            half4 scrPos = ComputeScreenPos(o.vertex);
            half2 uv = scrPos.xy;
            
            o.uv[0] = uv;
            o.uv[1] = uv + half2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
            o.uv[2] = uv - half2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
            o.uv[3] = uv + half2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
            o.uv[4] = uv - half2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
            return o;
        }
        
        v2f VertBlurHorizontal(appdata_t v)
        {
            v2f o;
            o.vertex = TransformObjectToHClip(v.vertex.xyz);
            half4 scrPos = ComputeScreenPos(o.vertex);
            half2 uv = scrPos.xy;
            //half2 uv = half2(scrPos.x,1.0-scrPos.y);
            
            o.uv[0] = uv;
            o.uv[1] = uv + half2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
            o.uv[2] = uv - half2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
            o.uv[3] = uv + half2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
            o.uv[4] = uv - half2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
            return o;
        }
        
        
        half4 FragBlurVertical(v2f i) : SV_Target
        {
            const half weight[3] = {0.4026, 0.2442,0.0545};

            half3 sum = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv[0]).rgb * weight[0];
            sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv[1]).rgb * weight[1];
            sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv[2]).rgb * weight[1];
            sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv[3]).rgb * weight[2];
            sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv[4]).rgb * weight[2];

            return half4(sum, 1.0);
        }
        
        half4 FragHorizontal(v2f i) : SV_Target
        {
            const half weight[3] = {0.4026, 0.2442,0.0545};

            half3 sum = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv[0]).rgb * weight[0];
            sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv[1]).rgb * weight[1];
            sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv[2]).rgb * weight[1];
            sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv[3]).rgb * weight[2];
            sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv[4]).rgb * weight[2];

            return half4(sum * _MultiplyColor, _Alpha);
        }
        
        ENDHLSL

        SubShader
        {
            ZTest Always
            Cull Off
            Lighting Off
            ZWrite Off
            
            Pass
            {
                Tags { "RenderPipeline" = "UniversalPipeline"}
                Name "VerticalBlurPass"
                HLSLPROGRAM
                
				#pragma vertex VertBlurVertical
				#pragma fragment FragBlurVertical
                
                ENDHLSL
            }

            Pass
            {
                Tags { "RenderPipeline" = "UniversalPipeline"}
                Name "HorizontalBlurPass"
                
                HLSLPROGRAM
                
			    #pragma vertex VertBlurHorizontal
			    #pragma fragment FragHorizontal
                
                ENDHLSL
            }
        }
    
}