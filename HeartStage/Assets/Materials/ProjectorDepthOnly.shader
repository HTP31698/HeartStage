Shader "Custom/ProjectorDepthOnly"
{
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-1"
            "RenderType" = "Opaque"
        }

        ZWrite On       // 깊이 기록
        ColorMask 0     // 색 안 그림

        Pass { }
    }
}
