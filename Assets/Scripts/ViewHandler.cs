using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;

public class ViewHandler : MonoBehaviour
{
    private List<View> views;
    private Camera m_cam;
    void Start()
    {
        m_cam = transform.Find("revertedCamera").GetComponent<Camera>();
    }

    
}

[System.Serializable]
public class View
{
    public Transform transform;
    public float aspectRatio;
    public float fovY;
    public float farClipPlane;
    public float nearClipPlane;
    public Matrix4x4 getProjMat() {
        float tan = Mathf.Tan(fovY * 0.5f);
        float f = farClipPlane;
        float n = nearClipPlane;

        float xx = 1.0f / (aspectRatio * tan);
        float yy = 1.0f / tan;
        float zz = -(f + n)/(f - n);
        float zw = -2f * f * n / (f - n);

        Matrix4x4 mat = new Matrix4x4();
        mat[0, 0] = xx;
        mat[1, 1] = yy;
        mat[2, 2] = zz;
        mat[2, 3] = zw;
        mat[3, 2] = -1;
        return Matrix4x4.Perspective(fovY, aspectRatio, n, f);
    }
}