Shader "Custom/DrawLines"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct v2f
            {
                float4 vertex: SV_POSITION;
                float4 color: COLOR0;
            };

            struct Vertex {
                float2 pos;
                float4 color;
            };

            StructuredBuffer<Vertex> vertices;
            
            
            v2f vert (uint id : SV_VertexID, uint inst : SV_InstanceID)
            {
                Vertex v = vertices[id];
                v2f o;
                o.vertex = float4(v.pos,0,1);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                return float4(1,0,0,1);
            }
            ENDCG
        }
    }
}
