#if NET_4_6 || NET_STANDARD_2_0
using System;
using UnityCef.Shared;
using UnityEngine;
using UnityEngine.Events;

namespace UnityCef.Unity
{
    [Serializable]
    public class OnConsoleMessageEvent : UnityEvent<LogLevel, string, string, int> { }
}
#endif
