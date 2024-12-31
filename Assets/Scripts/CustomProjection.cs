using UnityEngine;

public class CustomProjectionMatrix : MonoBehaviour
{
    public Camera targetCamera; 
    public float fovY = 15f;
    [Tooltip("width/height")]public float aspect = 15f;

    void Start()
    {
        targetCamera = GetComponent<Camera>();
        SetProjMat();
    }

    void SetProjMat() {
        float tan = Mathf.Tan(fovY * 0.5f);
        float f = targetCamera.farClipPlane;
        float n = targetCamera.nearClipPlane;


        float xx = 1.0f / (aspect * tan);
        float yy = 1.0f / tan;
        float zz = -(f + n)/(f - n);
        float zw = -2f * f * n / (f - n);

        Matrix4x4 mat = new Matrix4x4();
        mat[0, 0] = xx;
        mat[1, 1] = yy;
        mat[2, 2] = zz;
        mat[2, 3] = zw;
        mat[3, 2] = -1;
        Matrix4x4 m = Matrix4x4.Perspective(fovY, aspect, n, f);
        targetCamera.projectionMatrix = m;
    }
}