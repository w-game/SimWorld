using System;
using System.Collections.Generic;

public class DialogData
{
    public string Content { get; set; }
    public Action Callback { get; set; }
    public List<DialogOption> Options { get; set; } = new List<DialogOption>();
}

public class DialogOption
{
    public string Text { get; set; }
    public Action OnClick { get; set; }
}