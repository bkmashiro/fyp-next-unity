using UnityEngine;

public class MainUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public enum EditMode
    {
        View,
        Edit
    }

    EditMode editMode = EditMode.View;

    public void OnEditModeButtonClick()
    {
        editMode = editMode == EditMode.View ? EditMode.Edit : EditMode.View;
    }

    public enum Tool
    {
        Cursor,
        Camera,
        Comment,
        Anchor
    }

    Tool tool = Tool.Cursor;

    public void SetTool(Tool tool)
    {
        this.tool = tool;
    }

    public void OnCameraToolButtonClick()
    {
        SetTool(Tool.Camera);
    }

    public void OnCommentToolButtonClick()
    {
        SetTool(Tool.Comment);
    }

    public void OnAnchorToolButtonClick()
    {
        SetTool(Tool.Anchor);
    }

    public void OnCaptureButtonClick()
    {
        if (tool == Tool.Camera)
        {
            // TODO: Capture the screen, then enter the cursor mode
        }
    }

    public void OnSendCommentButtonClick()
    {
        if (tool == Tool.Comment)
        {
            // TODO: Send the comment
        }
    }
}
