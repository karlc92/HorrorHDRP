using System.Collections.Generic;

public class TaskListEntryViewData
{
    public string TaskInstanceId;
    public string TitleKey;
    public List<string> DetailKeys = new List<string>();
    public List<TaskListDetailViewData> Details = new List<TaskListDetailViewData>();
    public bool Completed;
}
