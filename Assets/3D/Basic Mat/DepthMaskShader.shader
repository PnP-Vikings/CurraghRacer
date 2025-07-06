Shader "Masked/Mask" {
	SubShader{
		Tags("Queue" = "Geometry+10")

		ColorMask a
		ZWrite On

		Pass()
	}
}