using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    [System.Serializable]
    public struct Task
    {
        public string taskId;
        public string taskMessage;
        public bool isCompleted;
        public string nextTaskId;
    }

    [SerializeField] private List<Task> tasks = new List<Task>();
    private Dictionary<string, Task> taskDictionary = new Dictionary<string, Task>();

    [SerializeField] private string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); return; }

        InitializeTaskDictionary(false);
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
        {
            if (NotificationManager.Instance != null) NotificationManager.Instance.ForceCloseAllNotifications();
            return;
        }

        // Görevleri sýfýrla
        InitializeTaskDictionary(true);

        // Görev kontrol sürecini baþlat
        StopAllCoroutines();
        StartCoroutine(CheckAndStartQuests());
    }

    private IEnumerator CheckAndStartQuests()
    {
        yield return new WaitForSeconds(0.2f);

        if (SceneManager.GetActiveScene().name != mainMenuSceneName)
        {
            if (GameSaveManager.Instance != null && GameSaveManager.Instance.isGameLoadedFromSave)
            {
                Debug.Log("Save yüklendi, baþlangýç görevi TaskManager tarafýndan atlandý.");
            }
            else
            {
                Debug.Log("Yeni oyun veya Restart: Baþlangýç görevleri veriliyor.");
                StartInitialQuests();
            }
        }
    }

    private void StartInitialQuests()
    {
        if (NotificationManager.Instance == null) return;
        TriggerTask("task_find_flashlight");
    }

    private void InitializeTaskDictionary(bool resetCompletionStatus)
    {
        taskDictionary.Clear();
        foreach (Task task in tasks)
        {
            Task currentTask = task;
            if (resetCompletionStatus) currentTask.isCompleted = false;
            taskDictionary[currentTask.taskId] = currentTask;
        }
    }

    public List<string> GetCompletedTasks()
    {
        List<string> completed = new List<string>();
        foreach (var kvp in taskDictionary)
        {
            if (kvp.Value.isCompleted) completed.Add(kvp.Key);
        }
        return completed;
    }

    public void RestoreCompletedTasks(List<string> loadedTaskIds)
    {
        InitializeTaskDictionary(true);
        string lastActiveTaskId = "";

        foreach (string id in loadedTaskIds)
        {
            if (taskDictionary.ContainsKey(id))
            {
                Task t = taskDictionary[id];
                t.isCompleted = true;
                taskDictionary[id] = t;

                if (!string.IsNullOrEmpty(t.nextTaskId))
                {
                    lastActiveTaskId = t.nextTaskId;
                }
            }
        }

        if (!string.IsNullOrEmpty(lastActiveTaskId) && taskDictionary.ContainsKey(lastActiveTaskId))
        {
            Task activeTask = taskDictionary[lastActiveTaskId];
            if (!activeTask.isCompleted && NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification(NotificationType.GorevHatirlatma, activeTask.taskMessage);
            }
        }
        else if (loadedTaskIds.Count == 0)
        {
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(NotificationType.GorevHatirlatma, tasks[0].taskMessage);
        }
    }

    // --- YENÝ EKLENEN YARDIMCI METOT ---
    public string GetTaskBaseMessage(string taskId)
    {
        if (taskDictionary.ContainsKey(taskId))
        {
            return taskDictionary[taskId].taskMessage;
        }
        return "";
    }
    // -----------------------------------

    public void TriggerTask(string taskId)
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        if (taskDictionary.ContainsKey(taskId) && !taskDictionary[taskId].isCompleted)
        {
            Task task = taskDictionary[taskId];
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification(NotificationType.GorevHatirlatma, task.taskMessage);
        }
    }

    public void CompleteTask(string taskId)
    {
        if (taskDictionary.ContainsKey(taskId) && !taskDictionary[taskId].isCompleted)
        {
            Task task = taskDictionary[taskId];
            task.isCompleted = true;
            taskDictionary[taskId] = task;

            if (NotificationManager.Instance != null)
                NotificationManager.Instance.CloseTaskNotification(task.taskMessage);

            if (!string.IsNullOrEmpty(task.nextTaskId))
                TriggerTask(task.nextTaskId);
        }
    }
}