using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class GameMgr : MonoBehaviour
{
    private const string RootName = "GameMgr";
    private const string ManagerPrefabPath = "Prefabs/Managers/";
    private const string ManagerResourcePath = "Managers/";

    private static GameMgr instance;

    private readonly Dictionary<Type, Component> managers = new();
    private bool coreManagersLoaded;
    private bool sceneManagersLoaded;

    public static GameMgr Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public DataManager Data { get; private set; }
    public InputManager Input { get; private set; }
    private UIManager ui;

    public UIManager UI
    {
        get
        {
            if (ui == null)
                ui = GetOrCreateUIManager();

            return ui;
        }
    }
    public AudioManager Audio { get; private set; }
    public ObjectPoolManager Pool { get; private set; }
    public TimerManager Timer { get; private set; }
    public LoadoutManager Loadout { get; private set; }
    public PreviewManager Preview { get; private set; }
    public PlayerManager Player { get; private set; }
    public CameraManager Camera { get; private set; }
    public GameManager Game { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootstrapBeforeSceneLoad()
    {
        EnsureInstance();
        instance.LoadCoreManagers();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapAfterSceneLoad()
    {
        EnsureInstance();
        instance.LoadSceneManagers();
        instance.LoadGameFlow();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        var existing = FindObjectOfType<GameMgr>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        var root = new GameObject(RootName);
        instance = root.AddComponent<GameMgr>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCoreManagers();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneManagersLoaded = false;
        LoadSceneManagers();
        LoadGameFlow();
    }

    public T Get<T>() where T : Component
    {
        if (typeof(T) == typeof(UIManager))
            return UI as T;

        return GetOrCreateManager<T>();
    }

    private void LoadCoreManagers()
    {
        if (coreManagersLoaded)
            return;

        Data = GetOrCreateManager<DataManager>();
        Input = GetOrCreateManager<InputManager>();
        Audio = GetOrCreateManager<AudioManager>();
        Pool = GetOrCreateManager<ObjectPoolManager>();
        Timer = GetOrCreateManager<TimerManager>();
        Loadout = GetOrCreateManager<LoadoutManager>();
        Preview = GetOrCreateManager<PreviewManager>();
        Player = GetOrCreateManager<PlayerManager>();

        coreManagersLoaded = true;
    }

    private void LoadSceneManagers()
    {
        if (sceneManagersLoaded)
            return;

        ui = GetOrCreateUIManager();
        Camera = GetOrCreateManager<CameraManager>();
        sceneManagersLoaded = true;
    }

    private void LoadGameFlow()
    {
        if (Game != null)
            return;

        Game = GetOrCreateManager<GameManager>();
    }

    private T GetOrCreateManager<T>() where T : Component
    {
        var type = typeof(T);
        if (managers.TryGetValue(type, out var cached) && cached != null)
            return (T)cached;

        var existing = FindObjectOfType<T>();
        if (existing != null)
            return RegisterManager(existing);

        var prefab = Resources.Load<GameObject>(ManagerPrefabPath + type.Name) ??
                     Resources.Load<GameObject>(ManagerResourcePath + type.Name);

        T manager;
        if (prefab != null)
        {
            var obj = Instantiate(prefab);
            obj.name = type.Name;
            manager = obj.GetComponent<T>();
            if (manager == null)
                manager = obj.AddComponent<T>();
        }
        else
        {
            var obj = new GameObject(type.Name);
            manager = obj.AddComponent<T>();
        }

        return RegisterManager(manager);
    }

    private UIManager GetOrCreateUIManager()
    {
        if (managers.TryGetValue(typeof(UIManager), out var cached) && cached != null)
            return (UIManager)cached;

        var existing = FindObjectOfType<UIManager>();
        if (existing != null)
            return RegisterManager(existing);

        var canvasObj = GetOrCreateCanvasObject();
        var manager = canvasObj.GetComponent<UIManager>();
        if (manager == null)
            manager = canvasObj.AddComponent<UIManager>();

        return RegisterManager(manager);
    }

    private static GameObject GetOrCreateCanvasObject()
    {
        var canvas = FindObjectOfType<Canvas>();
        GameObject canvasObj;
        if (canvas != null)
        {
            canvasObj = canvas.gameObject;
        }
        else
        {
            canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        if (canvasObj.GetComponent<CanvasScaler>() == null)
            canvasObj.AddComponent<CanvasScaler>();

        if (canvasObj.GetComponent<GraphicRaycaster>() == null)
            canvasObj.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        return canvasObj;
    }

    private T RegisterManager<T>(T manager) where T : Component
    {
        managers[typeof(T)] = manager;
        if (manager is not UIManager)
            manager.transform.SetParent(transform, true);

        RegisterMonoSingleton(manager);
        return manager;
    }

    private static void RegisterMonoSingleton<T>(T manager) where T : Component
    {
        switch (manager)
        {
            case DataManager value:
                DataManager.RegisterInstance(value);
                break;
            case InputManager value:
                InputManager.RegisterInstance(value);
                break;
            case UIManager value:
                UIManager.RegisterInstance(value);
                break;
            case AudioManager value:
                AudioManager.RegisterInstance(value);
                break;
            case ObjectPoolManager value:
                ObjectPoolManager.RegisterInstance(value);
                break;
            case TimerManager value:
                TimerManager.RegisterInstance(value);
                break;
            case LoadoutManager value:
                LoadoutManager.RegisterInstance(value);
                break;
            case PreviewManager value:
                PreviewManager.RegisterInstance(value);
                break;
            case PlayerManager value:
                PlayerManager.RegisterInstance(value);
                break;
            case CameraManager value:
                CameraManager.RegisterInstance(value);
                break;
            case GameManager value:
                GameManager.RegisterInstance(value);
                break;
        }
    }
}
