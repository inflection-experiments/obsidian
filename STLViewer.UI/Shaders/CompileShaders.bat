@echo off
echo Compiling GLSL shaders to SPIR-V...

if not exist "%VULKAN_SDK%\Bin\glslc.exe" (
    echo Error: Vulkan SDK not found or glslc.exe not available
    echo Please install the Vulkan SDK and ensure glslc.exe is in PATH
    exit /b 1
)

echo Compiling vertex shader...
"%VULKAN_SDK%\Bin\glslc.exe" vertex.vert -o vertex.spv
if errorlevel 1 (
    echo Error compiling vertex shader
    exit /b 1
)

echo Compiling fragment shader...
"%VULKAN_SDK%\Bin\glslc.exe" fragment.frag -o fragment.spv
if errorlevel 1 (
    echo Error compiling fragment shader
    exit /b 1
)

echo Shaders compiled successfully!
echo Generated files:
echo - vertex.spv
echo - fragment.spv
