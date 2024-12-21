using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CustomProjection : MonoBehaviour
{
    [SerializeField] private float curvature;
    private Camera cam;
    private void Start() {
        cam = GetComponent<Camera>();
        Matrix4x4 mat = cam.projectionMatrix;
        mat.m00 *= 1.0f + curvature;
        mat.m11 *= 1.0f + curvature;
        cam.projectionMatrix = mat;
    }
}
