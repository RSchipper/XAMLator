﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace XAMLator
{
    public class EvalRequest
    {
        public string Code;
        public string NewTypeName;
        public string Xaml;
        public Dictionary<string, string> StyleSheets;
        public bool NeedsRebuild;
        public string ResourceName;
        public string OriginalTypeName;
    }

    public class EvalMessage
    {
        public string MessageType;
        public string Text;
        public int Line;
        public int Column;

        public EvalMessage(string messageType, string text, int line = 0, int column = 0)
        {
            MessageType = messageType;
            Text = text;
            Line = line;
            Column = column;
        }
    }

    public class EvalResult
    {
        public EvalMessage[] Messages;
        public TimeSpan Duration;
        public Type ResultType;
        public Type OriginalType;
        public bool HasResult => ResultType != null;
        public string Code;
        public string ResourceName;

        public bool HasErrors
        {
            get { return Messages != null && Messages.Any(m => m.MessageType == "error"); }
        }
    }

    public class EvalResponse
    {
        public EvalMessage[] Messages;
        public Dictionary<string, List<string>> WatchValues;
        public TimeSpan Duration;

        public bool HasErrors
        {
            get { return Messages != null && Messages.Any(m => m.MessageType == "error"); }
        }
    }
}

