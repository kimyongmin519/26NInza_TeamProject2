#if UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class TimelineBubbleDialogTests
{
    [Test]
    public void ReusingMapStartEventClearsPreviousTimelineCallback()
    {
        var request = new StartBubbleDialogEvent().InitData(null, null, () => { });
        request.Accepted = true;
        request.InitData(null, null);
        Assert.That(request.Accepted, Is.False);
        Assert.That(request.Completed, Is.Null);
    }

    [Test]
    public void OnlyMatchingCancellationCompletesTheRequestAndOnlyOnce()
    {
        var go = new GameObject("Bubble completion test");
        go.SetActive(false);
        try
        {
            var manager = go.AddComponent<BubbleDialogManager>();
            var request = new StartBubbleDialogEvent();
            int completions = 0;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BubbleDialogManager).GetField("_activeRequest", flags).SetValue(manager, request);
            typeof(BubbleDialogManager).GetField("_completed", flags).SetValue(manager, (Action)(() => completions++));
            MethodInfo cancel = typeof(BubbleDialogManager).GetMethod("HandleCancelDialog", flags);
            cancel.Invoke(manager, new object[] { new CancelBubbleDialogEvent { Request = new StartBubbleDialogEvent() } });
            Assert.That(completions, Is.Zero);
            cancel.Invoke(manager, new object[] { new CancelBubbleDialogEvent { Request = request } });
            cancel.Invoke(manager, new object[] { new CancelBubbleDialogEvent { Request = request } });
            Assert.That(completions, Is.EqualTo(1));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
#endif
