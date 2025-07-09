#version 450

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inNormal;
layout(location = 2) in vec2 inTexCoord;

layout(push_constant) uniform PushConstants {
    mat4 model;
    mat4 view;
    mat4 proj;
} pc;

layout(location = 0) out vec3 fragNormal;
layout(location = 1) out vec3 fragPos;
layout(location = 2) out vec2 fragTexCoord;

void main() {
    gl_Position = pc.proj * pc.view * pc.model * vec4(inPosition, 1.0);
    fragNormal = normalize((pc.model * vec4(inNormal, 0.0)).xyz);
    fragPos = vec3(pc.model * vec4(inPosition, 1.0));
    fragTexCoord = inTexCoord;
}
