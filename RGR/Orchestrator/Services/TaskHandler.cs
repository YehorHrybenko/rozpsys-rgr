namespace Orchestrator.Services;

public class TaskHandler
{
    public Dictionary<int, Action<string>> listeners = [];

    public void AddResponse(int id, string result)
    {
        lock (listeners)
        {
            listeners[id].Invoke(result);
            listeners.Remove(id);
        }
    }

    public void Subscribe(int id, Action<string> action)
    {
        lock (listeners)
        {
            listeners.Add(id, action);
        }
    }

    public Task<string> PromiseRetrieve(int id)
    {
        var promise = new TaskCompletionSource<string>();

        Subscribe(id, promise.SetResult);

        return promise.Task;
    }
}
