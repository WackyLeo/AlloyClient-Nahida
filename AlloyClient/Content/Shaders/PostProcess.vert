#version 460 core

out vec2 vertUVs;

void main()
{
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);

    vertUVs = (vec2(x, y) + 1.0) *0.5;
}