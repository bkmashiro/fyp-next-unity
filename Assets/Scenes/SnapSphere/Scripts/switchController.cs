using UnityEngine;
using System.Collections.Generic;

public class switchController : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> panels = new List<GameObject>();
    
    private int currentPanelIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 确保开始时只有一个面板是启用的
        if (panels.Count > 0)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].SetActive(i == 0);
            }
        }
    }

    public void SwitchToNextPanel()
    {
        if (panels.Count <= 1) return;

        // 禁用当前面板
        panels[currentPanelIndex].SetActive(false);
        
        // 更新索引到下一个面板
        currentPanelIndex = (currentPanelIndex + 1) % panels.Count;
        
        // 启用下一个面板
        panels[currentPanelIndex].SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
