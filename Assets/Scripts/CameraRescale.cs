using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraRescale : MonoBehaviour
{
    [SerializeField] float targetAspect;
    private Camera camera;

    private void Rescale(){
        camera = GetComponent<Camera>();
    }
}
