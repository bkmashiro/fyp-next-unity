using UnityEngine;
using UnityEngine.UI;

public class TransformUIController : MonoBehaviour
{
    [SerializeField] private SimpleTransformController transformController;
    
    [Header("UI Buttons")]
    [SerializeField] private Button positionButton;
    [SerializeField] private Button rotationButton;
    [SerializeField] private Button scaleButton;
    
    [Header("Button Colors")]
    [SerializeField] private Color activeColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);
    
    private void Start()
    {
        if (transformController == null)
        {
            transformController = FindFirstObjectByType<SimpleTransformController>();
            Debug.Log("TransformUIController: 自动查找SimpleTransformController: " + (transformController != null ? "成功" : "失败"));
        }
        else
        {
            Debug.Log("TransformUIController: 已引用SimpleTransformController");
        }
        
        // 检查按钮引用
        if (positionButton == null) Debug.LogWarning("TransformUIController: positionButton未设置");
        if (rotationButton == null) Debug.LogWarning("TransformUIController: rotationButton未设置");
        if (scaleButton == null) Debug.LogWarning("TransformUIController: scaleButton未设置");
        
        // 确保按钮可交互
        EnsureButtonInteractable(positionButton);
        EnsureButtonInteractable(rotationButton);
        EnsureButtonInteractable(scaleButton);
        
        // 初始状态设置为位置模式
        SetTransformMode(SimpleTransformController.HandleType.Position);
    }
    
    // 确保按钮可交互
    private void EnsureButtonInteractable(Button button)
    {
        if (button == null) return;
        
        // 确保按钮可交互
        button.interactable = true;
        
        // 确保按钮的Image组件可接收射线
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.raycastTarget = true;
            
            // 确保按钮不是完全透明的
            Color color = buttonImage.color;
            if (color.a < 0.1f)
            {
                color.a = 1f;
                buttonImage.color = color;
                Debug.Log("TransformUIController: 按钮透明度已调整");
            }
        }
        
        // 检查按钮是否在Canvas Group下
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Debug.Log("TransformUIController: CanvasGroup设置已调整");
        }
        
        // 检查按钮的RectTransform
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 确保按钮有足够的大小
            if (rectTransform.sizeDelta.x < 10 || rectTransform.sizeDelta.y < 10)
            {
                rectTransform.sizeDelta = new Vector2(50, 50);
                Debug.Log("TransformUIController: 按钮大小已调整");
            }
        }
    }
    
    // 公共方法，供编辑器绑定按钮事件
    public void SetPositionMode()
    {
        Debug.Log("TransformUIController: SetPositionMode被调用");
        SetTransformMode(SimpleTransformController.HandleType.Position);
    }
    
    public void SetRotationMode()
    {
        Debug.Log("TransformUIController: SetRotationMode被调用");
        SetTransformMode(SimpleTransformController.HandleType.Rotation);
    }
    
    public void SetScaleMode()
    {
        Debug.Log("TransformUIController: SetScaleMode被调用");
        SetTransformMode(SimpleTransformController.HandleType.Scale);
    }
    
    private void SetTransformMode(SimpleTransformController.HandleType mode)
    {
        Debug.Log("TransformUIController: SetTransformMode: " + mode);
        
        if (transformController == null)
        {
            Debug.LogError("TransformUIController: transformController为空，无法设置模式");
            return;
        }
        
        // 更新控制器模式
        transformController.SetHandleType(mode);
        
        // 更新按钮颜色
        UpdateButtonColors(mode);
    }
    
    private void UpdateButtonColors(SimpleTransformController.HandleType activeMode)
    {
        // 重置所有按钮颜色
        if (positionButton != null)
            positionButton.GetComponent<Image>().color = inactiveColor;
            
        if (rotationButton != null)
            rotationButton.GetComponent<Image>().color = inactiveColor;
            
        if (scaleButton != null)
            scaleButton.GetComponent<Image>().color = inactiveColor;
            
        // 设置当前模式按钮为激活颜色
        switch (activeMode)
        {
            case SimpleTransformController.HandleType.Position:
                if (positionButton != null)
                    positionButton.GetComponent<Image>().color = activeColor;
                break;
            case SimpleTransformController.HandleType.Rotation:
                if (rotationButton != null)
                    rotationButton.GetComponent<Image>().color = activeColor;
                break;
            case SimpleTransformController.HandleType.Scale:
                if (scaleButton != null)
                    scaleButton.GetComponent<Image>().color = activeColor;
                break;
        }
    }
} 