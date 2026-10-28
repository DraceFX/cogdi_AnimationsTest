using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultViewerUI : MonoBehaviour
{
    private enum View
    {
        Users,
        Scenarios,
        Csv
    }

    public enum ResultsSourceType
    {
        LocalFileSystem,
        AdbDevice
    }

    [Header("Root")]
    [SerializeField] private GameObject resultsRoot; //Корневой объект панели результатов (скрыт по умолчанию)

    [Header("Navigation")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private Button backButton;

    [Header("Content")]
    [SerializeField] private RectTransform listContainer; //Контейнер для кнопок
    [SerializeField] private Button listItemPrefab; //Префаб кнопки. Должен содержать TMP_Text в дочерних объектах
    [SerializeField] private TMP_Text contentText; //TMP_Text для отображения CSV. Должен быть ребёнком listContainer

    [Header("Source")]
    [SerializeField] private ResultsSourceType sourceType = ResultsSourceType.LocalFileSystem;
    [SerializeField] private string accountsPath = "Accounts";

    private IResultsSource _source;
    private View _view;
    private string _currentUser;
    private string _currentScenario;
    private CancellationTokenSource _navCts;

    private void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (resultsRoot != null)
        {
            resultsRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        try
        {
            _navCts?.Cancel();
        }
        catch { }

        _navCts?.Dispose();
    }

    public void Open()
    {
        if (resultsRoot == null) return;
        if (sourceType == ResultsSourceType.AdbDevice)
        {
            _source = null;
        }

        resultsRoot.SetActive(true);
        _ = NavigateToAsync(View.Users);
    }

    public void Close()
    {
        if (resultsRoot != null)
        {
            resultsRoot.SetActive(false);
        }
    }

    private async Task NavigateToAsync(View view, string user = null, string scenario = null)
    {
        // Отменяем предыдущую навигацию — защита от гонок при быстрых кликах
        _navCts?.Cancel();
        _navCts?.Dispose();
        _navCts = new CancellationTokenSource();
        var ct = _navCts.Token;

        _view = view;
        _currentUser = user;
        _currentScenario = scenario;

        try
        {
            switch (view)
            {
                case View.Users: await RenderUsersAsync(ct); break;
                case View.Scenarios: await RenderScenariosAsync(ct); break;
                case View.Csv: await RenderCsvAsync(ct); break;
            }
        }
        catch (OperationCanceledException)
        {
            // Пользователь быстро переключился — нормально, ничего не логируем
        }
        catch (Exception e)
        {
            Debug.LogError($"ResultViewer: {e.GetType().Name}: {e.Message}");
            SetContentText($"Ошибка: {e.Message}");
        }
    }


    private void OnBackClicked()
    {
        switch (_view)
        {
            case View.Users: Close(); break;
            case View.Scenarios: _ = NavigateToAsync(View.Users); ; break;
            case View.Csv: _ = NavigateToAsync(View.Scenarios, _currentUser); break;
        }
    }

    private async Task<IResultsSource> GetSourceAsync(CancellationToken ct)
    {
        if (_source != null) return _source;

        if (sourceType == ResultsSourceType.LocalFileSystem)
        {
            var path = Path.IsPathRooted(accountsPath) ? accountsPath : Path.Combine(Application.persistentDataPath, accountsPath);

            Debug.Log($"Results source: LocalFileSystem @ {path}");
            _source = new LocalResultsSource(path);
            return _source;
        }

        // AdbDevice
        var monitor = FindAnyObjectByType<HeadsetMonitor>();
        if (monitor == null || monitor.ConnectedDevice == null)
        {
            Debug.LogError("ADB source: нет активного подключения к шлему");
            return null;
        }

        var adb = monitor.AdbExecutor;
        if (adb == null)
        {
            Debug.LogError("ADB source: AdbExecutor не инициализирован");
            return null;
        }

        if (string.IsNullOrWhiteSpace(accountsPath))
        {
            Debug.LogError("ADB source: не задан accountsPath");
            return null;
        }

        Debug.Log($"Results source: AdbDevice @ {accountsPath} ({monitor.ConnectedDevice.Serial})");
        _source = new AdbResultsSource(adb, monitor.ConnectedDevice.Serial, accountsPath);
        return _source;
    }

    private async Task RenderUsersAsync(CancellationToken ct)
    {
        SetHeader("Результаты — пользователи");
        ClearContent();

        var source = await GetSourceAsync(ct);
        if (source == null) { SetContentText("Источник данных недоступен"); return; }

        var users = await source.GetUsersAsync(ct);
        ct.ThrowIfCancellationRequested();
        await Task.Yield(); // возврат в главный поток Unity перед работой с UI

        if (users.Count == 0)
        {
            SetContentText("Пользователи не найдены");
            return;
        }

        foreach (var user in users)
        {
            var captured = user;
            CreateListItem(user, () => _ = NavigateToAsync(View.Scenarios, captured));
        }
    }

    private async Task RenderScenariosAsync(CancellationToken ct)
    {
        SetHeader($"{_currentUser} — сценарии");
        ClearContent();

        var source = await GetSourceAsync(ct);
        if (source == null)
        {
            SetContentText("Источник данных недоступен");
            return;
        }

        var scenarios = await source.GetScenariosAsync(_currentUser, ct);
        ct.ThrowIfCancellationRequested();
        await Task.Yield();

        if (scenarios.Count == 0)
        {
            SetContentText("Сценарии не найдены");
            return;
        }

        var user = _currentUser;
        foreach (var scenario in scenarios)
        {
            var captured = scenario;
            CreateListItem(scenario, () => _ = NavigateToAsync(View.Csv, user, captured));
        }
    }

    private async Task RenderCsvAsync(CancellationToken ct)
    {
        SetHeader($"{_currentUser} > {_currentScenario}");
        ClearContent();

        var source = await GetSourceAsync(ct);
        if (source == null)
        {
            SetContentText("Источник данных недоступен");
            return;
        }

        var raw = await source.ReadCsvAsync(_currentUser, _currentScenario, ct);
        ct.ThrowIfCancellationRequested();
        await Task.Yield();

        if (raw == null)
        {
            SetContentText("Файл не найден или не читается");
            return;
        }

        SetContentText(CsvTable.FormatAsText(raw));
    }

    private void SetHeader(string text)
    {
        if (headerText != null)
        {
            headerText.text = text;
        }
    }

    private void ClearContent()
    {
        if (listContainer != null)
        {
            for (int i = listContainer.childCount - 1; i >= 0; i--)
            {
                var child = listContainer.GetChild(i).gameObject;
                if (contentText != null && child == contentText.gameObject) continue;

                Destroy(child);
            }
        }

        if (contentText != null)
        {
            contentText.text = string.Empty;
            contentText.gameObject.SetActive(false);
        }
    }

    private void SetContentText(string text)
    {
        if (contentText == null) return;

        contentText.text = text;
        contentText.gameObject.SetActive(true);
    }

    private void CreateListItem(string label, Action onClick)
    {
        if (listItemPrefab == null || listContainer == null) return;

        var btn = Instantiate(listItemPrefab, listContainer);
        btn.gameObject.SetActive(true);

        var txt = btn.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            txt.text = label;
        }

        btn.onClick.AddListener(() => onClick());
    }
}