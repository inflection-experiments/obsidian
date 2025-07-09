#version 450

layout(location = 0) in vec3 fragNormal;
layout(location = 1) in vec3 fragPos;
layout(location = 2) in vec2 fragTexCoord;

layout(location = 0) out vec4 outColor;

void main() {
    vec3 lightDir = normalize(vec3(1.0, 1.0, 1.0));
    vec3 normal = normalize(fragNormal);

    float diff = max(dot(normal, lightDir), 0.2);
    vec3 color = vec3(0.7, 0.8, 1.0) * diff;

    outColor = vec4(color, 1.0);
}
