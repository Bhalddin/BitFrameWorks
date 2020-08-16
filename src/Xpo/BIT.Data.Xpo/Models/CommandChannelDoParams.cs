﻿using System;

namespace BIT.Xpo.Models
{
    public class CommandChannelDoParams
    {
        public string Command { get; set; }

        public object Args { get; set; }
        public CommandChannelDoParams(string command, object args)
        {
            Command = command;
            Args = args;
        }
        public CommandChannelDoParams()
        {
        }
    }
}