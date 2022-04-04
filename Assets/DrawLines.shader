Shader "Custom/DrawLines" {
    Properties {}
    SubShader {
        Blend SrcAlpha OneMinusSrcAlpha

        LOD 100


        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct v2f {
                float4 vertex: SV_POSITION;
                float4 color: COLOR0;
                float2 uv : TEXCOORD1;
            };

            struct Vertex {
                float2 pos;
                float4 color;
            };

            static float2 uvByVertexID[6] =
            {
                float2(1.0, 0.0),
                float2(0.0, 1.0),
                float2(0.0, 0.0),
                float2(0.0, 0.0),
                float2(1.0, 1.0),
                float2(1.0, 0.0)
            };

            StructuredBuffer<Vertex> vertices;
            
            
            v2f vert (uint id : SV_VertexID, uint inst : SV_InstanceID) {
                Vertex v = vertices[id+inst*6];
                v2f o;
                o.vertex = float4(v.pos,0,1);
                o.color = v.color;
                o.uv = uvByVertexID[id];
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target {
                return length(i.uv-float2(.5,.5)) < .5 ? i.color : float4(0,0,0,0);
            }
            ENDCG
        }
    }
}
