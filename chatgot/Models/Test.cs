using System;
using System.Collections.Generic;

public class RootTest
{
    public string type { get; set; }
    public AppThreadPostUpdated appThreadPostUpdated { get; set; }
    public DateTime timestamp { get; set; }
}

public class AppThreadPostUpdated
{
    public string appId { get; set; }
    public string threadId { get; set; }
    public string postId { get; set; }
    public NewPost newPost { get; set; }
}

public class NewPost
{
    public string id { get; set; }
    public Source source { get; set; }
    public string type { get; set; }
    public DateTime dateCreated { get; set; }
    public DateTime dateLastUpdated { get; set; }
    public ChatMessage chatMessage { get; set; }
    public List<object> billingEvents { get; set; }
}

public class Source
{
    public string workflowId { get; set; }
    public string stepId { get; set; }
    public string logId { get; set; }
    public string idempotencyKey { get; set; }
}

public class ChatMessage
{
    public string id { get; set; }
    public string source { get; set; }
    public string content { get; set; }
    public long dateSent { get; set; }
    public bool isInProgress { get; set; }
    public PresentationInfo _presentationInfo { get; set; }
    public Metadata _metadata { get; set; }
    public DebugInfo _debugInfo { get; set; }
    public bool loggingEnabled { get; set; }
}

public class PresentationInfo
{
    public string messageSource { get; set; }
}

public class Metadata
{
    public string workflowId { get; set; }
    public string automationId { get; set; }
    public List<object> citations { get; set; }
    public string groupId { get; set; }
}

public class DebugInfo
{
    public ModelSettings modelSettings { get; set; }
}

public class ModelSettings
{
    public string model { get; set; }
    public string preamble { get; set; }
    public ImageModel imageModel { get; set; }
    public double temperature { get; set; }
    public int maxResponseTokens { get; set; }
    public bool multiModelEnabled { get; set; }
    public string userMessagePrefix { get; set; }
    public string summarizationEngine { get; set; }
    public string systemMessagePrefix { get; set; }
    public string tokenOverflowStrategy { get; set; }
}

public class ImageModel
{
    public object model { get; set; }
}
