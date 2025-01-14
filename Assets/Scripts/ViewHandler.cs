using System.Collections.Generic;
using System.Collections;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.AI;

public class ViewHandler : MonoBehaviour
{
    [SerializeField] private View leftMirror;
    [SerializeField] private View rightMirror;
    [SerializeField] private View rearMirror;
    [SerializeField] private View leftBlindSpot;
    [SerializeField] private View rightBlindSpot;
    [SerializeField] private View leftWide;
    [SerializeField] private View rightWide;
    [SerializeField] private AnimationCurve curve;
    [SerializeField] private ViewState state = ViewState.Normal;
    [SerializeField] private KeyCode leftBlindViewKey = KeyCode.X;
    [SerializeField] private KeyCode rightBlindViewKey = KeyCode.Z;
    [SerializeField] private KeyCode wideViewKey = KeyCode.Tab;
    [SerializeField] public float animSpeed;
    private List<View> views;
    private Camera m_cam;
    void Start()
    {
        m_cam = transform.Find("revertedCamera").GetComponent<Camera>();
        views = new() { leftMirror, rightMirror, rearMirror, leftBlindSpot, rightBlindSpot, leftWide, rightWide };
        leftMirror.Init(true);
        rightMirror.Init(true);
        rearMirror.Init(false);
        rightBlindSpot.Init(false);
        leftBlindSpot.Init(false);
        leftWide.Init(false);
        rightWide.Init(false);
        ComputeCameras();
    }

    private void Update() {
        
        if (Input.GetKey(leftBlindViewKey)) state = ViewState.LeftBlind;
        else if (Input.GetKey(rightBlindViewKey)) state = ViewState.RightBlind;
        else if (Input.GetKey(wideViewKey)) state = ViewState.Wide;
        else state = ViewState.Normal;

        ComputeCameras();

        foreach (View view in views)
        {
            view.RenderView(m_cam);
        }
    }

    private void ComputeCameras() {
        switch (state)
        {
            case ViewState.LeftBlind:
                leftMirror.Hide(this, curve);
                rearMirror.Hide(this, curve);
                rightBlindSpot.Hide(this, curve);
                leftWide.Hide(this, curve);
                rightWide.Hide(this, curve);
                rightMirror.Show(this, curve);
                leftBlindSpot.Show(this, curve);
                break;
            case ViewState.RightBlind:
                rearMirror.Hide(this, curve);
                leftWide.Hide(this, curve);
                rightWide.Hide(this, curve);
                rightMirror.Hide(this, curve);
                leftBlindSpot.Hide(this, curve);
                leftMirror.Show(this, curve);
                rightBlindSpot.Show(this, curve);
                break;
            case ViewState.Wide:
                rightMirror.Hide(this, curve);
                leftBlindSpot.Hide(this, curve);
                leftMirror.Hide(this, curve);
                rightBlindSpot.Hide(this, curve);
                rearMirror.Show(this, curve);
                leftWide.Show(this, curve);
                rightWide.Show(this, curve);
                break;
            case ViewState.Normal:
            default:
                leftBlindSpot.Hide(this, curve);
                leftWide.Hide(this, curve);
                rightWide.Hide(this, curve);
                rightBlindSpot.Hide(this, curve);
                rearMirror.Show(this, curve);
                leftMirror.Show(this, curve);
                rightMirror.Show(this, curve);
                break;
        }
    }

    public enum ViewState {
        Normal, LeftBlind, RightBlind, Wide
    }
}

[System.Serializable]
public class View
{
    public Transform viewTransform;
    public RectTransform graphicParent;
    public Vector2 hiddenPos, shownPos;
    public Vector2Int textureSize;
    public float aspectRatio;
    public float fovY;
    public float farClipPlane;
    public float nearClipPlane;
    public RawImage imageComp;
    private RenderTexture tex;
    [HideInInspector] public bool shouldRender = false;
    private Material renderMat;
    private float animationValue = 0.0f;
    public void Init(bool shouldFlipMirror) {
        CreateRT(ref tex, textureSize.x, textureSize.y, 8, GraphicsFormat.R8G8B8A8_UNorm);
        Material tmp = Resources.Load<Material>("Materials/mirrorMat");
        renderMat = new Material(tmp);
        Resources.UnloadAsset(tmp);
        imageComp.texture = tex;
        imageComp.material = renderMat;
        renderMat.SetFloat("_isFlipped", shouldFlipMirror ? 1.0f : 0.0f);
        aspectRatio = (float)textureSize.x / ((float)textureSize.y);
    }

    public void Show(ViewHandler self, AnimationCurve curve) {

        float interp = curve.Evaluate(animationValue);
        graphicParent.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, interp);
        animationValue += Time.deltaTime * self.animSpeed;
        animationValue = Mathf.Clamp01(animationValue);

        /* if (!shouldRender)
            self.StartCoroutine(Animate(curve, true, self.animSpeed));
        shouldRender = true; */
    }

    public void Hide(ViewHandler self, AnimationCurve curve) {

        float interp = curve.Evaluate(animationValue);
        graphicParent.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, interp);
        animationValue -= Time.deltaTime * self.animSpeed;
        animationValue = Mathf.Clamp01(animationValue);
/* 
        if (shouldRender)
            self.StartCoroutine(Animate(curve, true, -self.animSpeed));
        shouldRender = false; */
    }

    private IEnumerator Animate(AnimationCurve curve, bool dir, float animSpeed) {
        animationValue = 0.0f;
        while(true) {
            float interp = curve.Evaluate(animationValue);
            if (dir)
                graphicParent.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, interp);
            else
                graphicParent.anchoredPosition = Vector2.Lerp(shownPos, hiddenPos, interp);
            animationValue += Time.deltaTime * animSpeed;
            animationValue = Mathf.Clamp01(animationValue);
            
            yield return new WaitForEndOfFrame();
        }
    }

    public Matrix4x4 GetProjMat() {
        return Matrix4x4.Perspective(fovY, aspectRatio, nearClipPlane, farClipPlane);
    }

    public Matrix4x4 ComputeViewMatrix()
    {
        return Matrix4x4.LookAt(viewTransform.position, viewTransform.position + viewTransform.forward, viewTransform.up);
    }

    public void RenderView(Camera camera) {
        if (animationValue > 0.1f){
            camera.projectionMatrix = GetProjMat();
            camera.transform.SetPositionAndRotation(viewTransform.position, viewTransform.rotation);
            camera.targetTexture = tex;
            camera.Render();
        }
    }

    private void CreateRT(ref RenderTexture rt, int w, int h, int depth, GraphicsFormat format) {
        rt = new RenderTexture(w, h, depth, format, 5);
    }
}