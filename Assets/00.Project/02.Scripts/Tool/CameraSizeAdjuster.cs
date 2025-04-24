using UnityEngine;

public class CameraAndBackgroundAdjuster : MonoBehaviour
{
    public Camera mainCamera;
  //  public SpriteRenderer backgroundSpriteRenderer;
    public float defaultWidth = 720f; // 기준 너비
    public float defaultHeight = 1280f; // 기준 높이
    public float defaultOrthographicSize = 7f; // 기준 Orthographic Size
    private float deviceWidth = 0.0f;

    void Awake()
    {
        AdjustCameraAndBackgroundSize();
        deviceWidth = Screen.width;
    }

    //void OnRectTransformDimensionsChange()
    //{
    //    AdjustCameraAndBackgroundSize();
    //}

    //private void OnApplicationFocus(bool focus)
    //{
    //    if(focus == false)
    //    {
    //        AdjustCameraAndBackgroundSize();
    //    }
    //}

    private void FixedUpdate()
    {
        if(deviceWidth != Screen.width)
        {
            AdjustCameraAndBackgroundSize();
        }
    }


    void AdjustCameraAndBackgroundSize()
    { 
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }



        float targetAspect = defaultWidth / defaultHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        float orthographicSize = defaultOrthographicSize;

        if (currentAspect >= targetAspect)
        {
            orthographicSize = defaultOrthographicSize;
        }
        else
        {
            float adjustmentFactor = targetAspect / currentAspect;
            orthographicSize = defaultOrthographicSize * adjustmentFactor;
        }

        mainCamera.orthographicSize = orthographicSize;


 

       // AdjustBackgroundScale();
    }

    //void AdjustBackgroundScale()
    //{
    //    float screenHeight = mainCamera.orthographicSize * 2;
    //    float screenWidth = screenHeight * Screen.width / Screen.height;

    //        Vector2 spriteSize = backgroundSpriteRenderer.sprite.bounds.size;

    //        Vector3 newScale = backgroundSpriteRenderer.transform.localScale;
    //        newScale.x = screenWidth / spriteSize.x;
    //        newScale.y = screenHeight / spriteSize.y;

    //        backgroundSpriteRenderer.transform.localScale = newScale;

    //}

    void Update()
    {
     //   AdjustCameraAndBackgroundSize();
    }
}