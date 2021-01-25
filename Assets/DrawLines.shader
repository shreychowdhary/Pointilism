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
                float3 pos;
                float4 color;
            };

            StructuredBuffer<Vertex> vertices;
            
            
            v2f vert (uint id : SV_VertexID, uint inst : SV_InstanceID)
            {
                Vertex v = vertices[id+inst*6];
                v2f o;
                o.vertex = float4(v.pos,1);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
