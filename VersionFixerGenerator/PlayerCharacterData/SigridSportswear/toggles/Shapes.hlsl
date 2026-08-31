// **** SHAPEKEYS SHADER ****
// By: Cybertron, SinsOfSeven

struct VertexAttributes {
    float3 position;
    float3 normal;
    float4 tangent;
};

RWStructuredBuffer<VertexAttributes> rw_buffer : register(u5);
StructuredBuffer<VertexAttributes> base : register(t50);
StructuredBuffer<VertexAttributes> shapekey : register(t51);

Texture1D<float4> IniParams : register(t120);
#define key IniParams[88].x

[numthreads(1, 1, 1)]
void main(uint3 threadID : SV_DispatchThreadID)
{
    uint i = threadID.x;
    VertexAttributes diff;
    diff.position = shapekey[i].position - base[i].position;
    diff.normal = shapekey[i].normal - base[i].normal;
    diff.tangent = shapekey[i].tangent - base[i].tangent;
    rw_buffer[i].position += diff.position*key;
    rw_buffer[i].normal += diff.normal*key;
    rw_buffer[i].tangent += diff.tangent*key;
}