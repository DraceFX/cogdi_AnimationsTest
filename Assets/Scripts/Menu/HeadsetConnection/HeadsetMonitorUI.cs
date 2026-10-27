using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeadsetMonitorUI : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] private Image batteryFillImage;
    [SerializeField] private Slider storageSlider;
    [SerializeField] private TMP_Text modelText;
    [SerializeField] private TMP_Text batteryText;
    [SerializeField] private TMP_Text storageText;
    [SerializeField] private TMP_Text serialText;


    [Header("Model Image")]
    [SerializeField] private Image modelImage;                          // UI Image под спрайт шлема
    [SerializeField] private Sprite fallbackModelSprite;
    [SerializeField] private List<ModelImagePair> modelSprites = new List<ModelImagePair>();

    [Header("Apk Install")]
    [SerializeField] private Button installApkButton;
    [SerializeField] private TMP_Text installStatusText;

    public event Action InstallApkClicked;

    [Serializable]
    public class ModelImagePair
    {
        public string DisplayModel;
        public Sprite Image;
    }

    private void Awake()
    {
        if (installApkButton != null)
        {
            installApkButton.onClick.AddListener(() => InstallApkClicked?.Invoke());
        }
    }

    public void UpdateModel(string displayName)
    {
        if (modelText == null) return;

        modelText.text = string.IsNullOrEmpty(displayName) ? "Не подключено" : displayName;
        UpdateModelImage(displayName);
    }

    public void UpdateBattery(int level)
    {
        if (level < 0) return;
        if (batteryFillImage != null)
        {
            batteryFillImage.fillAmount = level / 100f;
        }
        if (batteryText != null)
        {
            batteryText.text = $"{level}%";
        }
    }

    public void UpdateStorage(long totalBytes, long freeBytes)
    {
        if (totalBytes <= 0) return;
        if (storageSlider != null)
        {
            storageSlider.value = (float)freeBytes / totalBytes;
        }

        var totalGb = (totalBytes / (1024f * 1024f * 1024f)).ToString("F1");
        var freeGb = (freeBytes / (1024f * 1024f * 1024f)).ToString("F1");
        if (storageText != null)
        {
            storageText.text = $"Всего: {totalGb} ГБ - Свободно: {freeGb} ГБ";
        }
    }

    public void UpdateStatus(HeadsetStatus status)
    {
        UpdateBattery(status.BatteryLevel);
        UpdateStorage(status.TotalStorageBytes, status.FreeStorageBytes);
    }

    public void UpdateSerial(string serial)
    {
        if (serialText == null) return;
        serialText.text = string.IsNullOrEmpty(serial) ? "Не подключено" : serial;
    }

    public void SetInstallButtonInteractable(bool interactable)
    {
        if (installApkButton != null)
        {
            installApkButton.interactable = interactable;
        }
    }

    public void SetInstallStatus(string text, Color? color = null)
    {
        if (installStatusText == null) return;

        installStatusText.text = text;
        if (color.HasValue)
        {
            installStatusText.color = color.Value;
        }
    }

    private void UpdateModelImage(string displayName)
    {
        if (modelImage == null) return;

        if (string.IsNullOrEmpty(displayName))
        {
            modelImage.sprite = fallbackModelSprite;
            modelImage.enabled = fallbackModelSprite != null;
            return;
        }

        // Точное совпадение по DisplayModel
        foreach (var pair in modelSprites)
        {
            if (pair == null || pair.Image == null || string.IsNullOrEmpty(pair.DisplayModel)) continue;

            if (string.Equals(pair.DisplayModel, displayName, StringComparison.OrdinalIgnoreCase))
            {
                modelImage.sprite = pair.Image;
                modelImage.enabled = true;
                return;
            }
        }

        // Не нашли — fallback
        modelImage.sprite = fallbackModelSprite;
        modelImage.enabled = fallbackModelSprite != null;

        Debug.LogWarning($"Нет спрайта для модели '{displayName}', показан fallback.");
    }
}